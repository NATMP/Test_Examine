using NATMP.Utilities.GamePlaySystem;
using UnityEngine;

namespace NATMP.UI.Map
{
    public class StageRowItem : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;

        private StageItemCell[] _cells;
        private int _cols;
        private float _spacingX;
        private float _itemWidth;

        public void EnsureCreated(GameObject stageItemPrefab, int cols, float spacingX)
        {
            if (_cells != null && _cells.Length == cols)
                return;

            if (_root == null)
                _root = transform as RectTransform;

            _cols = Mathf.Max(1, cols);
            _spacingX = spacingX;
            _cells = new StageItemCell[_cols];

            // Clear existing children (defensive when prefab edited in-scene).
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            var prefabRect = stageItemPrefab != null ? stageItemPrefab.GetComponent<RectTransform>() : null;
            float itemW = prefabRect != null ? prefabRect.rect.width : 240f;
            float itemH = prefabRect != null ? prefabRect.rect.height : 277f;
            _itemWidth = itemW;

            // IMPORTANT: do not override anchor/pivot here.
            // LoopListView2 will set them based on ArrangeType (e.g. BottomToTop => pivot.y = 0).
            _root.sizeDelta = new Vector2(_cols * itemW + (_cols - 1) * _spacingX, itemH);

            for (int i = 0; i < _cols; i++)
            {
                if (stageItemPrefab == null)
                    break;

                var go = Instantiate(stageItemPrefab, _root);
                go.name = $"{stageItemPrefab.name}_{i}";
                go.SetActive(true);

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 0);
                    rt.pivot = new Vector2(0, 0);
                    rt.anchoredPosition = new Vector2(i * (itemW + _spacingX), 0);
                }

                _cells[i] = go.GetComponent<StageItemCell>();
            }
        }

        public void BindCell(int col, StageData stageOrNull)
        {
            if (_cells == null || col < 0 || col >= _cells.Length)
                return;

            var cell = _cells[col];
            if (cell == null)
                return;

            if (stageOrNull == null)
                cell.SetEmpty();
            else
                cell.Bind(stageOrNull);
        }

        public void ApplyRowLayout(int itemsInRow, bool rtl)
        {
            if (_cells == null || _cells.Length == 0)
                return;

            int activeCount = Mathf.Clamp(itemsInRow, 0, _cells.Length);
            float step = _itemWidth + _spacingX;
            float rowWidth = _root != null ? _root.sizeDelta.x : (_cells.Length * step);

            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                    continue;

                bool active = i < activeCount;
                cell.gameObject.SetActive(true); // keep root active; cell controls visuals

                // Move/hide unused slots so they don't create visible gaps.
                var rt = cell.transform as RectTransform;
                if (rt != null)
                {
                    if (!active)
                    {
                        rt.anchoredPosition = new Vector2(-99999, 0);
                    }
                    else
                    {
                        float x = rtl
                            ? (rowWidth - (activeCount * _itemWidth + (activeCount - 1) * _spacingX)) + i * step
                            : i * step;
                        rt.anchoredPosition = new Vector2(x, 0);
                    }
                }

                if (!active)
                    cell.SetEmpty();
            }
        }

        public void ApplySerpentineRtl(int itemsInRow, bool rtl)
        {
            if (_cells == null || _cells.Length == 0)
                return;
            int activeCount = Mathf.Clamp(itemsInRow, 0, _cells.Length);
            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                    continue;
                bool active = i < activeCount;
                // Line H alternates by column (0 normal, 1 flipped, 2 normal...) regardless of RTL/LTR.
                bool flipH = active && (i % 2 == 1);
                // Line V flips for RTL rows (same for all active cells).
                bool flipV = active && rtl;
                cell.SetLineFlip(flipH, flipV);
            }
        }
    }
}

