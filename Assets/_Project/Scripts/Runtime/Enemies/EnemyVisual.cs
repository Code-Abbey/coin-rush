using UnityEngine;

namespace CoinRush.Enemies
{
    /// <summary>
    /// Pure presentation for an enemy: tints the body by state and shows a "!" alert marker while chasing.
    /// Kept separate from EnemyBehaviour so behaviour and visuals can change independently.
    /// Uses a MaterialPropertyBlock (URP Lit: _BaseColor / _EmissionColor) so instances don't create material copies.
    /// </summary>
    [RequireComponent(typeof(EnemyBehaviour))]
    public class EnemyVisual : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private GameObject alertMarker;

        [Header("State Colours")]
        [SerializeField] private Color patrolColor = new Color(0.55f, 0.25f, 0.85f);
        [SerializeField] private Color chaseColor = new Color(0.95f, 0.15f, 0.2f);
        [SerializeField] private Color searchColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField, Min(0.1f)] private float colorLerpSpeed = 6f;

        [Header("Squash & Stretch")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private float bounceAmount = 0.08f;
        [SerializeField] private float bounceSpeed = 10f;

        private EnemyBehaviour behaviour;
        private MaterialPropertyBlock block;
        private Color currentColor;
        private Color targetColor;
        private Vector3 baseScale;

        private void Awake()
        {
            behaviour = GetComponent<EnemyBehaviour>();
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            block = new MaterialPropertyBlock();
            currentColor = targetColor = patrolColor;
            if (bodyRoot != null) baseScale = bodyRoot.localScale;
            if (alertMarker != null) alertMarker.SetActive(false);
        }

        private void OnEnable() => behaviour.StateChanged += HandleStateChanged;
        private void OnDisable() => behaviour.StateChanged -= HandleStateChanged;

        private void HandleStateChanged(EnemyState previous, EnemyState current)
        {
            targetColor = current switch
            {
                EnemyState.Chase => chaseColor,
                EnemyState.Search => searchColor,
                _ => patrolColor
            };
            if (alertMarker != null) alertMarker.SetActive(current == EnemyState.Chase);
        }

        private void Update()
        {
            currentColor = Color.Lerp(currentColor, targetColor, colorLerpSpeed * Time.deltaTime);
            if (bodyRenderer != null)
            {
                bodyRenderer.GetPropertyBlock(block);
                block.SetColor(ColorId, currentColor);
                block.SetColor(EmissionId, currentColor * 0.25f);
                bodyRenderer.SetPropertyBlock(block);
            }

            if (bodyRoot != null && behaviour.State != EnemyState.Idle)
            {
                float speedFactor = behaviour.State == EnemyState.Chase ? 1.6f : 1f;
                float s = Mathf.Sin(Time.time * bounceSpeed * speedFactor) * bounceAmount;
                bodyRoot.localScale = new Vector3(baseScale.x - s, baseScale.y + s, baseScale.z - s);
            }
        }
    }
}
