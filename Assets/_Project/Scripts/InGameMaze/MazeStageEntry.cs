using System;
using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Một dòng trong <see cref="MazeStageRoster"/>. Procedural: <see cref="ProceduralSeed"/> cho generator.
    /// Manual: chỉ qua Addressables (<see cref="ManualAddressableAddress"/>); seed dùng cho RNG đích.
    /// </summary>
    [Serializable]
    public class MazeStageEntry
    {
        [Tooltip("Procedural: seed cho InGameMazeMazeGenerator. Manual: hạt cho random đích / session.")]
        public int ProceduralSeed;

        public MazeStageContentSource Source = MazeStageContentSource.Procedural;

        [Tooltip("Địa chỉ Addressables (vd. Addressables/MazeMap/map_0001) trỏ tới TextAsset .bytes.")]
        public string ManualAddressableAddress = "";

        [Tooltip("Tăng khi đổi thuật toán sinh maze.")]
        public int GeneratorVersion = 1;

        [Tooltip("Bật để cố định ô đích (phải walkable).")]
        public bool OverrideTarget;

        public Vector2Int TargetOverride;

        public static MazeStageEntry CreateDefaultForStage(int stageIndex)
        {
            return new MazeStageEntry
            {
                ProceduralSeed = MazeGameplaySeed.DeterministicFromStageIndex(stageIndex),
                Source = MazeStageContentSource.Procedural,
                ManualAddressableAddress = "",
                GeneratorVersion = 1,
                OverrideTarget = false,
                TargetOverride = default
            };
        }
    }
}
