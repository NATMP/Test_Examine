using UnityEngine;

namespace NATMP.Utilities.DesignPatterns
{
    /// <summary>
    /// Persistent singleton pattern that survives scene changes.
    /// Automatically calls DontDestroyOnLoad on the GameObject.
    /// Use this for managers that need to persist across scenes.
    /// </summary>
    /// <typeparam name="T">The type that inherits from this singleton</typeparam>
    /// <remarks>
    /// Usage: public class GameManager : PersistentSingleton&lt;GameManager&gt; { }
    /// Access: GameManager.Instance
    /// </remarks>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObject = new();
        private static bool isApplicationQuitting = false;

        /// <summary>
        /// Gets the persistent singleton instance. Creates one if it doesn't exist.
        /// The instance persists across scene loads.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    UnityLogger.LogWarning($"[PersistentSingleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new($"[PersistentSingleton] {typeof(T).Name}");
                            instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);

                            //UnityLogger.Log($"[PersistentSingleton] Created new instance of {typeof(T).Name}");
                        }
                        else
                        {
                            DontDestroyOnLoad(instance.gameObject);
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
                OnInitialize();
            }
            else if (instance != this)
            {
                UnityLogger.LogWarning($"[PersistentSingleton] Duplicate instance of {typeof(T).Name} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called once when the singleton is first initialized.
        /// Override this instead of Awake for initialization logic.
        /// </summary>
        protected virtual void OnInitialize()
        {
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
