using CoinRush.Core;
using CoinRush.Player;
using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>
    /// Exit of a world. Closed (dim, barrier up) until the GameManager opens it when the coin quota
    /// is reached; then the swirl (SG_Portal Shader Graph) spins up and the player can step in.
    /// The swirl speed/intensity are driven through material properties exposed by the Shader Graph.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        private static readonly int SwirlSpeedId = Shader.PropertyToID("_SwirlSpeed");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [SerializeField] private Renderer swirlRenderer;
        [Tooltip("Force-field wall shown while the portal is closed.")]
        [SerializeField] private GameObject closedBarrier;
        [SerializeField] private ParticleSystem openParticles;
        [SerializeField] private Light portalLight;

        [Header("Look")]
        [SerializeField] private float closedSwirlSpeed = 0.3f;
        [SerializeField] private float openSwirlSpeed = 2.5f;
        [SerializeField] private float closedIntensity = 0.3f;
        [SerializeField] private float openIntensity = 3f;
        [SerializeField] private float transitionSpeed = 1.5f;

        private MaterialPropertyBlock block;
        private float openAmount;
        private bool isOpen;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            block = new MaterialPropertyBlock();
            ApplyLook(0f);
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.RegisterPortal(transform.position);
        }

        private void OnEnable() => GameEvents.PortalOpened += HandleOpened;
        private void OnDisable() => GameEvents.PortalOpened -= HandleOpened;

        private void HandleOpened(Vector3 _)
        {
            isOpen = true;
            if (closedBarrier != null) closedBarrier.SetActive(false);
            if (openParticles != null) openParticles.Play();
        }

        private void Update()
        {
            float target = isOpen ? 1f : 0f;
            if (Mathf.Approximately(openAmount, target)) return;
            openAmount = Mathf.MoveTowards(openAmount, target, transitionSpeed * Time.deltaTime);
            ApplyLook(openAmount);
        }

        private void ApplyLook(float t)
        {
            if (swirlRenderer != null)
            {
                swirlRenderer.GetPropertyBlock(block);
                block.SetFloat(SwirlSpeedId, Mathf.Lerp(closedSwirlSpeed, openSwirlSpeed, t));
                block.SetFloat(IntensityId, Mathf.Lerp(closedIntensity, openIntensity, t));
                swirlRenderer.SetPropertyBlock(block);
            }
            if (portalLight != null) portalLight.intensity = Mathf.Lerp(0.5f, 4f, t);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isOpen || GameManager.Instance == null) return;
            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) GameManager.Instance.EnterPortal(player.Health);
        }
    }
}
