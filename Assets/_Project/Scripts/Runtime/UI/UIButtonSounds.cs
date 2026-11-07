using CoinRush.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Hover and click sounds for any Button (added to every button by the builder).</summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private SoundId clickSound = SoundId.UIClick;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => AudioManager.Play(clickSound));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable) AudioManager.Play(SoundId.UIHover);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is AxisEventData) AudioManager.Play(SoundId.UIHover); // gamepad / keyboard navigation
        }
    }
}
