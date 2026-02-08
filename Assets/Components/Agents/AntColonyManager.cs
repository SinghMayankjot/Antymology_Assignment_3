using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Central registry for all ants in the scene.
    /// Tracks occupancy per grid coordinate so agents can make
    /// decisions about shared resources (eg. mulch consumption, health sharing).
    /// </summary>
    public class AntColonyManager : Singleton<AntColonyManager>
    {
        private readonly List<AntAgent> ants = new List<AntAgent>();
        private readonly Dictionary<Vector3Int, HashSet<AntAgent>> occupancy = new Dictionary<Vector3Int, HashSet<AntAgent>>();

        /// <summary>
        /// Optional reference to the single queen.
        /// </summary>
        public AntAgent Queen { get; private set; }

        /// <summary>
        /// Registers a new ant and its starting position.
        /// </summary>
        public void RegisterAnt(AntAgent ant, Vector3Int gridPosition)
        {
            if (!ants.Contains(ant))
            {
                ants.Add(ant);
            }

            if (ant.IsQueen)
            {
                Queen = ant;
            }

            UpdateAntPosition(ant, gridPosition);
        }

        /// <summary>
        /// Updates the cached position of an ant. Removes stale
        /// entries and inserts the new position into the occupancy map.
        /// </summary>
        public void UpdateAntPosition(AntAgent ant, Vector3Int gridPosition)
        {
            // remove old occupancy
            foreach (var kvp in occupancy)
            {
                if (kvp.Value.Contains(ant))
                {
                    kvp.Value.Remove(ant);
                    break;
                }
            }

            if (!occupancy.TryGetValue(gridPosition, out var bucket))
            {
                bucket = new HashSet<AntAgent>();
                occupancy[gridPosition] = bucket;
            }

            bucket.Add(ant);
        }

        /// <summary>
        /// Removes an ant from all tracking structures.
        /// </summary>
        public void UnregisterAnt(AntAgent ant)
        {
            ants.Remove(ant);

            if (Queen == ant)
            {
                Queen = null;
            }

            foreach (var kvp in occupancy)
            {
                if (kvp.Value.Contains(ant))
                {
                    kvp.Value.Remove(ant);
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if more than one ant occupies the supplied grid cell.
        /// </summary>
        public bool IsContested(Vector3Int gridPosition)
        {
            return occupancy.TryGetValue(gridPosition, out var bucket) && bucket.Count > 1;
        }

        /// <summary>
        /// Returns all ants occupying a grid position (including the caller).
        /// </summary>
        public IReadOnlyCollection<AntAgent> GetAntsAt(Vector3Int gridPosition)
        {
            if (occupancy.TryGetValue(gridPosition, out var bucket))
            {
                return bucket;
            }

            return System.Array.Empty<AntAgent>();
        }

        /// <summary>
        /// Exposes the currently tracked ants.
        /// </summary>
        public IReadOnlyList<AntAgent> Ants => ants;
    }
}
