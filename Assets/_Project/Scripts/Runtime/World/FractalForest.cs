using System.Linq;
using CoinRush.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinRush.Level
{
    /// <summary>
    /// Grows a small forest of procedural <see cref="FractalTree"/>s when the world loads.
    /// The builder supplies candidate spots (clear of paths, pads, ramps and hazards) and a
    /// per-world style; the "forests" section of coinrush_config.json can override the style,
    /// the number of trees and the seed without rebuilding the scene.
    /// </summary>
    public class FractalForest : MonoBehaviour
    {
        [SerializeField] private Vector3[] spots = new Vector3[0];
        [SerializeField] private Material barkMaterial;
        [SerializeField] private Material leafMaterial;
        [SerializeField] private ForestData style = new ForestData();

        public void Configure(Vector3[] candidateSpots, Material bark, Material leaves, ForestData defaults)
        {
            spots = candidateSpots;
            barkMaterial = bark;
            leafMaterial = leaves;
            style = defaults;
        }

        private void Awake()
        {
            if (barkMaterial == null || leafMaterial == null || spots.Length == 0) return;

            ForestData s = Merge(style, JsonConfig.FindForest(JsonConfig.Load(), SceneManager.GetActiveScene().name));
            Material bark = new Material(barkMaterial) { name = "FractalBark (runtime)" };
            Material leaves = new Material(leafMaterial) { name = "FractalLeaves (runtime)" };
            bark.SetColor("_BaseColor", JsonConfig.ParseColor(s.barkColor, barkMaterial.GetColor("_BaseColor")));
            leaves.SetColor("_BaseColor", JsonConfig.ParseColor(s.leafColor, leafMaterial.GetColor("_BaseColor")));

            // Seeded shuffle of the candidate spots, then take as many trees as configured.
            var rng = new System.Random(s.seed);
            var chosen = spots.Select(p => (p, key: rng.Next()))
                              .OrderBy(t => t.key)
                              .Take(Mathf.Clamp(s.treeCount, 0, spots.Length))
                              .Select(t => t.p)
                              .ToList();

            for (int i = 0; i < chosen.Count; i++)
            {
                var go = new GameObject($"FractalTree_{i + 1:00}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.transform.position = chosen[i];
                go.transform.rotation = Quaternion.Euler(0f, rng.Next(0, 360), 0f);
                go.AddComponent<FractalTree>().Generate(s, s.seed * 1000 + i, bark, leaves);
            }
        }

        /// <summary>JSON values win wherever they are set; everything else keeps the builder's style.</summary>
        private static ForestData Merge(ForestData baseStyle, ForestData json)
        {
            if (json == null) return baseStyle;
            return new ForestData
            {
                sceneName = baseStyle.sceneName,
                treeCount = json.treeCount > 0 ? json.treeCount : baseStyle.treeCount,
                seed = json.seed != 0 ? json.seed : baseStyle.seed,
                iterations = json.iterations > 0 ? json.iterations : baseStyle.iterations,
                branchAngle = json.branchAngle > 0f ? json.branchAngle : baseStyle.branchAngle,
                trunkLength = json.trunkLength > 0f ? json.trunkLength : baseStyle.trunkLength,
                trunkRadius = json.trunkRadius > 0f ? json.trunkRadius : baseStyle.trunkRadius,
                lengthScale = json.lengthScale > 0f ? json.lengthScale : baseStyle.lengthScale,
                leafSize = json.leafSize > 0f ? json.leafSize : baseStyle.leafSize,
                barkColor = string.IsNullOrEmpty(json.barkColor) ? baseStyle.barkColor : json.barkColor,
                leafColor = string.IsNullOrEmpty(json.leafColor) ? baseStyle.leafColor : json.leafColor,
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
            foreach (Vector3 p in spots) Gizmos.DrawWireSphere(p, 0.6f);
        }
    }
}
