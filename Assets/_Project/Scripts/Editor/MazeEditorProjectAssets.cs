#if UNITY_EDITOR
using UnityEditor;

namespace NATMP.Gameplay.Maze.Editor
{
    /// <summary>Gán roster/config mặc định khi mở tool mà chưa kéo asset (tìm asset đầu tiên trong project).</summary>
    internal static class MazeEditorProjectAssets
    {
        internal static void TryAssignDefaultRosterAndConfig(ref MazeStageRoster roster, ref MazeGenerationConfig config)
        {
            if (roster == null)
            {
                var guids = AssetDatabase.FindAssets("t:MazeStageRoster");
                if (guids.Length > 0)
                    roster = AssetDatabase.LoadAssetAtPath<MazeStageRoster>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (config == null)
            {
                var cg = AssetDatabase.FindAssets("t:MazeGenerationConfig");
                if (cg.Length > 0)
                    config = AssetDatabase.LoadAssetAtPath<MazeGenerationConfig>(AssetDatabase.GUIDToAssetPath(cg[0]));
            }
        }
    }
}
#endif
