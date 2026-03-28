using NATMP.Utilities.GamePlaySystem;
using GamesTan.UI;
using UnityEngine;
using System.Collections.Generic;

namespace NATMP.UI.Map
{
    /// <summary>
    /// Giống setup mặc định package: layout top → bottom, index 0 ở trên.
    /// Serpentine: hàng chẵn (0,2,4…) LTR (1–4, 9–12…), hàng lẻ (1,3…) RTL (8–5, 16–13…).
    /// </summary>
    public class StageMapScrollDataSource : MonoBehaviour, ISuperScrollRectDataProvider
    {
        [SerializeField]
        private SuperScrollRect _scrollRect;

        [SerializeField]
        private bool _serpentineOddRowsRtl = true;

        private PlayerMapLevelData _mapLevelData;
        private IReadOnlyList<StageData> _sourceStages;

        private void Start()
        {
            GameController.Instance.DataController.Initialize();
            var playerData = GameController.Instance.DataController.GetData<PlayerDataJson>();
            _mapLevelData = playerData.MapLevelData;
            InitScrollView(_mapLevelData.Stages);
        }

        public void InitScrollView(IReadOnlyList<StageData> source)
        {
            _sourceStages = source;
            if (_scrollRect == null)
                return;
            _scrollRect.DoAwake(this);
        }

        public int GetCellCount()
        {
            if (_sourceStages == null || _sourceStages.Count == 0)
                return 0;
            return GetPaddedCellCount(_sourceStages.Count, GetColumns());
        }

        public void SetCell(GameObject cell, int index)
        {
            var stageCell = cell.GetComponent<StageItemCell>();
            if (stageCell == null)
                return;

            int dataIndex = ResolveDataIndex(index);
            if (dataIndex < 0)
            {
                stageCell.SetEmpty();
                return;
            }

            stageCell.Bind(_sourceStages[dataIndex]);
        }

        private int GetColumns()
        {
            if (_scrollRect == null)
                return 1;
            if (!_scrollRect.IsGrid)
                return 1;
            return Mathf.Max(1, _scrollRect.Segment);
        }

        private static int GetPaddedCellCount(int stageCount, int cols)
        {
            if (cols <= 1)
                return stageCount;
            int rows = (stageCount + cols - 1) / cols;
            return rows * cols;
        }

        private int ResolveDataIndex(int visualIndex)
        {
            int count = _sourceStages.Count;
            if (count == 0)
                return -1;

            int cols = GetColumns();
            if (cols <= 1)
            {
                if (visualIndex < 0 || visualIndex >= count)
                    return -1;
                return visualIndex;
            }

            int padded = GetPaddedCellCount(count, cols);
            if (visualIndex < 0 || visualIndex >= padded)
                return -1;

            int rowsInMap = (count + cols - 1) / cols;
            int rowFromTop = visualIndex / cols;
            int col = visualIndex % cols;

            int itemsInRow = rowFromTop < rowsInMap - 1
                ? cols
                : (count % cols == 0 ? cols : count % cols);

            if (col >= itemsInRow)
                return -1;

            int baseStart = rowFromTop < rowsInMap - 1
                ? rowFromTop * cols
                : count - itemsInRow;

            bool rtl = _serpentineOddRowsRtl && (rowFromTop % 2 == 1);
            int offsetInRow = rtl ? (itemsInRow - 1 - col) : col;
            int dataIndex = baseStart + offsetInRow;

            return dataIndex < count ? dataIndex : -1;
        }
    }
}
