using System;
using System.Collections;
using GamesTan.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GamesTan.SuperScrollRectDemo {
    public class DemoGrid : DemoBase {
        [Header("Layout Setting")] public Toggle IsVertical;
        public Toggle IsGrid;
        public InputField GridSegments;
        [Header("Spacing")]
        public InputField SpacingX;
        public InputField SpacingY;


        [Header("JumpTo")] public InputField InputJumpTo;
        public Button BtnGoto;

        [Header("RefreshSpeed")] public InputField InputRefreshSpeed;
        public Button BtnRefreshSpeed;
        [Header("Cache")] public Button BtnClearCache;

        private void Update() {
            GridSegments.gameObject.SetActive(IsGrid.isOn);
        }

        protected override void DoAwake() {
            IsVertical.onValueChanged.AddListener((isOn) => {
                ScrollRect.Direction =
                    isOn ? BaseSuperScrollRect.EScrollDir.Vertical : BaseSuperScrollRect.EScrollDir.Horizontal;
                ScrollRect.DoAwake(this);
            });
            IsGrid.onValueChanged.AddListener((isOn) => {
                ScrollRect.IsGrid = isOn;
                if (isOn) ScrollRect.Segment = int.Parse(GridSegments.text);
                ScrollRect.ReloadData();
            });
            GridSegments.onValueChanged.AddListener((message) => {
                if (ScrollRect.IsGrid) {
                    ScrollRect.Segment = int.Parse(GridSegments.text);
                    ScrollRect.ReloadData();
                }
            });
            
            SpacingX.onValueChanged.AddListener((message) => {
                ScrollRect.Spacing = new Vector2(int.Parse(SpacingX.text), int.Parse(SpacingY.text));
                ScrollRect.ReloadData();
            });
            SpacingY.onValueChanged.AddListener((message) => {
                ScrollRect.Spacing = new Vector2(int.Parse(SpacingX.text), int.Parse(SpacingY.text));
                ScrollRect.ReloadData();
            });
            

            BtnGoto.onClick.AddListener(() => { ScrollRect.JumpTo(int.Parse(InputJumpTo.text)); });
            BtnRefreshSpeed.onClick.AddListener(
                () => { ScrollRect.SetRefreshSpeed(int.Parse(InputRefreshSpeed.text)); });
            BtnClearCache.onClick.AddListener(() => { ScrollRect.ClearCache(); });
        }
    }
}