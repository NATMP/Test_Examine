using System.Collections.Generic;
using UnityEngine;

public static class InGameMazeGridPathFinder
{
    public static int CountWalkableCells(bool[,] walkable, int width, int height)
    {
        int n = 0;
        for (int x = 1; x <= width; x++)
        for (int y = 1; y <= height; y++)
        {
            if (walkable[x, y])
                n++;
        }

        return n;
    }

    public static int CountReachableWalkableCells(bool[,] walkable, Vector2Int start, int width, int height)
    {
        if (!IsWalkable(walkable, start, width, height))
            return 0;

        var seen = new bool[width + 1, height + 1];
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        seen[start.x, start.y] = true;
        int count = 0;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            count++;
            foreach (var n in Neighbors4(c))
            {
                if (!IsWalkable(walkable, n, width, height) || seen[n.x, n.y])
                    continue;
                seen[n.x, n.y] = true;
                q.Enqueue(n);
            }
        }

        return count;
    }

    /// <summary>
    /// BFS: số ô trên đường ngắn nhất từ <paramref name="start"/> (gồm start), trùng <c>path.Count</c> của <see cref="TryFindPath"/>.
    /// Không walkable hoặc không tới được: -1.
    /// </summary>
    public static void FillShortestPathCellCounts(
        bool[,] walkable,
        Vector2Int start,
        int width,
        int height,
        int[,] cellCountFromStart)
    {
        for (int x = 1; x <= width; x++)
        for (int y = 1; y <= height; y++)
            cellCountFromStart[x, y] = -1;

        if (!IsWalkable(walkable, start, width, height))
            return;

        var q = new Queue<Vector2Int>();
        cellCountFromStart[start.x, start.y] = 1;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d = cellCountFromStart[c.x, c.y];
            foreach (var n in Neighbors4(c))
            {
                if (!IsWalkable(walkable, n, width, height))
                    continue;
                if (cellCountFromStart[n.x, n.y] >= 0)
                    continue;
                cellCountFromStart[n.x, n.y] = d + 1;
                q.Enqueue(n);
            }
        }
    }

    /// <summary>Mọi ô walkable đều liên thông 4-hướng với <paramref name="start"/>.</summary>
    public static bool AllWalkableCellsReachableFrom(bool[,] walkable, Vector2Int start, int width, int height)
    {
        if (!IsWalkable(walkable, start, width, height))
            return false;
        return CountReachableWalkableCells(walkable, start, width, height) == CountWalkableCells(walkable, width, height);
    }

    // BFS trên lưới 4 hướng.
    // walkable[x,y] chỉ dùng index 1..width, 1..height (mảng thường có kích thước [width+1,height+1]).
    public static bool TryFindPath(
        bool[,] walkable,
        Vector2Int start,
        Vector2Int goal,
        int width,
        int height,
        out List<Vector2Int> path)
    {
        path = null;
        if (!IsWalkable(walkable, start, width, height) || !IsWalkable(walkable, goal, width, height))
            return false;

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        cameFrom[start] = start;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == goal)
                break;

            foreach (var n in Neighbors4(cur))
            {
                if (!IsWalkable(walkable, n, width, height)) continue;
                if (cameFrom.ContainsKey(n)) continue;

                cameFrom[n] = cur;
                q.Enqueue(n);
            }
        }

        if (!cameFrom.ContainsKey(goal))
            return false;

        var rev = new List<Vector2Int>();
        var c = goal;
        while (c != start)
        {
            rev.Add(c);
            c = cameFrom[c];
        }
        rev.Add(start);
        rev.Reverse();
        path = rev;
        return true;
    }

    private static bool IsWalkable(bool[,] walkable, Vector2Int c, int width, int height)
    {
        if (c.x < 1 || c.x > width || c.y < 1 || c.y > height)
            return false;
        return walkable[c.x, c.y];
    }

    private static IEnumerable<Vector2Int> Neighbors4(Vector2Int c)
    {
        yield return new Vector2Int(c.x + 1, c.y);
        yield return new Vector2Int(c.x - 1, c.y);
        yield return new Vector2Int(c.x, c.y + 1);
        yield return new Vector2Int(c.x, c.y - 1);
    }
}

