using System.Collections.Generic;
using UnityEngine;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Roster nội dung maze theo stage (1-based). Nếu thiếu dòng cho một stage,
    /// <see cref="MazeStageContentResolver"/> dùng <see cref="MazeStageEntry.CreateDefaultForStage"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "MazeStageRoster", menuName = "NATMP/Maze/Maze Stage Roster")]
    public class MazeStageRoster : ScriptableObject
    {
        [SerializeField] private List<MazeStageEntry> _entries = new();

        public IReadOnlyList<MazeStageEntry> Entries => _entries;

        /// <summary>stageIndex 1-based.</summary>
        public bool TryGetEntry(int stageIndex, out MazeStageEntry entry)
        {
            entry = null;
            if (_entries == null || stageIndex < 1 || stageIndex > _entries.Count)
                return false;
            entry = _entries[stageIndex - 1];
            return entry != null;
        }

#if UNITY_EDITOR
        /// <summary>Editor: đảm bảo có ít nhất <paramref name="stageIndex"/> phần tử (1-based).</summary>
        public void EditorEnsureEntryCountAtLeast(int stageIndex)
        {
            if (_entries == null)
                _entries = new List<MazeStageEntry>();
            while (_entries.Count < stageIndex)
                _entries.Add(MazeStageEntry.CreateDefaultForStage(_entries.Count + 1));
        }

        /// <summary>Editor: xóa toàn bộ dòng roster — runtime dùng <see cref="MazeStageEntry.CreateDefaultForStage"/> khi thiếu.</summary>
        public void EditorClearAllEntries()
        {
            if (_entries == null)
                _entries = new List<MazeStageEntry>();
            else
                _entries.Clear();
        }
#endif
    }
}
