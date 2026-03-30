using NATMP.Utilities.GamePlaySystem;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace NATMP.UI.Map
{
    /// <summary>
    /// Stage map dùng SuperScrollView (LoopListView2).
    /// </summary>
    public class StageMapScrollDataSource : MonoBehaviour
    {
        [SerializeField]
        private LoopListView2 _scrollRect;

        private ScrollRect _unityScrollRect;

        [SerializeField]
        private bool _bottomToTop = true;

        [Header("Grid (row-based, like GridViewSampleDemo)")]
        [SerializeField] private int _columns = 4;
        [SerializeField] private float _spacingX = 40f;
        [SerializeField] private float _rowPadding = 80f;
        [SerializeField] private bool _serpentineOddRowsRtl = true;

        [SerializeField] private GameObject _stageItemPrefab;

        private PlayerMapLevelData _mapLevelData;
        private IReadOnlyList<StageData> _sourceStages;

        private Vector3 _lastContentLocalPos;
        private int _lastContentChildCount;
        private float _nextSiblingCheckTime;
        private const float SiblingCheckIntervalSeconds = 0.08f; // ~12.5Hz, enough for UI ordering

        private static readonly List<Transform> SiblingBuffer = new List<Transform>(64);

        private void Start()
        {
            GameController.Instance.DataController.Initialize();
            var playerData = GameController.Instance.DataController.GetData<PlayerDataJson>();
            _mapLevelData = playerData.MapLevelData;
            InitScrollView(_mapLevelData.Stages);
        }

        private void LateUpdate()
        {
            // Keep sibling order stable while scrolling/inertia happens.
            if (_unityScrollRect == null || _unityScrollRect.content == null)
                return;

            var content = _unityScrollRect.content;
            int childCount = content.childCount;
            Vector3 pos = content.localPosition;

            bool moved = (pos - _lastContentLocalPos).sqrMagnitude > 0.0001f;
            bool childrenChanged = childCount != _lastContentChildCount;
            if (!childrenChanged && (!moved || Time.unscaledTime < _nextSiblingCheckTime))
                return;

            _nextSiblingCheckTime = Time.unscaledTime + SiblingCheckIntervalSeconds;
            _lastContentChildCount = childCount;
            _lastContentLocalPos = pos;

            // Only sort when we detect actual disorder.
            if (!IsSiblingOrderValid(content))
            {
                SortContentSiblingsByItemIndex(content);
            }
        }

        public void InitScrollView(IReadOnlyList<StageData> source)
        {
            _sourceStages = source;
            if (_scrollRect == null)
                _scrollRect = GetComponent<LoopListView2>();
            if (_unityScrollRect == null)
                _unityScrollRect = GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                Debug.LogError($"{nameof(StageMapScrollDataSource)} requires {nameof(LoopListView2)} on the same GameObject.");
                return;
            }
            if (_unityScrollRect == null)
            {
                Debug.LogError($"{nameof(StageMapScrollDataSource)} requires {nameof(ScrollRect)} on the same GameObject.");
                return;
            }

            _scrollRect.ArrangeType = _bottomToTop ? ListItemArrangeType.BottomToTop : ListItemArrangeType.TopToBottom;
            var rowPrefabData = _scrollRect.GetItemPrefabConfData("Stage_Row");
            if (rowPrefabData != null)
                rowPrefabData.mPadding = _rowPadding;

            _scrollRect.InitListView(GetRowCount(), OnGetRowByIndex);

            // Initialize sort baseline.
            if (_unityScrollRect.content != null)
            {
                _lastContentChildCount = _unityScrollRect.content.childCount;
                _lastContentLocalPos = _unityScrollRect.content.localPosition;
                SortContentSiblingsByItemIndex(_unityScrollRect.content);
            }
        }

        private int GetRowCount()
        {
            if (_sourceStages == null || _sourceStages.Count == 0)
                return 0;
            int cols = Mathf.Max(1, _columns);
            return (_sourceStages.Count + cols - 1) / cols;
        }

        private LoopListViewItem2 OnGetRowByIndex(LoopListView2 listView, int rowIndex)
        {
            if (_sourceStages == null)
                return null;

            if (rowIndex < 0)
                return null;
            int dataRowIndex = rowIndex;

            var rowItem = listView.NewListViewItem("Stage_Row");
            if (rowItem == null)
                return null;

            // Sibling order is handled by throttled LateUpdate sorting.

            var row = rowItem.GetComponent<StageRowItem>();
            if (row == null)
                return rowItem;

            if (rowItem.IsInitHandlerCalled == false)
            {
                rowItem.IsInitHandlerCalled = true;
                row.EnsureCreated(_stageItemPrefab, Mathf.Max(1, _columns), _spacingX);
            }

            int cols = Mathf.Max(1, _columns);
            int stageCount = _sourceStages.Count;
            int baseIndex = dataRowIndex * cols;
            int itemsInRow = Mathf.Clamp(stageCount - baseIndex, 0, cols);
            bool rtl = _serpentineOddRowsRtl && (dataRowIndex % 2 == 1);
            row.ApplyRowLayout(itemsInRow, rtl);

            // bind 4 cells inside one row
            for (int col = 0; col < cols; col++)
            {
                int dataIndex = ResolveDataIndex(dataRowIndex, col, cols, stageCount);
                row.BindCell(col, dataIndex >= 0 ? _sourceStages[dataIndex] : null);
            }
            // Must be applied after Bind/SetEmpty because those methods toggle visuals.
            row.ApplySerpentineRtl(itemsInRow, rtl);

            return rowItem;
        }

        private static int GetItemIndexOrMin(Transform t)
        {
            if (t == null) return int.MinValue;
            var it = t.GetComponent<LoopListViewItem2>();
            return it != null ? it.ItemIndex : int.MinValue;
        }

        private static bool IsSiblingOrderValid(Transform content)
        {
            int n = content.childCount;
            if (n <= 2) return true;
            int prev = GetItemIndexOrMin(content.GetChild(0));
            for (int i = 1; i < n; i++)
            {
                int cur = GetItemIndexOrMin(content.GetChild(i));
                if (cur < prev)
                    return false;
                prev = cur;
            }
            return true;
        }

        private static void SortContentSiblingsByItemIndex(Transform content)
        {
            if (content == null) return;
            int n = content.childCount;
            if (n <= 1) return;

            SiblingBuffer.Clear();
            for (int i = 0; i < n; i++)
                SiblingBuffer.Add(content.GetChild(i));

            // Sort by LoopListViewItem2.ItemIndex ascending. Stable by instance id fallback.
            SiblingBuffer.Sort((a, b) =>
            {
                if (a == b) return 0;
                int ai = GetItemIndexOrMin(a);
                int bi = GetItemIndexOrMin(b);
                int c = ai.CompareTo(bi);
                return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });

            for (int i = 0; i < SiblingBuffer.Count; i++)
                SiblingBuffer[i].SetSiblingIndex(i);
        }

        private int ResolveDataIndex(int rowIndex, int col, int cols, int stageCount)
        {
            if (rowIndex < 0 || col < 0 || col >= cols || stageCount <= 0)
                return -1;

            int baseIndex = rowIndex * cols;
            if (baseIndex >= stageCount)
                return -1;

            int itemsInRow = Mathf.Min(cols, stageCount - baseIndex);
            if (col >= itemsInRow)
                return -1;

            bool rtl = _serpentineOddRowsRtl && (rowIndex % 2 == 1);
            int offset = rtl ? (itemsInRow - 1 - col) : col;
            int idx = baseIndex + offset;
            return (idx >= 0 && idx < stageCount) ? idx : -1;
        }
    }
}
