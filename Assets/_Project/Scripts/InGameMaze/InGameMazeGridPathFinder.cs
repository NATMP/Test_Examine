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

    /// <summary>BFS đếm ô tới được — phiên bản không GC: dùng chung <paramref name="visitStamp"/> + <paramref name="queue"/>.</summary>
    public static int CountReachableWalkableCells(
        bool[,] walkable,
        Vector2Int start,
        int width,
        int height,
        int[,] visitStamp,
        ref int stampGeneration,
        Queue<Vector2Int> queue)
    {
        if (!IsWalkable(walkable, start, width, height))
            return 0;

        stampGeneration++;
        if (stampGeneration <= 0 || stampGeneration == int.MaxValue)
        {
            System.Array.Clear(visitStamp, 0, visitStamp.Length);
            stampGeneration = 1;
        }

        int tag = stampGeneration;
        queue.Clear();
        queue.Enqueue(start);
        visitStamp[start.x, start.y] = tag;
        int count = 0;

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            count++;
            TryEnqueueWalkable(walkable, visitStamp, queue, width, height, tag, c.x + 1, c.y);
            TryEnqueueWalkable(walkable, visitStamp, queue, width, height, tag, c.x - 1, c.y);
            TryEnqueueWalkable(walkable, visitStamp, queue, width, height, tag, c.x, c.y + 1);
            TryEnqueueWalkable(walkable, visitStamp, queue, width, height, tag, c.x, c.y - 1);
        }

        return count;
    }

    /// <summary>BFS đếm (cấp phát tạm — chỉ dùng khi gọi thưa, vd. validate).</summary>
    public static int CountReachableWalkableCells(bool[,] walkable, Vector2Int start, int width, int height)
    {
        var stamp = new int[width + 1, height + 1];
        int gen = 0;
        var q = new Queue<Vector2Int>(Mathf.Max(16, width * height / 4));
        return CountReachableWalkableCells(walkable, start, width, height, stamp, ref gen, q);
    }

    /// <summary>
    /// BFS: số ô trên đường ngắn nhất từ <paramref name="start"/> (gồm start), trùng <c>path.Count</c> của <see cref="TryFindPath"/>.
    /// Không walkable hoặc không tới được: -1. Truyền sẵn <paramref name="queue"/> để tránh cấp phát mỗi lần gọi.
    /// </summary>
    public static void FillShortestPathCellCounts(
        bool[,] walkable,
        Vector2Int start,
        int width,
        int height,
        int[,] cellCountFromStart,
        Queue<Vector2Int> queue)
    {
        for (int x = 1; x <= width; x++)
        for (int y = 1; y <= height; y++)
            cellCountFromStart[x, y] = -1;

        if (!IsWalkable(walkable, start, width, height))
            return;

        queue.Clear();
        cellCountFromStart[start.x, start.y] = 1;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            int d = cellCountFromStart[c.x, c.y];
            TryRelaxShortest(walkable, cellCountFromStart, queue, width, height, d, c.x + 1, c.y);
            TryRelaxShortest(walkable, cellCountFromStart, queue, width, height, d, c.x - 1, c.y);
            TryRelaxShortest(walkable, cellCountFromStart, queue, width, height, d, c.x, c.y + 1);
            TryRelaxShortest(walkable, cellCountFromStart, queue, width, height, d, c.x, c.y - 1);
        }
    }

    /// <summary>Overload cấp phát queue nội bộ (ít dùng).</summary>
    public static void FillShortestPathCellCounts(
        bool[,] walkable,
        Vector2Int start,
        int width,
        int height,
        int[,] cellCountFromStart)
    {
        var q = new Queue<Vector2Int>(Mathf.Max(16, width * height / 4));
        FillShortestPathCellCounts(walkable, start, width, height, cellCountFromStart, q);
    }

    /// <summary>Mọi ô walkable đều liên thông 4-hướng với <paramref name="start"/>.</summary>
    public static bool AllWalkableCellsReachableFrom(bool[,] walkable, Vector2Int start, int width, int height)
    {
        if (!IsWalkable(walkable, start, width, height))
            return false;
        return CountReachableWalkableCells(walkable, start, width, height) == CountWalkableCells(walkable, width, height);
    }

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

        int cap = Mathf.Max(16, width * height / 4);
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>(cap);
        var q = new Queue<Vector2Int>(cap);
        q.Enqueue(start);
        cameFrom[start] = start;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == goal)
                break;

            TryCameStep(walkable, cameFrom, q, width, height, cur, cur.x + 1, cur.y);
            TryCameStep(walkable, cameFrom, q, width, height, cur, cur.x - 1, cur.y);
            TryCameStep(walkable, cameFrom, q, width, height, cur, cur.x, cur.y + 1);
            TryCameStep(walkable, cameFrom, q, width, height, cur, cur.x, cur.y - 1);
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

    private static void TryEnqueueWalkable(
        bool[,] walkable,
        int[,] visitStamp,
        Queue<Vector2Int> q,
        int width,
        int height,
        int tag,
        int x,
        int y)
    {
        if (x < 1 || x > width || y < 1 || y > height)
            return;
        if (!walkable[x, y] || visitStamp[x, y] == tag)
            return;
        visitStamp[x, y] = tag;
        q.Enqueue(new Vector2Int(x, y));
    }

    private static void TryRelaxShortest(
        bool[,] walkable,
        int[,] cellCountFromStart,
        Queue<Vector2Int> q,
        int width,
        int height,
        int d,
        int x,
        int y)
    {
        if (x < 1 || x > width || y < 1 || y > height)
            return;
        if (!walkable[x, y])
            return;
        if (cellCountFromStart[x, y] >= 0)
            return;
        cellCountFromStart[x, y] = d + 1;
        q.Enqueue(new Vector2Int(x, y));
    }

    private static void TryCameStep(
        bool[,] walkable,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Queue<Vector2Int> q,
        int width,
        int height,
        Vector2Int cur,
        int x,
        int y)
    {
        if (x < 1 || x > width || y < 1 || y > height)
            return;
        if (!walkable[x, y])
            return;
        var n = new Vector2Int(x, y);
        if (cameFrom.ContainsKey(n))
            return;
        cameFrom[n] = cur;
        q.Enqueue(n);
    }

    private static bool IsWalkable(bool[,] walkable, Vector2Int c, int width, int height)
    {
        if (c.x < 1 || c.x > width || c.y < 1 || c.y > height)
            return false;
        return walkable[c.x, c.y];
    }
}
