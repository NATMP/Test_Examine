using NATMP.UI.Map;
using NATMP.Utilities.GamePlaySystem;

using UnityEngine;
using UnityEngine.UI;

public class UIHomeManager : MonoBehaviour
{
    [SerializeField] private Button _btnReset;
    [SerializeField] private StageMapScrollDataSource _stageMapScroll;
    private void OnEnable()
    {
        if (_btnReset != null)
            _btnReset.onClick.AddListener(OnResetClicked);
    }
    private void OnDisable()
    {
        if (_btnReset != null)
        {
            _btnReset.onClick.RemoveListener(OnResetClicked);
        }
    }
    private void OnResetClicked()
    {
        var mapLevelData = GameController.Instance.DataController.GetData<PlayerDataJson>().MapLevelData;
        mapLevelData.ResetAllStages();
        _stageMapScroll.InitScrollView(mapLevelData.Stages);
    }
}
