using UnityEngine;

namespace toolbox.Singleton
{
    /// <summary>
    /// Persistent singleton that destroys all other instances of the same type that were created before it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RegulatorSingletonBehaviour<T> : SingletonBehaviour<T> where T : Component
    {
        public float InitializationTime { get; private set; }
        protected override void InitializeSingleton() {
            if (!Application.isPlaying) return;
            InitializationTime = Time.time;
            DontDestroyOnLoad(gameObject);

            T[] oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (T old in oldInstances) {
                if (old.GetComponent<RegulatorSingletonBehaviour<T>>().InitializationTime < InitializationTime) {
                    Destroy(old.gameObject);
                }
            }

            if (instance == null) {
                instance = this as T;
            }
        }
    }
}