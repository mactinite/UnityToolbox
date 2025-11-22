using UnityEngine;

namespace toolbox.Singleton
{
    /// <summary>
    /// Inherit from this base class to create a singleton.
    /// </summary>
    public class SingletonBehaviour<T> : MonoBehaviour
        where T : Component
    {
        protected static T instance;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // Search for existing instance.
                    instance = (T)FindFirstObjectByType(typeof(T));

                    // Create new instance if one doesn't already exist.
                    if (instance == null)
                    {
                        // Need to create a new GameObject to attach the singleton to.
                        var singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
                return;

            instance = this as T;
        }
    }
}
