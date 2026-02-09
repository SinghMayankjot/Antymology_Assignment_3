using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;
using Antymology.Agents;

namespace Antymology.Agents.Evolution
{
    /// <summary>
    /// Basic evolutionary loop that mutates behaviour genomes between timed evaluation cycles.
    /// Fitness is measured as the number of nest blocks produced during an evaluation window.
    /// </summary>
    public class EvolutionManager : Singleton<EvolutionManager>
    {
        [Header("Population")]
        public int populationSize = 6;
        public int eliteCount = 2;
        public float mutationStrength = 0.2f;

        [Header("Evaluation")]
        public float evaluationDurationSeconds = 60f;
        public bool regenerateTerrainEachGeneration = true;
        public bool autoRun = true;

        /// <summary>
        /// The genome currently applied to spawned ants.
        /// </summary>
        public AntGenome CurrentGenome { get; private set; }

        private readonly List<AntGenome> _population = new List<AntGenome>();
        private readonly List<float> _fitness = new List<float>();
        private int _currentIndex = 0;
        private float _timer = 0f;
        private bool _generationActive = false;
        private System.Random _rng;

        private void Start()
        {
            _rng = new System.Random(ConfigurationManager.Instance.Seed + 999);
            InitializePopulation();

            if (autoRun)
            {
                BeginGeneration(0);
            }
        }

        private void Update()
        {
            if (!autoRun || !_generationActive)
                return;

            _timer += Time.deltaTime;
            if (_timer >= evaluationDurationSeconds)
            {
                CompleteGeneration();
            }
        }

        private void InitializePopulation()
        {
            _population.Clear();
            _fitness.Clear();
            for (int i = 0; i < populationSize; i++)
            {
                _population.Add(AntGenome.CreateRandom(_rng));
                _fitness.Add(0f);
            }
        }

        private void BeginGeneration(int index)
        {
            if (index < 0 || index >= _population.Count)
                return;

            _generationActive = true;
            _timer = 0f;
            CurrentGenome = _population[index];
            _currentIndex = index;

            // Reset world state and ants for a clean evaluation.
            AntColonyManager.Instance?.DestroyAllAnts();
            if (regenerateTerrainEachGeneration)
            {
                WorldManager.Instance.RegenerateWorld();
            }
            else
            {
                WorldManager.Instance.ClearNestCount();
            }

            WorldManager.Instance.SpawnAnts(CurrentGenome);
        }

        private void CompleteGeneration()
        {
            _generationActive = false;
            _fitness[_currentIndex] = WorldManager.Instance.NestBlockCount;

            int nextIndex = _currentIndex + 1;
            if (nextIndex >= _population.Count)
            {
                EvolvePopulation();
                nextIndex = 0;
            }

            BeginGeneration(nextIndex);
        }

        private void EvolvePopulation()
        {
            // Rank genomes by fitness.
            List<int> indices = new List<int>();
            for (int i = 0; i < _population.Count; i++) indices.Add(i);
            indices.Sort((a, b) => _fitness[b].CompareTo(_fitness[a]));

            List<AntGenome> newPop = new List<AntGenome>();
            List<float> newFit = new List<float>();

            // Elitism: keep top genomes intact.
            int elites = Mathf.Clamp(eliteCount, 1, populationSize);
            for (int i = 0; i < elites; i++)
            {
                newPop.Add(_population[indices[i]].Clone());
                newFit.Add(0f);
            }

            // Fill the rest with mutated copies of elites.
            while (newPop.Count < populationSize)
            {
                var parent = newPop[_rng.Next(0, elites)].Clone();
                parent.Mutate(_rng, mutationStrength);
                newPop.Add(parent);
                newFit.Add(0f);
            }

            _population.Clear();
            _population.AddRange(newPop);
            _fitness.Clear();
            _fitness.AddRange(newFit);
        }
    }
}
