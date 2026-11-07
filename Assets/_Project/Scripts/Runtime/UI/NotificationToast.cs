using System.Collections;
using System.Collections.Generic;
using CoinRush.Core;
using TMPro;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>Big centred messages ("PORTAL OPEN!", "SHIELD UP!"). Queued so they never overlap.</summary>
    public class NotificationToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text messageText;
        [SerializeField, Min(0.2f)] private float holdTime = 1.4f;

        private readonly Queue<(string, Color)> queue = new Queue<(string, Color)>();
        private bool showing;

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        private void OnEnable() => GameEvents.Notification += Enqueue;
        private void OnDisable() => GameEvents.Notification -= Enqueue;

        private void Enqueue(string message, Color color)
        {
            queue.Enqueue((message, color));
            if (!showing) StartCoroutine(ShowQueue());
        }

        private IEnumerator ShowQueue()
        {
            showing = true;
            while (queue.Count > 0)
            {
                (string message, Color color) = queue.Dequeue();
                messageText.text = message;
                messageText.color = color;
                StartCoroutine(UITween.Pop(messageText.transform, 0.35f, 0.4f));
                yield return UITween.Fade(group, 1f, 0.15f);
                yield return new WaitForSecondsRealtime(holdTime);
                yield return UITween.Fade(group, 0f, 0.3f);
            }
            showing = false;
        }
    }
}
