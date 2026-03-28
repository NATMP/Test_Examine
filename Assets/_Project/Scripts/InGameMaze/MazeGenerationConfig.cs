using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// ScriptableObject: kích thước maze và tham số sinh tường / độ dài đường tối thiểu.
    /// </summary>
    [CreateAssetMenu(fileName = "MazeGenerationConfig", menuName = "NATMP/Maze/Maze Generation Config")]
    public class MazeGenerationConfig : ScriptableObject
    {
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 13;
        [SerializeField] [Range(0f, 1f)]
        [Tooltip("Xác suất thử đặt tường trên từng ô nội bộ; chỉ giữ tường nếu toàn bộ ô sàn vẫn liên thông với bug (một maze một miền).")]
        private float _wallChance = 0.35f;
        [SerializeField]
        [Tooltip("Số ô trên đường đi ngắn nhất (BFS) từ bug tới đích, gồm cả ô xuất phát và đích. Đích random chỉ chọn ô có path.Count ≥ giá trị này (10, 15, 20, 30… đều được). Trùng nghĩa với kiểm tra sinh maze.")]
        private int _minPathLength = 10;
        [SerializeField] private int _maxGenerationAttempts = 30;

        /// <summary>Kích thước lưới gồm cả một vòng tường biên; tối thiểu 3 để còn phần trong cho bug (2,2).</summary>
        public int Width => Mathf.Max(3, _width);
        public int Height => Mathf.Max(3, _height);
        public float WallChance => Mathf.Clamp01(_wallChance);
        public int MinPathLength => Mathf.Max(2, _minPathLength);
        public int MaxGenerationAttempts => Mathf.Max(1, _maxGenerationAttempts);

        public MazeGenerationParameters ToParameters() =>
            new(Width, Height, WallChance, MinPathLength, MaxGenerationAttempts);
    }

    /// <summary>
    /// Snapshot không phụ thuộc SO — truyền vào generator tĩnh.
    /// </summary>
    public readonly struct MazeGenerationParameters
    {
        public int Width { get; }
        public int Height { get; }
        public float WallChance { get; }
        public int MinPathLength { get; }
        public int MaxGenerationAttempts { get; }

        public MazeGenerationParameters(int width, int height, float wallChance, int minPathLength, int maxGenerationAttempts)
        {
            Width = width;
            Height = height;
            WallChance = wallChance;
            MinPathLength = minPathLength;
            MaxGenerationAttempts = maxGenerationAttempts;
        }

        public static MazeGenerationParameters DefaultClassic => new(10, 13, 0.35f, 10, 30);
    }

    public static class MazeGameplaySeed
    {
        public static int DeterministicFromStageIndex(int stageIndex) => unchecked(stageIndex * 73856093 ^ 19349663);
    }
}
