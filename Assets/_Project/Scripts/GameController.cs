using UnityEngine;
using NATMP.Utilities.DesignPatterns;
using NATMP.Utilities;

public class GameController : PersistentSingleton<GameController>
{
    [SerializeField] private DataController _dataController;
    public DataController DataController => _dataController;

    /// <summary>Stage 1-based chọn trên map trước khi load gameplay. -1 = chưa chọn.</summary>
    public int PendingGameplayStageIndex { get; set; } = -1;
    override protected void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
    }
}
