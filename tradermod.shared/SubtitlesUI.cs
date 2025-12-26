using TMPro;
using UnityEngine;

namespace tarkin.tradermod.shared
{
    public class SubtitlesUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;

        private void OnEnable()
        {
            SubtitleBehaviour.OnSubtitleChange += SetText;
        }

        private void OnDisable()
        {
            SubtitleBehaviour.OnSubtitleChange -= SetText;
        }

        public void SetText(string subtitle)
        {
            if (string.IsNullOrEmpty(subtitle))
            {
                canvasGroup.alpha = 0f;
                return;
            }

            canvasGroup.alpha = 1f;
            text.text = subtitle;
        }
    }
}
