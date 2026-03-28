#if UNITY_EDITOR
using NATMP.Gameplay.Maze;
using UnityEditor;
using UnityEngine;

namespace NATMP.Gameplay.Maze.Editor
{
    /// <summary>
    /// Roster, danh sách stage, validate, reset — layout vẽ ở <see cref="MazeLayoutEditorWindow"/>.
    /// </summary>
    public sealed class MazeStageBrowserWindow : EditorWindow
    {
        private const int MaxStageIndex = 999;

        private MazeStageRoster _roster;
        private MazeGenerationConfig _config;
        private Vector2 _scrollList;
        private Vector2 _scrollBottom;
        private string _searchFilter = "";
        private int _selectedStage = 1;
        private string _validateOutput = "";

        [MenuItem("NATMP/Maze/Maze GD Tool")]
        public static void OpenWindow()
        {
            var w = GetWindow<MazeStageBrowserWindow>();
            w.titleContent = new GUIContent("Maze GD");
            w.minSize = new Vector2(440, 520);
        }

        /// <summary>Mở cửa sổ Maze Layout cho stage (dùng roster/config từ Maze GD nếu đang mở).</summary>
        public static void OpenFocusedOnStage(int stageIndex1Based)
        {
            MazeStageRoster r = null;
            MazeGenerationConfig c = null;
            var gds = Resources.FindObjectsOfTypeAll<MazeStageBrowserWindow>();
            if (gds != null && gds.Length > 0)
            {
                r = gds[0]._roster;
                c = gds[0]._config;
            }

            MazeEditorProjectAssets.TryAssignDefaultRosterAndConfig(ref r, ref c);
            MazeLayoutEditorWindow.OpenWindow(r, c, Mathf.Clamp(stageIndex1Based, 1, MaxStageIndex));
        }

        private void OnEnable()
        {
            MazeEditorProjectAssets.TryAssignDefaultRosterAndConfig(ref _roster, ref _config);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Maze GD — roster & stage", EditorStyles.boldLabel);
            _roster = (MazeStageRoster)EditorGUILayout.ObjectField("Roster", _roster, typeof(MazeStageRoster), false);
            _config = (MazeGenerationConfig)EditorGUILayout.ObjectField("Maze config", _config, typeof(MazeGenerationConfig), false);
            _searchFilter = EditorGUILayout.TextField("Filter (số stage)", _searchFilter);

            if (_roster == null || _config == null)
            {
                EditorGUILayout.HelpBox("Gán Roster và MazeGenerationConfig.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Roster rows: {_roster.Entries.Count}", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Stage đã có dòng roster: cột Map mở cửa sổ Maze Layout cho stage đó. Chưa có dòng — bấm Ensure trước.",
                MessageType.None);

            DrawStageList();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Selected: stage {_selectedStage}", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"Ensure 1..{_selectedStage}"))
                    EnsureRosterRows(_selectedStage, "Ensure maze rows");

                if (GUILayout.Button($"Ensure 1..{MaxStageIndex}"))
                    EnsureRosterRows(MaxStageIndex, "Ensure maze rows 999");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Mở Maze Layout (stage đang chọn)", GUILayout.Height(22)))
                    OpenLayoutForSelectedStage();
            }

            _scrollBottom = EditorGUILayout.BeginScrollView(_scrollBottom, GUILayout.MinHeight(220));
            DrawSelectedEntryInspector();
            EditorGUILayout.EndScrollView();

            DrawTools();
        }

        private void OpenLayoutForSelectedStage()
        {
            if (_roster == null || _config == null)
                return;
            if (!_roster.TryGetEntry(_selectedStage, out _))
            {
                EditorUtility.DisplayDialog(
                    "Maze Layout",
                    $"Chưa có dòng roster cho stage {_selectedStage}. Bấm Ensure trước.",
                    "OK");
                return;
            }

            MazeLayoutEditorWindow.OpenWindow(_roster, _config, _selectedStage);
        }

        private void DrawStageList()
        {
            _scrollList = EditorGUILayout.BeginScrollView(_scrollList, GUILayout.Height(180));
            for (int i = 1; i <= MaxStageIndex; i++)
            {
                var sf = _searchFilter?.Trim() ?? "";
                if (sf.Length > 0 && !i.ToString().Contains(sf))
                    continue;

                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedStage == i, $"{i}", GUILayout.Width(52)))
                    _selectedStage = i;

                string hint = "resolver default";
                if (_roster.TryGetEntry(i, out var e) && e != null)
                    hint = $"{e.Source} · seed {e.ProceduralSeed}";
                GUILayout.Label(hint, EditorStyles.miniLabel);

                bool hasRosterRow = _roster.TryGetEntry(i, out _);
                if (hasRosterRow)
                {
                    var stageIdx = i;
                    if (GUILayout.Button(new GUIContent("Map", "Mở cửa sổ Maze Layout cho stage này."), GUILayout.Width(40)))
                        MazeLayoutEditorWindow.OpenWindow(_roster, _config, stageIdx);
                }
                else
                {
                    GUILayout.Label(new GUIContent("—", "Chưa có dòng roster — Ensure trước."), GUILayout.Width(40));
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedEntryInspector()
        {
            var so = new SerializedObject(_roster);
            so.Update();
            var entries = so.FindProperty("_entries");
            if (entries == null || _selectedStage < 1 || _selectedStage > entries.arraySize)
            {
                EditorGUILayout.HelpBox("Chưa có dòng roster cho stage này — bấm Ensure.", MessageType.Warning);
                so.ApplyModifiedProperties();
                return;
            }

            var el = entries.GetArrayElementAtIndex(_selectedStage - 1);
            EditorGUILayout.PropertyField(el, new GUIContent($"Stage {_selectedStage}"), true);
            so.ApplyModifiedProperties();
        }

        private void DrawTools()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate layout"))
                    RunValidate();

                if (GUILayout.Button("Randomize procedural seed"))
                    RandomizeSeedForSelected();
            }

            GUI.backgroundColor = new Color(1f, 0.65f, 0.55f);
            if (GUILayout.Button("Clear ALL maze data (roster + map files + Addressables)", GUILayout.Height(24)))
                ClearAllMazeData();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox(
                "Xóa hết: làm trống roster, xóa map_*.bytes trong Assets/_Project/Addressables/MazeMap và gỡ Addressables.",
                MessageType.Warning);

            if (!string.IsNullOrEmpty(_validateOutput))
                EditorGUILayout.HelpBox(_validateOutput, MessageType.Info);
        }

        private void ClearAllMazeData()
        {
            if (!EditorUtility.DisplayDialog(
                    "Xóa toàn bộ dữ liệu maze",
                    "Sẽ làm trống toàn bộ dòng trong roster, xóa mọi file map_*.bytes trong Assets/_Project/Addressables/MazeMap và gỡ khỏi Addressables.\n\n" +
                    "Nên backup project nếu cần. Tiếp tục?",
                    "Xóa hết",
                    "Hủy"))
                return;

            Undo.RecordObject(_roster, "Clear all maze data");
            _roster.EditorClearAllEntries();
            EditorUtility.SetDirty(_roster);

            MazeAddressablesMapRegistration.DeleteAllMazeMapBytesUnderDefaultFolder();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var layoutWin in Resources.FindObjectsOfTypeAll<MazeLayoutEditorWindow>())
                layoutWin.ClearPaintState();

            _validateOutput = "Đã xóa roster + file map + Addressables.";
        }

        private void EnsureRosterRows(int upToStageInclusive, string undoLabel)
        {
            Undo.RecordObject(_roster, undoLabel);
            _roster.EditorEnsureEntryCountAtLeast(upToStageInclusive);
            EditorUtility.SetDirty(_roster);
            AssetDatabase.SaveAssets();
        }

        private void RunValidate()
        {
            _validateOutput = "";
            var p = _config.ToParameters();
            var res = MazeStageContentResolver.Resolve(_roster, _selectedStage, p);
            if (MazeLayoutValidator.TryValidate(res.Walkable, p.Width, p.Height, res.BugStart, p.MinPathLength, out var err))
                _validateOutput = $"Stage {_selectedStage}: hợp lệ.";
            else
                _validateOutput = $"Stage {_selectedStage}: {err}";
        }

        private void RandomizeSeedForSelected()
        {
            _roster.EditorEnsureEntryCountAtLeast(_selectedStage);
            if (!_roster.TryGetEntry(_selectedStage, out var entry) || entry == null)
            {
                _validateOutput = "Không tạo được entry.";
                return;
            }

            Undo.RecordObject(_roster, "Random maze seed");
            int s;
            do
            {
                s = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            } while (s == 0);

            entry.ProceduralSeed = s;
            EditorUtility.SetDirty(_roster);
            AssetDatabase.SaveAssets();
            _validateOutput = $"Stage {_selectedStage}: seed mới = {s}";
        }
    }
}
#endif
