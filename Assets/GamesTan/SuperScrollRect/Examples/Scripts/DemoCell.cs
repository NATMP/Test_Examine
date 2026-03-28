using GamesTan.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GamesTan.SuperScrollRectDemo {
    public class DemoCell : MonoBehaviour, IScrollCell {
        public Button BtnItem;
        public Text TextCount;
        public Text TextName;

        private DemoCellData _data;

        public void BindData(DemoCellData data) {
            _data = data;
            BtnItem.onClick.RemoveListener(OnClick_BtnItem);
            BtnItem.onClick.AddListener(OnClick_BtnItem);
            TextCount.text = data.Count.ToString();
            TextName.text = data.Name.ToString();
            name = "Cell " + data.Idx;
        }

        void OnClick_BtnItem() {
            UnityEngine.Debug.Log(" Click " + _data);
        }
    }
}