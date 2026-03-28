using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NATMP.Gameplay.Maze;

namespace NATMP.Utilities.GamePlaySystem
{
    [Serializable]
    public class PlayerDataJson : ISaveManager
    {
        [SerializeField] private PlayerMapLevelData _mapLevelData = new();
        private readonly List<ISaveManager> _subModules = new();

        public PlayerMapLevelData MapLevelData => _mapLevelData;

        public PlayerDataJson()
        {
            _mapLevelData = new PlayerMapLevelData();
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
        private const int TotalStageCount = 999;

        [SerializeField] private int _stageUnlocked = 1;
        [SerializeField] private List<StageData> _stages = new();

        protected override bool IsEncryptedModule => false;

        public int StageUnlocked => _stageUnlocked;
        public IReadOnlyList<StageData> Stages => _stages;

        public override void Load()
        {
            if (File.Exists(FilePath))
            {
                base.Load();
                EnsureValidData();
                return;
            }

            InitializeRandomMapData();
            Save();
        }

        public void ResetAllStages()
        {
            InitializeRandomMapData();
            Save();
        }

        public bool TryGetStage(int stageIndex, out StageData stage)
        {
            stage = null;
            if (_stages == null || stageIndex < 1 || stageIndex > _stages.Count)
                return false;
            var candidate = _stages[stageIndex - 1];
            if (candidate.StageIndex != stageIndex)
                return false;
            stage = candidate;
            return true;
        }

        private void EnsureValidData()
        {
            if (_stages == null || _stages.Count != TotalStageCount)
            {
                InitializeRandomMapData();
                Save();
                return;
            }

            if (FillMissingMazeSeeds())
                Save();
        }

        private bool FillMissingMazeSeeds()
        {
            bool dirty = false;
            for (int i = 0; i < _stages.Count; i++)
            {
                var s = _stages[i];
                if (s.MazeSeed != 0)
                    continue;
                _stages[i] = new StageData(s.StageIndex, s.IsUnlocked, s.StarCount, s.HasTutorialLabel, MazeGameplaySeed.DeterministicFromStageIndex(s.StageIndex));
                dirty = true;
            }
            return dirty;
        }

        private void InitializeRandomMapData()
        {
            _stageUnlocked = UnityEngine.Random.Range(1, TotalStageCount + 1);
            _stages = new List<StageData>(TotalStageCount);

            for (int stageIndex = 1; stageIndex <= TotalStageCount; stageIndex++)
            {
                bool isUnlocked = stageIndex < _stageUnlocked;
                int starCount = isUnlocked ? UnityEngine.Random.Range(1, 4) : 0;
                bool hasTutorialLabel = stageIndex == 1;
                int mazeSeed;
                do
                {
                    mazeSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                } while (mazeSeed == 0);

                _stages.Add(new StageData(stageIndex, isUnlocked, starCount, hasTutorialLabel, mazeSeed));
            }
        }
    }

    [Serializable]
    public class StageData
    {
        [SerializeField] private int _stageIndex;
        [SerializeField] private bool _isUnlocked;
        [SerializeField] private int _starCount;
        [SerializeField] private bool _hasTutorialLabel;
        [SerializeField] private int _mazeSeed;

        public int StageIndex => _stageIndex;
        public bool IsUnlocked => _isUnlocked;
        public int StarCount => _starCount;
        public bool HasTutorialLabel => _hasTutorialLabel;
        public int MazeSeed => _mazeSeed;

        public StageData(int stageIndex, bool isUnlocked, int starCount, bool hasTutorialLabel, int mazeSeed)
        {
            _stageIndex = stageIndex;
            _isUnlocked = isUnlocked;
            _starCount = starCount;
            _hasTutorialLabel = hasTutorialLabel;
            _mazeSeed = mazeSeed;
        }
    }
}
