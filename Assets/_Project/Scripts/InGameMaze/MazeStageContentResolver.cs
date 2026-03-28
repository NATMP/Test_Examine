using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Gom seed/layout từ <see cref="MazeStageRoster"/> thành <see cref="MazeStageResolveResult"/>.
    /// </summary>
    public static class MazeStageContentResolver
    {
        public static MazeStageResolveResult Resolve(
            MazeStageRoster roster,
            int stageIndex,
            MazeGenerationParameters parameters)
        {
            var entry = GetEffectiveEntry(roster, stageIndex);
            int contentSeed = entry.ProceduralSeed != 0
                ? entry.ProceduralSeed
                : MazeGameplaySeed.DeterministicFromStageIndex(stageIndex);

            var defaultBug = new Vector2Int(2, 2);
            bool hasFixedTarget = false;
            Vector2Int fixedTarget = default;

            if (entry.Source == MazeStageContentSource.Manual)
            {
                if (TryLoadManual(entry, parameters.Width, parameters.Height, out var walkableManual, out var bugFromMap))
                {
                    if (entry.OverrideTarget
                        && MazeLayoutValidator.IsTargetCellWalkable(walkableManual, parameters.Width, parameters.Height, entry.TargetOverride))
                    {
                        hasFixedTarget = true;
                        fixedTarget = entry.TargetOverride;
                    }

                    return new MazeStageResolveResult(walkableManual, contentSeed, bugFromMap, hasFixedTarget, fixedTarget);
                }

                UnityLogger.LogWarning($"[MazeStageContentResolver] Stage {stageIndex}: manual không đọc được (Addressables), fallback procedural.");
            }

            var walkable = InGameMazeMazeGenerator.Generate(parameters, defaultBug, contentSeed);

            if (entry.OverrideTarget
                && MazeLayoutValidator.IsTargetCellWalkable(walkable, parameters.Width, parameters.Height, entry.TargetOverride))
            {
                hasFixedTarget = true;
                fixedTarget = entry.TargetOverride;
            }

            return new MazeStageResolveResult(walkable, contentSeed, defaultBug, hasFixedTarget, fixedTarget);
        }

        private static bool TryLoadManual(MazeStageEntry entry, int width, int height, out bool[,] walkable, out Vector2Int bugStart)
        {
            walkable = null;
            bugStart = new Vector2Int(2, 2);
            if (string.IsNullOrWhiteSpace(entry.ManualAddressableAddress))
                return false;

            return MazeManualLayoutAddressables.TryLoad(entry.ManualAddressableAddress, width, height, out walkable, out bugStart);
        }

        private static MazeStageEntry GetEffectiveEntry(MazeStageRoster roster, int stageIndex)
        {
            if (roster != null && roster.TryGetEntry(stageIndex, out var e) && e != null)
                return e;
            return MazeStageEntry.CreateDefaultForStage(stageIndex);
        }
    }
}
