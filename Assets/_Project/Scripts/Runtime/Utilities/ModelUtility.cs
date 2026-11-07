using UnityEngine;

namespace CoinRush.Utilities
{
    /// <summary>Helpers for imported 3D models (third-party FBX files come in different units/sizes).</summary>
    public static class ModelUtility
    {
        /// <summary>Combined world-space bounds of every renderer under <paramref name="root"/>.</summary>
        public static Bounds GetBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>
        /// Uniformly scales a model so its height equals <paramref name="height"/> metres and
        /// moves it so its feet rest on the parent's origin.
        /// </summary>
        public static void FitToHeight(GameObject model, float height)
        {
            model.transform.localScale = Vector3.one;
            Bounds b = GetBounds(model);
            if (b.size.y < 0.0001f) return;

            float factor = height / b.size.y;
            model.transform.localScale = Vector3.one * factor;

            b = GetBounds(model);
            Vector3 offset = model.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            Transform parent = model.transform.parent;
            Vector3 feet = parent != null ? parent.position : Vector3.zero;
            model.transform.position = feet + new Vector3(offset.x, offset.y, offset.z);
        }

        /// <summary>Sets the layer on a whole hierarchy.</summary>
        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }
}
