using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>Kết quả resolve một stage: lưới, seed RNG, bug/target tùy chọn.</summary>
    public readonly struct MazeStageResolveResult
    {
        public bool[,] Walkable { get; }
        public int ContentSeed { get; }
        public Vector2Int BugStart { get; }
        public bool HasFixedTarget { get; }
        public Vector2Int FixedTarget { get; }

        public MazeStageResolveResult(
            bool[,] walkable,
            int contentSeed,
            Vector2Int bugStart,
            bool hasFixedTarget,
            Vector2Int fixedTarget)
        {
            Walkable = walkable;
            ContentSeed = contentSeed;
            BugStart = bugStart;
            HasFixedTarget = hasFixedTarget;
            FixedTarget = fixedTarget;
        }
    }
}
