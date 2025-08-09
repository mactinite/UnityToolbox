using UnityEngine;

namespace toolbox.Singleton
{
    /// <summary>
    /// Persistent singleton that destroys later instances of the same type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PersistentSingletonBehaviour<T> : SingletonBehaviour<T> where T : Component
    {
        public bool AutoUnparentOnAwake = true;

        protected override void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            if (AutoUnparentOnAwake) {
                transform.SetParent(null);
            }

            if (instance == null) {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            } else {
                if (instance != this) {
                    Destroy(gameObject);
                }
            }
        }
    }
}