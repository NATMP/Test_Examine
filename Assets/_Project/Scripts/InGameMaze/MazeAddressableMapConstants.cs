namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Đường dẫn asset trong project và địa chỉ Addressables cho file map thủ công (.bytes).
    /// </summary>
    public static class MazeAddressableMapConstants
    {
        /// <summary>Thư mục chứa file .bytes trong project: <c>Assets/_Project/Addressables/MazeMap/</c>.</summary>
        public const string ProjectRelativeFolder = "Assets/_Project/Addressables/MazeMap";

        /// <summary>Địa chỉ Addressables: stage 1 → Addressables/MazeMap/map_0001.</summary>
        public const string AddressPrefix = "Addressables/MazeMap/map_";

        public static string AddressForStage(int stageIndex1Based) => $"{AddressPrefix}{stageIndex1Based:D4}";

        public static string ProjectRelativeAssetPath(int stageIndex1Based) =>
            $"{ProjectRelativeFolder}/map_{stageIndex1Based:D4}.bytes";
    }
}
