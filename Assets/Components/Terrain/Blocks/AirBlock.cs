using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Antymology.Terrain
{
    /// <summary>
    /// The air type of block. Contains the internal data representing phermones in the air.
    /// </summary>
    public class AirBlock : AbstractBlock
    {

        #region Fields

        /// <summary>
        /// Statically held is visible.
        /// </summary>
        private static bool _isVisible = false;

        /// <summary>
        /// Simple pheromone reservoir used by ants.
        /// </summary>
        private Dictionary<byte, double> phermoneDeposits;

        #endregion

        #region Methods

        /// <summary>
        /// Air blocks are going to be invisible.
        /// </summary>
        public override bool isVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Air blocks are invisible so asking for their tile map coordinate doesn't make sense.
        /// </summary>
        public override Vector2 tileMapCoordinate()
        {
            throw new Exception("An invisible tile cannot have a tile map coordinate.");
        }

        /// <summary>
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU WHATS POSSIBLE.
        /// </summary>
        /// <param name="neighbours"></param>
        public void Diffuse(AbstractBlock[] neighbours)
        {
            // Simple diffusion: push a small portion to neighbouring blocks, then decay this block.
            float share = pheromone;
            if (share <= 0f) return;

            float give = share * 0.1f;
            int count = 0;
            foreach (var n in neighbours)
            {
                if (n == null) continue;
                n.pheromone += give;
                count++;
            }

            if (count > 0)
                pheromone -= give * count;

            // Passive decay
            pheromone = Mathf.Max(0, pheromone * 0.98f);
        }

        #endregion

    }
}
