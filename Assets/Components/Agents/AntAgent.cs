using Antymology.Terrain;
using Antymology.Agents.Evolution;
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
        [Header("Configuration")]
        public AntConfig SharedConfig;
        public AntGenome GenomeOverride;

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
            ApplySharedConfig();
            if (GenomeOverride == null && EvolutionManager.Instance != null)
            {
                GenomeOverride = EvolutionManager.Instance.CurrentGenome;
            }
            ApplyGenome();
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

            DepositPheromone(IsQueen ? 2f : 1f);
            DecayAndDiffuse();

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
            List<Vector3Int> higherNeighbours = new List<Vector3Int>();
            float bestPheromone = float.MinValue;
            Vector3Int bestPheromoneTarget = Vector3Int.zero;
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

                var target = new Vector3Int(candidateColumn.x, surfaceY, candidateColumn.z);
                neighbours.Add(target);

                float pher = GetPheromone(target);
                if (pher > bestPheromone)
                {
                    bestPheromone = pher;
                    bestPheromoneTarget = target;
                }

                // Track options that bring us closer to the surface if we are underground.
                if (WorldManager.Instance.TryGetSurfaceAt(_gridPosition.x, _gridPosition.z, out int currentSurfaceY))
                {
                    if (_gridPosition.y < currentSurfaceY - 1 && surfaceY >= _gridPosition.y)
                    {
                        higherNeighbours.Add(target);
                    }
                }
            }

            if (neighbours.Count == 0)
                return;

            // Prefer climbing toward the surface if we are buried.
            Vector3Int chosen;
            if (higherNeighbours.Count > 0)
            {
                // pick the highest option to exit tunnels.
                higherNeighbours.Sort((a, b) => b.y.CompareTo(a.y));
                chosen = higherNeighbours[0];
            }
            else if (bestPheromone > 0)
            {
                chosen = bestPheromoneTarget;
            }
            else
            {
                chosen = neighbours[Random.Range(0, neighbours.Count)];
            }
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

            // Avoid building deeply buried nests; stay near the column surface.
            if (WorldManager.Instance.TryGetSurfaceAt(_gridPosition.x, _gridPosition.z, out int surfaceY))
            {
                if (_gridPosition.y < surfaceY - 1)
                    return;
            }

            // Require open space above to prevent entombed nests.
            if (!(WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y + 1, _gridPosition.z) is AirBlock))
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

        private void DepositPheromone(float amount)
        {
            AbstractBlock block = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);
            block.pheromone = Mathf.Min(block.pheromone + amount, 50f);
        }

        private void DecayAndDiffuse()
        {
            AbstractBlock block = WorldManager.Instance.GetBlock(_gridPosition.x, _gridPosition.y, _gridPosition.z);
            block.pheromone = Mathf.Max(0, block.pheromone * 0.97f);

            // Push a fraction to neighbours for a simple gradient.
            Vector3Int[] offsets = new[]
            {
                new Vector3Int(1,0,0),
                new Vector3Int(-1,0,0),
                new Vector3Int(0,1,0),
                new Vector3Int(0,-1,0),
                new Vector3Int(0,0,1),
                new Vector3Int(0,0,-1)
            };
            float share = block.pheromone * 0.05f;
            if (share <= 0) return;
            foreach (var o in offsets)
            {
                var pos = _gridPosition + o;
                AbstractBlock n = WorldManager.Instance.GetBlock(pos.x, pos.y, pos.z);
                n.pheromone += share;
            }
            block.pheromone = Mathf.Max(0, block.pheromone - share * offsets.Length);
        }

        private float GetPheromone(Vector3Int position)
        {
            return WorldManager.Instance.GetBlock(position.x, position.y, position.z).pheromone;
        }

        private void Die()
        {
            AntColonyManager.Instance.UnregisterAnt(this);
            Destroy(gameObject);
        }

        #endregion

        #region Config

        /// <summary>
        /// Copies values from a shared config object if one is assigned.
        /// </summary>
        public void ApplySharedConfig()
        {
            if (SharedConfig == null)
                return;

            IsQueen = SharedConfig.IsQueen;
            MaxHealth = SharedConfig.MaxHealth;
            HealthDecayPerSecond = SharedConfig.HealthDecayPerSecond;
            AcidHealthMultiplier = SharedConfig.AcidHealthMultiplier;
            MoveIntervalSeconds = SharedConfig.MoveIntervalSeconds;
            MaxClimbHeight = SharedConfig.MaxClimbHeight;
            DigChancePerStep = SharedConfig.DigChancePerStep;
            ShareThreshold = SharedConfig.ShareThreshold;
            ShareAmount = SharedConfig.ShareAmount;
            NestCostFraction = SharedConfig.NestCostFraction;
            NestCooldownSeconds = SharedConfig.NestCooldownSeconds;
        }

        /// <summary>
        /// Applies genome overrides if available.
        /// </summary>
        public void ApplyGenome()
        {
            if (GenomeOverride == null)
                return;

            MoveIntervalSeconds = GenomeOverride.MoveIntervalSeconds;
            MaxClimbHeight = GenomeOverride.MaxClimbHeight;
            DigChancePerStep = GenomeOverride.DigChancePerStep;
            ShareThreshold = GenomeOverride.ShareThreshold;
            ShareAmount = GenomeOverride.ShareAmount;
            NestCostFraction = GenomeOverride.NestCostFraction;
            NestCooldownSeconds = GenomeOverride.NestCooldownSeconds;
            HealthDecayPerSecond = GenomeOverride.HealthDecayPerSecond;
            AcidHealthMultiplier = GenomeOverride.AcidHealthMultiplier;
            MaxHealth = GenomeOverride.MaxHealth;
        }

        #endregion
    }
}
