using Antymology.Terrain;
using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Basic ant behaviour implementing the required rules (health, digging,
    /// mulch consumption, acid damage, health sharing, and nest production for the queen).
    /// The behaviour itself is intentionally simple/random; students can extend it
    /// with evolutionary logic later.
    /// </summary>
    public class AntAgent : MonoBehaviour
    {
        [Header("Identity")]
        public bool IsQueen = false;

        [Header("Health")]
        public float MaxHealth = 100f;
        public float HealthDecayPerSecond = 1.5f;
        public float AcidHealthMultiplier = 2f;

        [Header("Movement")]
        public float MoveIntervalSeconds = 0.5f;
        public int MaxClimbHeight = 2;

        [Header("Digging")]
        [Range(0f, 1f)] public float DigChancePerStep = 0.15f;

        [Header("Sharing")]
        [Range(0f, 1f)] public float ShareThreshold = 0.35f;
        public float ShareAmount = 10f;

        [Header("Queen")]
        public float NestCostFraction = 1f / 3f;
        public float NestCooldownSeconds = 2.5f;

        private float _currentHealth;
        private float _moveTimer;
        private float _nestTimer;
        private Vector3Int _gridPosition;

        void Start()
        {
            _currentHealth = MaxHealth;
            _gridPosition = WorldToGrid(transform.position);
            AntColonyManager.Instance.RegisterAnt(this, _gridPosition);
        }

        void Update()
        {
            _moveTimer += Time.deltaTime;
            _nestTimer += Time.deltaTime;

            ApplyHealthDecay(Time.deltaTime);

            if (_currentHealth <= 0f)
            {
                Die();
                return;
            }

            if (_moveTimer >= MoveIntervalSeconds)
            {
                Step();
                _moveTimer = 0f;
            }
        }

        /// <summary>
        /// Handles a single decision/action tick.
        /// </summary>
        private void Step()
        {
            // Refresh the grid position each step in case external forces moved the ant.
            _gridPosition = WorldToGrid(transform.position);
            AntColonyManager.Instance.UpdateAntPosition(this, _gridPosition);

            // 1) Eat mulch if available and uncontested.
            if (TryConsumeMulch())
            {
                return;
            }

            // 2) Share health with any neighbours on the same tile.
            TryShareHealth();

            // 3) Dig occasionally to expand tunnels.
            if (Random.value < DigChancePerStep)
            {
                TryDig();
            }

            // 4) Move randomly among reachable neighbours.
            TryRandomMove();

            // 5) If queen, attempt to place a nest block.
            if (IsQueen)
            {
                TryPlaceNest();
            }
        }

        #region Actions

        private void ApplyHealthDecay(float deltaTime)
        {
            var blockBeneath = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);
            float multiplier = blockBeneath is AcidicBlock ? AcidHealthMultiplier : 1f;
            _currentHealth -= HealthDecayPerSecond * multiplier * deltaTime;
        }

        private bool TryConsumeMulch()
        {
            AbstractBlock block = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);
            if (block is MulchBlock && !AntColonyManager.Instance.IsContested(_gridPosition))
            {
                _currentHealth = MaxHealth;
                WorldManager.Instance.SetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z, new AirBlock());
                return true;
            }

            return false;
        }

        private void TryShareHealth()
        {
            IReadOnlyCollection<AntAgent> cohabitants = AntColonyManager.Instance.GetAntsAt(_gridPosition);
            foreach (var other in cohabitants)
            {
                if (other == this)
                    continue;

                if (other._currentHealth / other.MaxHealth < ShareThreshold && _currentHealth > ShareAmount)
                {
                    float transferable = Mathf.Min(ShareAmount, _currentHealth);
                    _currentHealth -= transferable;
                    other.ReceiveHealth(transferable);
                }
            }
        }

        private void TryDig()
        {
            AbstractBlock block = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);
            if (block is ContainerBlock || block is AirBlock || block is NestBlock)
            {
                return;
            }

            WorldManager.Instance.SetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z, new AirBlock());

            // Settle onto the new surface if we just removed the ground beneath us.
            if (WorldManager.Instance.TryGetSurfaceAt(_gridPosition.x, _gridPosition.z, out int surfaceY))
            {
                _gridPosition = new Vector3Int(_gridPosition.x, surfaceY, _gridPosition.z);
                transform.position = new Vector3(_gridPosition.x, _gridPosition.y + 0.1f, _gridPosition.z);
                AntColonyManager.Instance.UpdateAntPosition(this, _gridPosition);
            }
        }

        private void TryRandomMove()
        {
            List<Vector3Int> neighbours = new List<Vector3Int>();
            Vector3Int[] offsets = new[]
            {
                new Vector3Int(1,0,0),
                new Vector3Int(-1,0,0),
                new Vector3Int(0,0,1),
                new Vector3Int(0,0,-1)
            };

            foreach (var offset in offsets)
            {
                Vector3Int candidateColumn = new Vector3Int(_gridPosition.x + offset.x, _gridPosition.y, _gridPosition.z + offset.z);
                if (!WorldManager.Instance.TryGetSurfaceAt(candidateColumn.x, candidateColumn.z, out int surfaceY))
                    continue;

                if (Mathf.Abs(surfaceY - _gridPosition.y) > MaxClimbHeight)
                    continue;

                AbstractBlock surfaceBlock = WorldManager.Instance.GetBlock(candidateColumn.x, surfaceY, candidateColumn.z);
                if (surfaceBlock is ContainerBlock)
                    continue;

                neighbours.Add(new Vector3Int(candidateColumn.x, surfaceY, candidateColumn.z));
            }

            if (neighbours.Count == 0)
                return;

            Vector3Int chosen = neighbours[Random.Range(0, neighbours.Count)];
            MoveTo(chosen);
        }

        private void TryPlaceNest()
        {
            if (_nestTimer < NestCooldownSeconds)
                return;

            float cost = MaxHealth * NestCostFraction;
            if (_currentHealth <= cost)
                return;

            AbstractBlock block = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);

            // Queens prefer empty or dug-out tiles.
            if (block is ContainerBlock)
                return;

            if (!(block is AirBlock))
            {
                // Dig out the space first.
                WorldManager.Instance.SetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z, new AirBlock());
            }

            WorldManager.Instance.SetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z, new NestBlock());
            _currentHealth -= cost;
            _nestTimer = 0f;
        }

        #endregion

        #region Helpers

        private void MoveTo(Vector3Int target)
        {
            Vector3 world = new Vector3(target.x, target.y + 0.1f, target.z);
            transform.position = world;
            _gridPosition = target;
            AntColonyManager.Instance.UpdateAntPosition(this, _gridPosition);
        }

        private Vector3Int WorldToGrid(Vector3 world)
        {
            return new Vector3Int(
                Mathf.RoundToInt(world.x),
                Mathf.RoundToInt(world.y),
                Mathf.RoundToInt(world.z));
        }

        private void ReceiveHealth(float amount)
        {
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
        }

        private void Die()
        {
            AntColonyManager.Instance.UnregisterAnt(this);
            Destroy(gameObject);
        }

        #endregion
    }
}
