using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Lightweight genome representing behaviour knobs for an ant colony.
    /// Mutations tweak these values between generations to explore the search space.
    /// </summary>
    [System.Serializable]
    public class AntGenome
    {
        public float MoveIntervalSeconds = 0.5f;
        public int MaxClimbHeight = 2;
        public float DigChancePerStep = 0.15f;
        public float ShareThreshold = 0.35f;
        public float ShareAmount = 10f;
        public float NestCostFraction = 1f / 3f;
        public float NestCooldownSeconds = 2.5f;
        public float HealthDecayPerSecond = 1.5f;
        public float AcidHealthMultiplier = 2f;
        public float MaxHealth = 100f;

        public static AntGenome CreateRandom(System.Random rng)
        {
            return new AntGenome
            {
                MoveIntervalSeconds = Mathf.Lerp(0.2f, 1.0f, (float)rng.NextDouble()),
                MaxClimbHeight = rng.Next(1, 4),
                DigChancePerStep = Mathf.Lerp(0.05f, 0.5f, (float)rng.NextDouble()),
                ShareThreshold = Mathf.Lerp(0.1f, 0.7f, (float)rng.NextDouble()),
                ShareAmount = Mathf.Lerp(5f, 25f, (float)rng.NextDouble()),
                NestCostFraction = Mathf.Lerp(0.2f, 0.5f, (float)rng.NextDouble()),
                NestCooldownSeconds = Mathf.Lerp(1.0f, 4.0f, (float)rng.NextDouble()),
                HealthDecayPerSecond = Mathf.Lerp(0.8f, 2.5f, (float)rng.NextDouble()),
                AcidHealthMultiplier = Mathf.Lerp(1.5f, 3.0f, (float)rng.NextDouble()),
                MaxHealth = Mathf.Lerp(70f, 150f, (float)rng.NextDouble())
            };
        }

        public AntGenome Clone()
        {
            return (AntGenome)MemberwiseClone();
        }

        public void Mutate(System.Random rng, float strength)
        {
            MoveIntervalSeconds = MutateFloat(MoveIntervalSeconds, 0.1f, 1.2f, rng, strength);
            MaxClimbHeight = Mathf.Clamp(Mathf.RoundToInt(MaxClimbHeight + NextGaussian(rng) * strength * 2f), 1, 4);
            DigChancePerStep = MutateFloat(DigChancePerStep, 0.01f, 0.8f, rng, strength);
            ShareThreshold = MutateFloat(ShareThreshold, 0.05f, 0.9f, rng, strength);
            ShareAmount = MutateFloat(ShareAmount, 1f, 40f, rng, strength);
            NestCostFraction = MutateFloat(NestCostFraction, 0.1f, 0.6f, rng, strength);
            NestCooldownSeconds = MutateFloat(NestCooldownSeconds, 0.5f, 6f, rng, strength);
            HealthDecayPerSecond = MutateFloat(HealthDecayPerSecond, 0.5f, 3f, rng, strength);
            AcidHealthMultiplier = MutateFloat(AcidHealthMultiplier, 1.0f, 4f, rng, strength);
            MaxHealth = MutateFloat(MaxHealth, 50f, 200f, rng, strength);
        }

        private float MutateFloat(float value, float min, float max, System.Random rng, float strength)
        {
            float delta = NextGaussian(rng) * strength * (max - min) * 0.1f;
            return Mathf.Clamp(value + delta, min, max);
        }

        private float NextGaussian(System.Random rng)
        {
            // Box-Muller transform
            float u1 = 1f - (float)rng.NextDouble();
            float u2 = 1f - (float)rng.NextDouble();
            return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        }
    }
}
