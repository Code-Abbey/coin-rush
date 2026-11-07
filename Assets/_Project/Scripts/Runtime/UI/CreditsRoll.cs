using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinRush.UI
{
    /// <summary>
    /// Classic rolling credits: scrolls the content upwards inside a masked viewport and loops.
    /// Mouse wheel / arrow keys scroll manually; the roll resumes after a short pause.
    /// </summary>
    public class CreditsRoll : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0f)] private float speed = 55f;
        [SerializeField, Min(0f)] private float manualSpeed = 900f;
        [SerializeField, Min(0f)] private float resumeDelay = 2f;

        private float resumeAt;

        private void OnEnable()
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, -viewport.rect.height * 0.35f);
            resumeAt = Time.unscaledTime + 0.8f; // short pause so the first lines can be read
        }

        private void Update()
        {
            float dy = 0f;
            Mouse mouse = Mouse.current;
            if (mouse != null) dy -= mouse.scroll.ReadValue().y * 0.6f;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dy += manualSpeed * Time.unscaledDeltaTime;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dy -= manualSpeed * Time.unscaledDeltaTime;
            }

            if (Mathf.Abs(dy) > 0.01f) resumeAt = Time.unscaledTime + resumeDelay;
            else if (Time.unscaledTime >= resumeAt) dy = speed * Time.unscaledDeltaTime;

            float max = content.rect.height;
            float min = -viewport.rect.height;
            float y = content.anchoredPosition.y + dy;
            if (y > max) y = min;      // loop back to the start
            if (y < min) y = min;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
        }
    }
}
