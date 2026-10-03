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
        static bool applicationIsQuitting;

        static SingletonBehaviour()
        {
            Application.quitting += () => applicationIsQuitting = true;
        }

        /// <summary>
        /// True when a live instance is registered. Unlike <see cref="Instance"/>, never creates one.
        /// </summary>
        public static bool HasInstance => instance != null;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// Returns null (instead of resurrecting a new GameObject) once the instance has been destroyed
        /// during scene teardown or while the application is quitting, so OnDisable/OnDestroy code can
        /// safely use <c>Instance?.Foo()</c>.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                // A non-null reference that compares equal to null is a destroyed Unity object.
                var wasDestroyed = !ReferenceEquals(instance, null);

                // Search for existing instance (e.g. one in a newly loaded scene that hasn't run Awake yet).
                instance = (T)FindFirstObjectByType(typeof(T));
                if (instance != null)
                    return instance;

                if (wasDestroyed || applicationIsQuitting)
                    return null;

                // Create new instance if one doesn't already exist.
                var singletonObject = new GameObject();
                instance = singletonObject.AddComponent<T>();
                singletonObject.name = typeof(T).ToString() + " (Singleton)";

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
