using TMPro;
using UnityEngine;

namespace tarkin.tradermod.eft.Bep
{
    public class SubtitlesUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;

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
