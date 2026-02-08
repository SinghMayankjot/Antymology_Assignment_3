using Antymology.Agents;
using Antymology.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace Antymology.UI
{
    /// <summary>
    /// Simple HUD element that reports the current nest block count
    /// (and number of living ants for convenience).
    /// Attach this to a Text element inside a Canvas.
    /// </summary>
    public class NestCounterUI : MonoBehaviour
    {
        public Text counterText;
        public float refreshIntervalSeconds = 0.5f;

        private float _timer;

        private void Awake()
        {
            if (counterText == null)
            {
                counterText = GetComponent<Text>();
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < refreshIntervalSeconds)
                return;

            _timer = 0f;

            if (counterText == null || WorldManager.Instance == null)
                return;

            int nests = WorldManager.Instance.NestBlockCount;
            int antCount = AntColonyManager.Instance != null ? AntColonyManager.Instance.Ants.Count : 0;
            counterText.text = $"Nest Blocks: {nests}\nAnts: {antCount}";
        }
    }
}
