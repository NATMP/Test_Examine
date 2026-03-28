using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Kiểm tra lưới walkable (liên thông, bug trên sàn, đường tới góc xa ≥ min path) — DRY với generator.
    /// </summary>
    public static class MazeLayoutValidator
    {
        public static bool TryValidate(
            bool[,] walkable,
            int width,
            int height,
            Vector2Int bugStart,
            int minPathLength,
            out string errorMessage)
        {
            errorMessage = null;
            if (walkable == null)
            {
                errorMessage = "walkable null.";
                return false;
            }

            if (bugStart.x < 1 || bugStart.x > width || bugStart.y < 1 || bugStart.y > height)
            {
                errorMessage = $"BugStart {bugStart} ngoài lưới 1..{width} × 1..{height}.";
                return false;
            }

            if (!walkable[bugStart.x, bugStart.y])
            {
                errorMessage = $"Ô bug {bugStart} không walkable.";
                return false;
            }

            if (!InGameMazeGridPathFinder.AllWalkableCellsReachableFrom(walkable, bugStart, width, height))
            {
                errorMessage = "Có ô walkable không liên thông với bug.";
                return false;
            }

            var farCorner = new Vector2Int(width - 1, height - 1);
            if (!InGameMazeGridPathFinder.TryFindPath(walkable, bugStart, farCorner, width, height, out var path)
                || path == null
                || path.Count < minPathLength)
            {
                errorMessage = $"Đường tới góc ({width - 1},{height - 1}) không đạt MinPathLength ≥ {minPathLength} (generator dùng cùng rule).";
                return false;
            }

            return true;
        }

        public static bool IsTargetCellWalkable(bool[,] walkable, int width, int height, Vector2Int cell)
        {
            if (walkable == null || cell.x < 1 || cell.x > width || cell.y < 1 || cell.y > height)
                return false;
            return walkable[cell.x, cell.y];
        }
    }
}
