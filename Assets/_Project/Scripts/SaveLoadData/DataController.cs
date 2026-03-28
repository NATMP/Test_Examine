using System;
using System.Collections.Generic;

using NATMP.Utilities.GamePlaySystem;

using UnityEngine;

namespace NATMP.Utilities
{
    public class DataController : MonoBehaviour
    {
        [SerializeField] private PlayerDataJson _playerData = new();
        private readonly Dictionary<Type , ISaveManager> _dataModules = new();

        public PlayerDataJson PlayerData => _playerData;

        public void Initialize()
        {
            _dataModules.Clear();
            RegisterModule(_playerData);
            LoadAll();
        }
        private void RegisterModule<T>(T module) where T : ISaveManager
        {
            _dataModules[typeof(T)] = module;
        }

        public T GetData<T>() where T : class, ISaveManager
        {
            if (_dataModules.TryGetValue(typeof(T) , out var module))
                return module as T;

            UnityLogger.LogError($"[DataController] Module {typeof(T).Name} chưa được đăng ký!");
            return null;
        }

        public void SaveAll()
        {
            foreach (var module in _dataModules.Values)
                module.Save();
            UnityLogger.Log("<b><color=white>[System]</color></b> Toàn bộ dữ liệu đã được lưu.");
        }

        public void LoadAll()
        {
            foreach (var module in _dataModules.Values)
                module.Load();
        }
    }
}