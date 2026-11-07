using System.Collections;
using UnityEngine;

namespace CoinRush.UI
{
    /// <summary>Hidden helper object to run coroutines from static code.</summary>
    public class MonoBehaviourRunner : MonoBehaviour
    {
        private static MonoBehaviourRunner instance;

        public static Coroutine Run(IEnumerator routine)
        {
            if (instance == null)
            {
                instance = new GameObject("[UITweenRunner]").AddComponent<MonoBehaviourRunner>();
                instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
            return instance.StartCoroutine(routine);
        }
    }
}
