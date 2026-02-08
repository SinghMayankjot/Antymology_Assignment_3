using UnityEngine;

namespace Antymology.Agents
{
    /// <summary>
    /// Shared ant configuration so stats can be tuned once and reused across all agents.
    /// </summary>
    [CreateAssetMenu(fileName = "AntConfig", menuName = "Antymology/Ant Config", order = 0)]
    public class AntConfig : ScriptableObject
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
        [Range(0f, 1f)] public float NestCostFraction = 1f / 3f;
        public float NestCooldownSeconds = 2.5f;
    }
}
