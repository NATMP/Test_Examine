using System;
using System.IO;
using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// File manual: magic "NMAP", version, width, height, [v2: bugX bugY], rồi width×height byte (1 walkable, 0 tường) hàng y=1..H, x=1..W.
    /// v1: bug spawn luôn (2,2). v2: bugX, bugY int32 sau height (ô nội thất, 1-based).
    /// </summary>
    public static class MazeManualLayoutBinary
    {
        private static readonly byte[] MagicBytes = { (byte)'N', (byte)'M', (byte)'A', (byte)'P' };

        public const int Version1 = 1;
        public const int Version2 = 2;

        private const int HeaderSizeV1 = 4 + 4 + 4 + 4;
        private const int HeaderSizeV2 = HeaderSizeV1 + 4 + 4;

        public static Vector2Int SanitizeBugStart(int x, int y, int width, int height)
        {
            if (width < 3 || height < 3)
                return new Vector2Int(2, 2);
            if (x < 2 || x >= width || y < 2 || y >= height)
                return new Vector2Int(2, 2);
            return new Vector2Int(x, y);
        }

        public static bool TryLoadFromFile(string absolutePath, int expectedWidth, int expectedHeight, out bool[,] walkable, out Vector2Int bugStart)
        {
            walkable = null;
            bugStart = new Vector2Int(2, 2);
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return false;

            try
            {
                var bytes = File.ReadAllBytes(absolutePath);
                return TryParse(bytes, expectedWidth, expectedHeight, out walkable, out bugStart);
            }
            catch (Exception e)
            {
                UnityLogger.LogWarning($"[MazeManualLayoutBinary] Load failed: {absolutePath}\n{e.Message}");
                return false;
            }
        }

        public static bool TryParse(byte[] bytes, int expectedWidth, int expectedHeight, out bool[,] walkable, out Vector2Int bugStart)
        {
            walkable = null;
            bugStart = new Vector2Int(2, 2);
            if (bytes == null || bytes.Length < HeaderSizeV1)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (bytes[i] != MagicBytes[i])
                    return false;
            }

            int offset = 4;
            int version = ReadInt32(bytes, ref offset);
            if (version != Version1 && version != Version2)
                return false;

            int w = ReadInt32(bytes, ref offset);
            int h = ReadInt32(bytes, ref offset);
            if (w != expectedWidth || h != expectedHeight)
            {
                UnityLogger.LogWarning($"[MazeManualLayoutBinary] Kích thước file {w}x{h} không khớp config {expectedWidth}x{expectedHeight}.");
                return false;
            }

            if (version == Version2)
            {
                if (bytes.Length < HeaderSizeV2)
                    return false;
                int bx = ReadInt32(bytes, ref offset);
                int by = ReadInt32(bytes, ref offset);
                bugStart = SanitizeBugStart(bx, by, w, h);
            }

            int need = offset + w * h;
            if (bytes.Length < need)
                return false;

            walkable = new bool[w + 1, h + 1];
            InGameMazeMazeGenerator.StampPerimeterWalls(walkable, w, h);

            for (int y = 1; y <= h; y++)
            {
                for (int x = 1; x <= w; x++)
                {
                    byte b = bytes[offset++];
                    walkable[x, y] = b != 0;
                }
            }

            return true;
        }

        private static int ReadInt32(byte[] bytes, ref int offset)
        {
            int v = BitConverter.ToInt32(bytes, offset);
            offset += 4;
            return v;
        }

#if UNITY_EDITOR
        public static void WriteToFile(string absolutePath, bool[,] walkable, int width, int height, Vector2Int bugStart)
        {
            if (walkable == null)
                throw new ArgumentNullException(nameof(walkable));

            bugStart = SanitizeBugStart(bugStart.x, bugStart.y, width, height);

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? ".");

            using var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            for (int i = 0; i < 4; i++)
                bw.Write(MagicBytes[i]);
            bw.Write(Version2);
            bw.Write(width);
            bw.Write(height);
            bw.Write(bugStart.x);
            bw.Write(bugStart.y);
            for (int y = 1; y <= height; y++)
            for (int x = 1; x <= width; x++)
                bw.Write((byte)(walkable[x, y] ? 1 : 0));
        }
#endif
    }
}
