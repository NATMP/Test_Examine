using System.Collections.Generic;
using GamesTan.UI;
using UnityEngine;

namespace GamesTan.SuperScrollRectDemo {
    
    public class DemoCellData {
        public int Idx;
        public string Name;
        public int Count;

        public override string ToString() {
            return $"Idx:{Idx} Name:{Name} Count:{Count}";
        }
    }

    
    public class DemoBase : MonoBehaviour, ISuperScrollRectDataProvider {
        [Header("Basic")] public SuperScrollRect ScrollRect;
        public int Count = 500;
        private List<DemoCellData> Datas = new List<DemoCellData>();
        private void Awake() {
            for (int i = 0; i < Count; i++) {
                Datas.Add(new DemoCellData() {
                    Idx = i,
                    Name = "Cell " + i,
                    Count = UnityEngine.Random.Range(1, 10)
                });
            }

            ScrollRect.DoAwake(this);
            DoAwake();
        }

        protected virtual void DoAwake() {
        }

        public int GetCellCount() {
            return Datas.Count;
        }

        public void SetCell(GameObject cell, int index) {
            var item = cell.GetComponent<DemoCell>();
            item.BindData(Datas[index]);
        }
    }
}