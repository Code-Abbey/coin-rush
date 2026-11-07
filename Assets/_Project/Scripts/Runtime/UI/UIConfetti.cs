using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRush.UI
{
    /// <summary>Lightweight UI confetti burst (unscaled time, no particle system needed on a canvas).</summary>
    public class UIConfetti : MonoBehaviour
    {
        [SerializeField] private Sprite pieceSprite;
        [SerializeField, Range(10, 300)] private int count = 140;
        [SerializeField] private Color[] colors =
        {
            new Color(1f, 0.71f, 0.2f), new Color(1f, 0.435f, 0.38f), new Color(0.263f, 0.851f, 0.741f),
            new Color(0.431f, 0.784f, 1f), new Color(0.965f, 0.949f, 0.918f)
        };

        private class Piece
        {
            public RectTransform rt;
            public Vector2 velocity;
            public float spin, sway, phase, life;
        }

        private readonly List<Piece> pieces = new List<Piece>();
        private RectTransform area;

        private void Awake() => area = (RectTransform)transform;

        public void Burst()
        {
            Clear();
            Rect r = area.rect;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                Image img = go.GetComponent<Image>();
                img.sprite = pieceSprite;
                img.color = colors[Random.Range(0, colors.Length)];
                img.raycastTarget = false;
                RectTransform rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(Random.Range(14f, 26f), Random.Range(8f, 14f));
                // Two cannons from the bottom corners plus a gentle rain from the top.
                bool rain = i % 3 == 0;
                bool left = i % 2 == 0;
                rt.anchoredPosition = rain
                    ? new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(0f, 300f))
                    : new Vector2(left ? r.xMin + 60f : r.xMax - 60f, r.yMin + 40f);
                Vector2 v = rain
                    ? new Vector2(Random.Range(-40f, 40f), Random.Range(-260f, -120f))
                    : new Vector2((left ? 1f : -1f) * Random.Range(250f, 750f), Random.Range(900f, 1500f));
                pieces.Add(new Piece { rt = rt, velocity = v, spin = Random.Range(-540f, 540f), sway = Random.Range(20f, 60f), phase = Random.value * 6f, life = 0f });
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float bottom = area.rect.yMin - 60f;
            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                Piece p = pieces[i];
                p.life += dt;
                p.velocity.y = Mathf.Max(p.velocity.y - 1400f * dt, -320f); // gravity + flutter terminal speed
                p.velocity.x *= 1f - 1.2f * dt;
                Vector2 pos = p.rt.anchoredPosition + p.velocity * dt;
                pos.x += Mathf.Sin(p.life * 5f + p.phase) * p.sway * dt;
                p.rt.anchoredPosition = pos;
                p.rt.Rotate(0f, 0f, p.spin * dt);
                p.rt.localScale = new Vector3(Mathf.Cos(p.life * 7f + p.phase), 1f, 1f); // 3D-ish tumble
                if (pos.y < bottom) { Destroy(p.rt.gameObject); pieces.RemoveAt(i); }
            }
        }

        private void Clear()
        {
            foreach (Piece p in pieces) if (p.rt != null) Destroy(p.rt.gameObject);
            pieces.Clear();
        }

        private void OnDisable() => Clear();
    }
}
