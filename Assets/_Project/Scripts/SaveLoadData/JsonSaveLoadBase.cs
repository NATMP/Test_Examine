using System.IO;

using UnityEngine;

namespace NATMP.Utilities
{
    public abstract class JsonSaveLoadBase : ISaveManager
    {
        protected virtual string FileName => $"{GetType().Name}.json";
        protected string FilePath => Path.Combine(Application.persistentDataPath , FileName);

        protected virtual bool IsEncryptedModule => true;

        public virtual void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(this , true);
                if (IsEncryptedModule)
                {
                    json = EncryptionUtility.Encrypt(json);
                }
                File.WriteAllText(FilePath , json);
                UnityLogger.Log($"<color=green>[Save Success]</color> {GetType().Name}");
            }
            catch (System.Exception e)
            {
                UnityLogger.LogError($"[Save Error] {GetType().Name}: {e.Message}");
            }
        }

        public virtual void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;

                string content = File.ReadAllText(FilePath);

                if (!string.IsNullOrEmpty(content) && !content.Trim().StartsWith("{"))
                {
                    content = EncryptionUtility.Decrypt(content);
                }

                JsonUtility.FromJsonOverwrite(content , this);
                UnityLogger.Log($"<color=green>[Load Success]</color> {GetType().Name}");
            }
            catch (System.Exception e)
            {
                UnityLogger.LogError($"[Load Error] {GetType().Name}: {e.Message}");
            }
        }
    }
}