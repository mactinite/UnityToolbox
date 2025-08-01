using UnityEngine;

namespace mactinite.ToolboxCommons
{
    /// <summary>
    /// Inherit from this base class to create a singleton.
    /// e.g. public class MyClassName : Singleton<MyClassName> {}
    /// </summary>
    public class SingletonMonobehavior<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        // Check to see if we're about to be destroyed.
        private static bool m_ShuttingDown = false;
        private static object m_Lock = new object();
        private static T m_Instance;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_ShuttingDown)
                {
                    Debug.LogWarning(
                        "[Singleton] Instance '"
                            + typeof(T)
                            + "' already destroyed. Returning null."
                    );
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        // Search for existing instance.
                        m_Instance = (T)FindFirstObjectByType(typeof(T));

                        // Create new instance if one doesn't already exist.
                        if (m_Instance == null)
                        {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            m_Instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";
                        }
                    }

                    return m_Instance;
                }
            }
        }

        public static bool InstanceExists()
        {
            return m_Instance != null;
        }

        private void OnApplicationQuit()
        {
            if (m_Instance == this)
                m_ShuttingDown = true;
        }

        private void Awake()
        {
            if (m_Instance == null && m_ShuttingDown)
            {
                Debug.LogWarning(
                    "Instance was shutdown, but a new instance was created, rebooting instance"
                );
                m_ShuttingDown = false;
            }
        }

        private void OnDestroy()
        {
            if (m_Instance == this)
            {
                m_ShuttingDown = true;
                m_Instance = null;
            }
        }
    }
}
