#if UNITY_EDITOR
using System.IO;
using NATMP.Gameplay.Maze;
using UnityEditor;
using UnityEngine;

namespace NATMP.Gameplay.Maze.Editor
{
    /// <summary>
    /// Cửa sổ vẽ / sinh / lưu map — chỉ mở từ <see cref="MazeStageBrowserWindow"/> (Map / Mở Maze Layout), không có menu riêng.
    /// </summary>
    public sealed class MazeLayoutEditorWindow : EditorWindow
    {
        private const int MaxStageIndex = 999;

        private MazeStageRoster _roster;
        private MazeGenerationConfig _config;
        private int _stageIndex = 1;
        private bool[,] _grid;
        private Vector2 _scrollPaint;
        private float _pendingScrollPaintY = -1f;
        private GUIStyle _bugCellGuiStyle;
        private string _status = "";
        /// <summary>Vị trí bug trên lưới nháp; override so với mặc định (2,2). Lưu vào .bytes khi Save.</summary>
        private Vector2Int _editorBugStart = new(2, 2);

        public static void OpenWindow(MazeStageRoster roster, MazeGenerationConfig config, int stageIndex1Based)
        {
            var w = GetWindow<MazeLayoutEditorWindow>();
            w.titleContent = new GUIContent("Maze Layout");
            w.minSize = new Vector2(520, 480);
            if (roster != null)
                w._roster = roster;
            if (config != null)
                w._config = config;
            MazeEditorProjectAssets.TryAssignDefaultRosterAndConfig(ref w._roster, ref w._config);

            int clamped = Mathf.Clamp(stageIndex1Based, 1, MaxStageIndex);
            if (clamped != w._stageIndex)
            {
                w._grid = null;
                w._editorBugStart = new Vector2Int(2, 2);
                w._status = $"Stage {clamped}. Generate hoặc Load.";
            }

            w._stageIndex = clamped;
            w.Show();
            w.Focus();
        }

        /// <summary>Gọi khi Clear ALL từ Maze GD — tránh lưới nháp lệch roster.</summary>
        internal void ClearPaintState()
        {
            _grid = null;
            _editorBugStart = new Vector2Int(2, 2);
            _status = "Roster/map đã reset từ Maze GD — Generate hoặc Load lại.";
            Repaint();
        }

        private void OnEnable()
        {
            MazeEditorProjectAssets.TryAssignDefaultRosterAndConfig(ref _roster, ref _config);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Maze layout — vẽ & lưu map", EditorStyles.boldLabel);
            _roster = (MazeStageRoster)EditorGUILayout.ObjectField("Roster", _roster, typeof(MazeStageRoster), false);
            _config = (MazeGenerationConfig)EditorGUILayout.ObjectField("Maze config", _config, typeof(MazeGenerationConfig), false);
            var prevStage = _stageIndex;
            _stageIndex = EditorGUILayout.IntSlider("Stage (1-based)", _stageIndex, 1, MaxStageIndex);
            if (prevStage != _stageIndex && _grid != null)
                RequestScrollPaintToBugRow();

            if (_roster == null || _config == null)
            {
                EditorGUILayout.HelpBox("Gán Roster và MazeGenerationConfig.", MessageType.Info);
                return;
            }

            if (_stageIndex > _roster.Entries.Count)
            {
                EditorGUILayout.HelpBox(
                    $"Roster chưa có dòng cho stage {_stageIndex}. Vào Maze GD → Ensure đủ dòng rồi bấm Map lại.",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate random maze", GUILayout.Height(26)))
                    GenerateRandomMaze();

                if (GUILayout.Button("Load from project .bytes", GUILayout.Height(26)))
                    LoadFromProjectBytes();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save → .bytes + Addressables + roster", GUILayout.Height(26)))
                    SaveToAddressablesAndRoster();

                if (GUILayout.Button(
                        new GUIContent(
                            "Xóa lưới nháp",
                            "Chỉ bỏ maze đang vẽ trong Editor. Không xóa file .bytes, không đổi roster."),
                        GUILayout.Height(26)))
                {
                    _grid = null;
                    _editorBugStart = new Vector2Int(2, 2);
                    _status = "Đã xóa lưới nháp (chưa lưu).";
                }
            }

            EditorGUILayout.HelpBox(
                "Lưới nháp là bản đang vẽ trước khi Save. Xóa file đã build: Assets/_Project/Addressables/MazeMap hoặc Maze GD → Clear ALL.",
                MessageType.None);

            EditorGUILayout.HelpBox(
                $"File: {MazeAddressableMapConstants.ProjectRelativeAssetPath(_stageIndex)}\n" +
                $"Address: {MazeAddressableMapConstants.AddressForStage(_stageIndex)}",
                MessageType.None);

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);

            EditorGUILayout.Space(4);
            DrawPaintGrid();
        }

        private static string AbsolutePathForStageMap(int stage)
        {
            var rel = MazeAddressableMapConstants.ProjectRelativeAssetPath(stage);
            var tail = rel.StartsWith("Assets/", System.StringComparison.Ordinal)
                ? rel.Substring("Assets/".Length)
                : rel;
            return Path.Combine(Application.dataPath, tail.Replace('/', Path.DirectorySeparatorChar));
        }

        private void GenerateRandomMaze()
        {
            if (_stageIndex > _roster.Entries.Count)
            {
                _status = "Thiếu dòng roster — Ensure trong Maze GD.";
                return;
            }

            Undo.RecordObject(_roster, "Maze layout generate");
            _roster.EditorEnsureEntryCountAtLeast(_stageIndex);
            if (!_roster.TryGetEntry(_stageIndex, out var entry) || entry == null)
            {
                _status = "Không lấy được entry roster.";
                return;
            }

            int seed;
            do
            {
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            } while (seed == 0);

            entry.ProceduralSeed = seed;
            EditorUtility.SetDirty(_roster);

            var p = _config.ToParameters();
            _editorBugStart = new Vector2Int(2, 2);
            var bug = _editorBugStart;
            _grid = InGameMazeMazeGenerator.Generate(p, bug, seed);
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, bug);
            RequestScrollPaintToBugRow();
            _status = $"Stage {_stageIndex}: maze ngẫu nhiên (seed {seed}). Chỉnh tay rồi Save.";
        }

        private void LoadFromProjectBytes()
        {
            var p = _config.ToParameters();
            var abs = AbsolutePathForStageMap(_stageIndex);
            if (!MazeManualLayoutBinary.TryLoadFromFile(abs, p.Width, p.Height, out var w, out var bugFromFile))
            {
                _status = $"Chưa có file hoặc sai kích thước: {abs}";
                return;
            }

            _grid = w;
            _editorBugStart = bugFromFile;
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, _editorBugStart);
            RequestScrollPaintToBugRow();
            _status = $"Stage {_stageIndex}: đã load từ .bytes.";
        }

        private void SaveToAddressablesAndRoster()
        {
            if (_grid == null)
            {
                _status = "Chưa có lưới — Generate hoặc Load trước.";
                return;
            }

            var p = _config.ToParameters();
            _roster.EditorEnsureEntryCountAtLeast(_stageIndex);
            if (!_roster.TryGetEntry(_stageIndex, out var entry) || entry == null)
            {
                _status = "Không lấy được entry roster.";
                return;
            }

            var bug = _editorBugStart;
            InGameMazeMazeGenerator.StampPerimeterWalls(_grid, p.Width, p.Height);
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, bug);
            if (!MazeLayoutValidator.TryValidate(_grid, p.Width, p.Height, bug, p.MinPathLength, out var err))
            {
                if (!EditorUtility.DisplayDialog(
                        "Layout không hợp lệ",
                        $"{err}\n\nVẫn lưu file và Addressables?",
                        "Vẫn lưu",
                        "Hủy"))
                {
                    _status = "Đã hủy lưu.";
                    return;
                }
            }

            var projPath = MazeAddressableMapConstants.ProjectRelativeAssetPath(_stageIndex);
            var abs = AbsolutePathForStageMap(_stageIndex);
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            MazeManualLayoutBinary.WriteToFile(abs, _grid, p.Width, p.Height, bug);
            AssetDatabase.Refresh();

            var address = MazeAddressableMapConstants.AddressForStage(_stageIndex);
            if (!MazeAddressablesMapRegistration.TryRegister(projPath, address, out var addrErr))
            {
                EditorUtility.DisplayDialog("Addressables", addrErr, "OK");
                _status = $"Đã ghi file nhưng Addressables lỗi: {addrErr}";
                return;
            }

            Undo.RecordObject(_roster, "Maze layout save manual");
            entry.Source = MazeStageContentSource.Manual;
            entry.ManualAddressableAddress = address;
            EditorUtility.SetDirty(_roster);
            AssetDatabase.SaveAssets();
            _status = $"Stage {_stageIndex}: đã lưu + Addressables '{address}'.";
        }

        private Vector2Int ResolvePaintBugStart() => _editorBugStart;

        private bool BugStartIsNotDefault() => _editorBugStart.x != 2 || _editorBugStart.y != 2;

        private void ResetBugStartToDefault()
        {
            if (_config == null || _grid == null)
                return;

            _editorBugStart = new Vector2Int(2, 2);
            var p = _config.ToParameters();
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, _editorBugStart);
            RequestScrollPaintToBugRow();
        }

        private void SetBugStartFromGridClick(int x, int y)
        {
            if (_config == null || _grid == null)
                return;

            var p = _config.ToParameters();
            if (x <= 1 || x >= p.Width || y <= 1 || y >= p.Height)
                return;

            _editorBugStart = x == 2 && y == 2 ? new Vector2Int(2, 2) : new Vector2Int(x, y);
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, _editorBugStart);
            RequestScrollPaintToBugRow();
        }

        /// <summary>Chuột trái trên ô nội thất: vòng · → █ → B → · (B rồi click lần nữa bỏ bug về mặc định 2,2 nếu đang override).</summary>
        private void ApplyGridCellLeftClick(int x, int y)
        {
            if (_config == null || _grid == null)
                return;

            var p = _config.ToParameters();
            if (x <= 1 || x >= p.Width || y <= 1 || y >= p.Height)
                return;

            bool isBug = x == _editorBugStart.x && y == _editorBugStart.y;
            bool walk = _grid[x, y];

            if (isBug)
            {
                if (_editorBugStart.x != 2 || _editorBugStart.y != 2)
                    ResetBugStartToDefault();
                return;
            }

            if (!walk)
            {
                _grid[x, y] = true;
                SetBugStartFromGridClick(x, y);
                return;
            }

            _grid[x, y] = false;
            InGameMazeMazeGenerator.StampPerimeterWalls(_grid, p.Width, p.Height);
            InGameMazeMazeGenerator.EnsureBugCellWalkable(_grid, p.Width, p.Height, _editorBugStart);
        }

        private void RequestScrollPaintToBugRow()
        {
            if (_grid == null || _config == null)
                return;
            var p = _config.ToParameters();
            var bug = ResolvePaintBugStart();
            const float approxRowHeight = 24f;
            int rowFromTop = bug.y - 1;
            _pendingScrollPaintY = Mathf.Max(0f, rowFromTop * approxRowHeight - 72f);
        }

        private static void DrawLegendChip(string text, Color bg, string caption)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = bg;
            GUILayout.Box(new GUIContent(text, caption), GUILayout.Width(22), GUILayout.Height(20));
            GUI.backgroundColor = old;
            GUILayout.Label(caption, EditorStyles.miniLabel, GUILayout.Width(46));
        }

        private void DrawPaintGrid()
        {
            if (_grid == null)
            {
                EditorGUILayout.HelpBox("Chưa có lưới — Generate random maze hoặc Load from project .bytes.", MessageType.Info);
                return;
            }

            var p = _config.ToParameters();
            var bug = ResolvePaintBugStart();

            var hdr = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                wordWrap = true
            };
            var cc = GUI.contentColor;
            GUI.contentColor = new Color(1f, 0.88f, 0.25f);
            EditorGUILayout.LabelField(
                $"BUG xuất phát: x = {bug.x}, y = {bug.y} — ô vàng cam chữ B; hàng ▼ = y đó. " +
                "Sau Generate mặc định (2,2). Trên lưới: mỗi lần click chuột trái trên ô nội thất theo vòng · (sàn) → █ (tường) → B (bug tại ô đó) → · (bỏ override bug, về 2,2). " +
                "Ô đang là B mà bug đang ở (2,2) thì click không đổi (ô bug luôn sàn). Save ghi bug vào .bytes. Trên cùng = y = 1 (khớp gameplay).",
                hdr);
            GUI.contentColor = cc;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Chú thích:", GUILayout.Width(52));
            DrawLegendChip("■", new Color(0.4f, 0.4f, 0.4f), "Biên");
            DrawLegendChip("·", new Color(0.85f, 0.9f, 1f), "Sàn");
            DrawLegendChip("█", new Color(0.25f, 0.28f, 0.4f), "Tường");
            DrawLegendChip("B", new Color(1f, 0.78f, 0.12f), "Bug");
            GUILayout.Label("Ô lưới: ·→█→B→·", EditorStyles.miniLabel, GUILayout.Width(118));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cuộn tới hàng BUG", GUILayout.Width(160)))
                    RequestScrollPaintToBugRow();

                using (new EditorGUI.DisabledScope(!BugStartIsNotDefault()))
                {
                    if (GUILayout.Button(new GUIContent("Bug mặc định (2,2)", "Đặt bug về (2,2) trên lưới; ghi vào file khi Save."), GUILayout.Width(150)))
                        ResetBugStartToDefault();
                }
            }

            if (_bugCellGuiStyle == null)
            {
                _bugCellGuiStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black }
                };
            }

            const float cell = 22f;
            const float rowLabelW = 52f;

            if (_pendingScrollPaintY >= 0f)
            {
                _scrollPaint.y = _pendingScrollPaintY;
                _pendingScrollPaintY = -1f;
            }

            _scrollPaint = EditorGUILayout.BeginScrollView(_scrollPaint, GUILayout.MinHeight(260));

            var oldBg = GUI.backgroundColor;
            for (int y = 1; y <= p.Height; y++)
            {
                GUILayout.BeginHorizontal();
                if (y == bug.y)
                {
                    GUI.contentColor = new Color(1f, 0.88f, 0.3f);
                    GUILayout.Label(new GUIContent($"▼ {y}", $"Hàng y = {bug.y} chứa BUG"), EditorStyles.boldLabel, GUILayout.Width(rowLabelW));
                    GUI.contentColor = Color.white;
                }
                else
                {
                    GUILayout.Label(y.ToString(), GUILayout.Width(rowLabelW));
                }

                for (int x = 1; x <= p.Width; x++)
                {
                    bool perimeter = x == 1 || x == p.Width || y == 1 || y == p.Height;
                    bool isBugCell = x == bug.x && y == bug.y;
                    bool walk = _grid[x, y];
                    if (perimeter)
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
                        GUILayout.Box("■", GUILayout.Width(cell), GUILayout.Height(cell));
                    }
                    else if (isBugCell)
                    {
                        GUI.backgroundColor = new Color(1f, 0.78f, 0.12f);
                        var bugTip =
                            $"BUG xuất phát — luôn là sàn (x={bug.x}, y={bug.y}). " +
                            "Click: nếu bug đang không phải mặc định (2,2) thì về (2,2); nếu đã (2,2) thì giữ nguyên.";
                        if (GUILayout.Button(new GUIContent("B", bugTip), _bugCellGuiStyle, GUILayout.Width(cell), GUILayout.Height(cell)))
                            ApplyGridCellLeftClick(x, y);
                    }
                    else
                    {
                        GUI.backgroundColor = walk ? new Color(0.85f, 0.9f, 1f) : new Color(0.25f, 0.28f, 0.4f);
                        var label = walk ? "·" : "█";
                        var cellTip = walk
                            ? "Sàn — click → tường"
                            : "Tường — click → sàn + đặt bug tại ô này";
                        if (GUILayout.Button(new GUIContent(label, cellTip), GUILayout.Width(cell), GUILayout.Height(cell)))
                            ApplyGridCellLeftClick(x, y);
                    }
                }

                GUI.backgroundColor = oldBg;
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(rowLabelW));
            for (int x = 1; x <= p.Width; x++)
                GUILayout.Label(x.ToString(), GUILayout.Width(cell));
            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
