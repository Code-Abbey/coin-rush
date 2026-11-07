using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>
    /// Simple full-screen panel (e.g. Controls) with a Back button that returns to the panel it was opened from.
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [Tooltip("Panel shown again when Back is pressed.")]
        [SerializeField] private GameObject returnTo;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (returnTo != null) returnTo.SetActive(false);
            MonoBehaviourRunner.Run(UITween.ShowPanel(gameObject));
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (returnTo != null) MonoBehaviourRunner.Run(UITween.ShowPanel(returnTo));
        }
    }
}
