using System;
using System.Collections.Generic;

using NATMP.Utilities.GameUnitSystem;

using UnityEngine;

namespace NATMP.Utilities.GamePlaySystem
{
    [Serializable]
    public class PlayerDataJson : ISaveManager
    {
        [SerializeField] private CurrenciesData _currenciesData = new();
        [SerializeField] private PlayerTalentCardData _talentCardData = new();
        [SerializeField] private TutorialDataJson _tutorialData = new();
        [SerializeField] private PlayerMapLevelData _mapLevelData = new();
        private readonly List<ISaveManager> _subModules = new();

        public CurrenciesData CurrenciesData => _currenciesData;
        public TutorialDataJson TutorialData => _tutorialData;
        public PlayerTalentCardData TalentCardData => _talentCardData;
        public PlayerMapLevelData MapLevelData => _mapLevelData;
        public PlayerDataJson()
        {
            // Khởi tạo các module con
            _currenciesData = new CurrenciesData();
            _tutorialData = new TutorialDataJson();
            _talentCardData = new PlayerTalentCardData();
            _mapLevelData = new PlayerMapLevelData();

            // Đưa vào danh sách để quản lý vòng đời chung
            _subModules.Add(_currenciesData);
            _subModules.Add(_tutorialData);
            _subModules.Add(_talentCardData);
            _subModules.Add(_mapLevelData);
        }
        public void Load()
        {
            foreach (var module in _subModules) module.Load();
        }

        public void Save()
        {
            SaveAll();
        }
        public void SaveAll()
        {
            foreach (var module in _subModules) module.Save();
        }
    }
    [Serializable]
    public class PlayerMapLevelData : JsonSaveLoadBase
    {
        [SerializeField] private int _level = 1;
        protected override bool IsEncryptedModule => false;
        public int Level => _level;
        public void SetLevel(int level)
        {
            _level = level;
            Save();
        }
    }
    [Serializable]
    public class CurrenciesData : JsonSaveLoadBase
    {
        [SerializeField] private long _coin;
        [SerializeField] private long _gem;
        public static EventBus<CoinChangeEventData> OnCoinChange = new();
        protected override bool IsEncryptedModule => false;
        public long Coin => _coin;
        public long Gem => _gem;

        public void SetCoin(long amount)
        {
            OnCoinChange?.Publish(new CoinChangeEventData() { CurrentCoin = _coin , NewCoin = amount });
            _coin = amount;
            Save();
        }
        public void AddCoin(long amount)
        {
            SetCoin(_coin + amount);
        }
        public void SubtractCoin(long amount)
        {
            SetCoin(_coin - amount);
        }
        public void SetGem(long amount)
        {
            _gem = amount;
            Save();
        }
    }
    [Serializable]
    public struct CoinChangeEventData
    {
        public long CurrentCoin;
        public long NewCoin;
    }
    [Serializable]
    public class PlayerTalentCardData : JsonSaveLoadBase
    {
        [SerializeField] private List<TalentCardData> _talentCards = new();
        override protected bool IsEncryptedModule => false;
        public List<TalentCardData> GetTalentCards()
        {
            return new List<TalentCardData>(_talentCards);
        }

        /// <summary>
        /// Get specific talent card data by ID.
        /// </summary>
        public TalentCardData GetTalentCard(string cardID)
        {
            return _talentCards.Find(card => card.IsUnlocked && card.CardID == cardID);
        }
        public TalentCardData GetTalentCard(StatType statType)
        {
            return _talentCards.Find(card => card.IsUnlocked && card.StatType == statType);
        }
        public List<TalentCardData> GetAllTalentCards(PerkTarget target)
        {
            return _talentCards.FindAll(card => card.IsUnlocked && card.TargetApply == target);
        }

        /// <summary>
        /// Add or update talent card data.
        /// </summary>
        public void AddOrUpdateTalentCard(TalentCardData cardData)
        {
            var existingCard = GetTalentCard(cardData.CardID);
            if (existingCard != null)
            {
                existingCard.IsUnlocked = cardData.IsUnlocked;
                existingCard.Level = cardData.Level;
                existingCard.Fragments = cardData.Fragments;
            }
            else
            {
                _talentCards.Add(cardData);
            }
            Save();
        }
    }
    [Serializable]
    public class TalentCardData
    {
        public string CardID; // Unique identifier for the card
        public bool IsUnlocked; // Whether the card is unlocked
        public int Level; // Current level of the card
        public int Fragments; // Number of fragments owned
        public PerkTarget TargetApply;
        public StatType StatType;

        public TalentCardData(string cardID , bool isUnlocked , int level , int fragments , PerkTarget target , StatType statType)
        {
            CardID = cardID;
            IsUnlocked = isUnlocked;
            Level = level;
            Fragments = fragments;
            TargetApply = target;
            StatType = statType;
        }
    }
    [Serializable]
    public class TutorialDataJson : JsonSaveLoadBase
    {
        // Danh sách lưu xuống JSON (Unity Serialization)
        public List<TutorialProgressEntry> ProgressEntries = new();

        // Runtime Cache: Giúp Manager check trạng thái với độ phức tạp O(1)
        private Dictionary<string , TutorialProgressEntry> _cache = new();
        protected override bool IsEncryptedModule => false;

        public override void Load()
        {
            base.Load();
            InitializeCache();
        }
        /// <summary>
        /// Gọi hàm này sau khi Load JSON để nạp vào Cache.
        /// </summary>
        public void InitializeCache()
        {
            _cache.Clear();
            foreach (var entry in ProgressEntries)
            {
                if (!string.IsNullOrEmpty(entry.IdentityKey))
                    _cache[entry.IdentityKey] = entry;
            }
        }

        public TutorialProgressEntry GetOrAddProgress(string identityKey)
        {
            if (_cache.TryGetValue(identityKey , out var entry))
                return entry;

            var newEntry = new TutorialProgressEntry { IdentityKey = identityKey };
            ProgressEntries.Add(newEntry);
            _cache[identityKey] = newEntry;

            return newEntry;
        }
        public void SetProgress(string identityKey , int lastStepIndex , bool isCompleted)
        {
            var entry = GetOrAddProgress(identityKey);
            entry.LastStepIndex = lastStepIndex;
            entry.IsCompleted = isCompleted;
            Save();
        }
        public int GetProgress(string identityKey)
        {
            if (_cache.TryGetValue(identityKey , out var entry))
            {
                return entry.LastStepIndex; // Trả về bước cuối cùng đã hoàn thành
            }

            return -1; // Trả về -1 nếu không tìm thấy tutorial
        }

        /// <summary>
        /// Kiểm tra xem tutorial đã hoàn thành hay chưa.
        /// </summary>
        public bool IsTutorialCompleted(string identityKey)
        {
            if (_cache.TryGetValue(identityKey , out var entry))
            {
                return entry.IsCompleted; // Trả về trạng thái hoàn thành của tutorial
            }

            return false; // Trả về false nếu không tìm thấy tutorial
        }
    }

    [Serializable]
    public class TutorialProgressEntry
    {
        public string IdentityKey;      // Tên của TutorialIdentity Asset
        public int LastStepIndex = -1;  // Chỉ số bước vừa hoàn thành
        public bool IsCompleted = false; // Đã xong toàn bộ Tutorial này chưa
    }
}
