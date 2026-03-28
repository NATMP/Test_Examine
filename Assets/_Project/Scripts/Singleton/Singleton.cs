using UnityEngine;

namespace NATMP.Utilities.DesignPatterns
{
    /// <summary>
    /// Generic singleton pattern for MonoBehaviour classes in Unity.
    /// Ensures only one instance exists and provides global access.
    /// Thread-safe implementation with lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type that inherits from this singleton</typeparam>
    /// <remarks>
    /// Usage: public class MyManager : Singleton&lt;MyManager&gt; { }
    /// Access: MyManager.Instance
    /// </remarks>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObject = new object();
        private static bool isApplicationQuitting = false;

        /// <summary>
        /// Gets the singleton instance. Creates one if it doesn't exist.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    UnityLogger.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                            instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                            
                            UnityLogger.Log($"[Singleton] Created new instance of {typeof(T).Name}");
                        }
                    }

                    return instance;
                }
            }
        }

        /// <summary>
        /// Checks if an instance exists without creating one.
        /// </summary>
        public static bool HasInstance => instance != null;

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                UnityLogger.LogWarning($"[Singleton] Duplicate instance of {typeof(T).Name} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
