using System.Collections.Generic;
using UnityEngine;
using NATMP.Gameplay.Maze;

public static class InGameMazeMazeGenerator
{
    /// <summary>
    /// Toàn bộ biên ma trận (x=1, x=width, y=1, y=height) luôn là tường — khớp maze có khung kín.
    /// </summary>
    public static void StampPerimeterWalls(bool[,] walkable, int width, int height)
    {
        for (int x = 1; x <= width; x++)
        {
            walkable[x, 1] = false;
            walkable[x, height] = false;
        }

        for (int y = 1; y <= height; y++)
        {
            walkable[1, y] = false;
            walkable[width, y] = false;
        }
    }

    /// <summary>Ô xuất phát bug luôn là sàn (sau StampPerimeter hoặc chỉnh tay lưới).</summary>
    public static void EnsureBugCellWalkable(bool[,] walkable, int width, int height, Vector2Int bugStart)
    {
        if (walkable == null)
            return;
        if (bugStart.x < 1 || bugStart.x > width || bugStart.y < 1 || bugStart.y > height)
            return;
        walkable[bugStart.x, bugStart.y] = true;
    }

    /// <summary>
    /// Nội bộ (không gồm biên): thứ tự ngẫu nhiên để tường phân bố đều, trừ ô bug.
    /// </summary>
    private static List<Vector2Int> BuildInteriorCellsShuffled(int width, int height, Vector2Int bugStart, System.Random rng)
    {
        var list = new List<Vector2Int>();
        for (int x = 2; x < width; x++)
        for (int y = 2; y < height; y++)
        {
            if (x == bugStart.x && y == bugStart.y)
                continue;
            list.Add(new Vector2Int(x, y));
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    /// <summary>
    /// Bắt đầu toàn bộ nội bộ là sàn, rồi thử đặt tường theo wallChance; chỉ giữ tường nếu
    /// mọi ô walkable vẫn cùng một miền liên thông với bug (đúng nghĩa maze một khối).
    /// </summary>
    private static void PlaceInteriorWallsPreservingConnectivity(
        bool[,] walkable,
        int width,
        int height,
        Vector2Int bugStart,
        double wallChance,
        System.Random rng,
        int[,] visitStamp,
        ref int visitStampGeneration,
        Queue<Vector2Int> bfsQueue)
    {
        var order = BuildInteriorCellsShuffled(width, height, bugStart, rng);
        int totalWalkable = InGameMazeGridPathFinder.CountWalkableCells(walkable, width, height);
        foreach (var c in order)
        {
            if (rng.NextDouble() >= wallChance)
                continue;
            if (!walkable[c.x, c.y])
                continue;

            walkable[c.x, c.y] = false;
            int reachable = InGameMazeGridPathFinder.CountReachableWalkableCells(
                walkable, bugStart, width, height, visitStamp, ref visitStampGeneration, bfsQueue);
            if (reachable != totalWalkable - 1)
                walkable[c.x, c.y] = true;
            else
                totalWalkable--;
        }
    }

    public static bool[,] Generate(MazeGenerationParameters parameters, Vector2Int bugStart, int mazeSeed)
    {
        int width = parameters.Width;
        int height = parameters.Height;
        var walkable = new bool[width + 1, height + 1];
        var rng = new System.Random(mazeSeed);
        double wallChance = parameters.WallChance;
        int minPath = parameters.MinPathLength;
        int maxAttempts = parameters.MaxGenerationAttempts;

        var farInnerCorner = new Vector2Int(width - 1, height - 1);
        var visitStamp = new int[width + 1, height + 1];
        var bfsQueue = new Queue<Vector2Int>(Mathf.Max(32, (width - 2) * (height - 2) + 4));
        int visitStampGeneration = 0;

        int attempts = 0;
        while (true)
        {
            attempts++;

            for (int x = 1; x <= width; x++)
                for (int y = 1; y <= height; y++)
                    walkable[x, y] = true;

            StampPerimeterWalls(walkable, width, height);
            walkable[bugStart.x, bugStart.y] = true;

            PlaceInteriorWallsPreservingConnectivity(
                walkable, width, height, bugStart, wallChance, rng, visitStamp, ref visitStampGeneration, bfsQueue);
            EnsureBugCellWalkable(walkable, width, height, bugStart);

            if (InGameMazeGridPathFinder.TryFindPath(walkable, bugStart, farInnerCorner, width, height, out var testPath)
                && testPath != null
                && testPath.Count >= minPath)
            {
                break;
            }

            if (attempts >= maxAttempts)
            {
                for (int x = 1; x <= width; x++)
                    for (int y = 1; y <= height; y++)
                        walkable[x, y] = true;
                StampPerimeterWalls(walkable, width, height);
                EnsureBugCellWalkable(walkable, width, height, bugStart);
                break;
            }
        }

        return walkable;
    }
}
