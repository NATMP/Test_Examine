using System;
using System.Collections;
using System.Collections.Generic;
using NATMP;
using NATMP.Gameplay.Maze;
using NATMP.Utilities.GamePlaySystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMazeController : MonoBehaviour
{
    /// <summary>Ô bắt đầu bên trong tường biên (hàng/cột 1 và W/H là tường).</summary>
    private static readonly Vector2Int BugStart = new(2, 2);

    [Header("Hierarchy")]
    [SerializeField] private Transform mazeRoot;
    [SerializeField] private Transform hintRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallCellPrefab;
    [SerializeField] private GameObject hintTilePrefab;
    [SerializeField] private GameObject bugPrefab;
    [SerializeField] private GameObject targetPrefab;

    [Header("UI")]
    [SerializeField] private Button findButton;
    [SerializeField] private Button autoMoveButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button retryButton;

    [Header("Maze generation")]
    [SerializeField] private MazeGenerationConfig mazeGenerationConfig;
    [Tooltip("Dùng khi Play trực tiếp GameplayScene hoặc không có stage từ map.")]
    [SerializeField] private int fallbackStageIndexForDirectPlay = 1;

    [Header("Lưới maze (world)")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 mazeTopLeftWorld = new(-4.5f, 4f);

    private MazeGenerationParameters _genParams;
    private int _mazeWidth;
    private int _mazeHeight;
    private int _activeStageIndex = 1;
    private int _activeMazeSeed;
    private int _targetPlacementSessionSalt;

    private bool[,] walkable;
    private Vector2Int targetCell;
    private Vector2Int currentBugCell = BugStart;

    private Transform bugTransform;
    private Transform targetTransform;

    private Coroutine autoMoveRoutine;
    private readonly List<Vector2Int> lastPath = new();
    private readonly List<Vector2Int> _pickMeetsMinScratch = new();
    private readonly List<Vector2Int> _pickLongestScratch = new();
    private int[,] _shortestPathCellScratch;

    private readonly List<GameObject> _wallPool = new();

    private void OnEnable()
    {
        if (findButton != null)
            findButton.onClick.AddListener(OnFindClicked);
        if (autoMoveButton != null)
            autoMoveButton.onClick.AddListener(OnAutoMoveClicked);
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnDisable()
    {
        if (findButton != null)
            findButton.onClick.RemoveListener(OnFindClicked);
        if (autoMoveButton != null)
            autoMoveButton.onClick.RemoveListener(OnAutoMoveClicked);
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    private void Awake()
    {
        _genParams = mazeGenerationConfig != null
            ? mazeGenerationConfig.ToParameters()
            : MazeGenerationParameters.DefaultClassic;
        _mazeWidth = _genParams.Width;
        _mazeHeight = _genParams.Height;
        _shortestPathCellScratch = new int[_mazeWidth + 1, _mazeHeight + 1];

        var gc = GameController.Instance;
        int pending = gc != null ? gc.PendingGameplayStageIndex : -1;
        int stageToPlay = pending > 0 ? pending : fallbackStageIndexForDirectPlay;
        if (pending > 0 && gc != null)
            gc.PendingGameplayStageIndex = -1;

        EnterStage(stageToPlay);
    }

    private void OnMenuClicked()
    {
        SceneManager.LoadScene(ProjectScenes.Home);
    }

    private void OnRetryClicked()
    {
        ResetPlayStateKeepTarget();
    }

    private void ResetPlayStateKeepTarget()
    {
        if (autoMoveRoutine != null)
        {
            StopCoroutine(autoMoveRoutine);
            autoMoveRoutine = null;
        }

        lastPath.Clear();
        ClearHint();

        currentBugCell = BugStart;
        if (bugTransform != null)
            bugTransform.position = CellToWorld(currentBugCell);
    }

    public void EnterStage(int newStageIndex)
    {
        if (autoMoveRoutine != null)
        {
            StopCoroutine(autoMoveRoutine);
            autoMoveRoutine = null;
        }

        lastPath.Clear();
        ClearHint();

        _activeStageIndex = Mathf.Max(1, newStageIndex);
        _activeMazeSeed = ResolveSavedMazeSeed(_activeStageIndex);
        if (_activeMazeSeed == 0)
            _activeMazeSeed = MazeGameplaySeed.DeterministicFromStageIndex(_activeStageIndex);

        _targetPlacementSessionSalt = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        walkable = InGameMazeMazeGenerator.Generate(_genParams, BugStart, _activeMazeSeed);
        RebuildMazeVisual();
        SpawnBugAndTarget();
    }

    private static int ResolveSavedMazeSeed(int stageIndex)
    {
        var gc = GameController.Instance;
        if (gc == null || gc.DataController == null)
            return 0;

        gc.DataController.Initialize();
        var playerData = gc.DataController.GetData<PlayerDataJson>();
        if (playerData == null)
            return 0;

        return playerData.MapLevelData.TryGetStage(stageIndex, out var stage) ? stage.MazeSeed : 0;
    }

    private void RebuildMazeVisual()
    {
        if (mazeRoot == null || wallCellPrefab == null)
            return;

        ReturnWallsToPool();

        for (int x = 1; x <= _mazeWidth; x++)
        {
            for (int y = 1; y <= _mazeHeight; y++)
            {
                if (walkable[x, y])
                    continue;

                var cell = RentWallFromPool();
                cell.transform.SetParent(mazeRoot, false);
                cell.name = $"W_{x}_{y}";
                cell.transform.position = CellToWorld(new Vector2Int(x, y));
                cell.transform.localScale = Vector3.one * (cellSize / 1f);
                cell.SetActive(true);
            }
        }
    }

    private GameObject RentWallFromPool()
    {
        if (_wallPool.Count > 0)
        {
            int last = _wallPool.Count - 1;
            var go = _wallPool[last];
            _wallPool.RemoveAt(last);
            return go;
        }

        return Instantiate(wallCellPrefab);
    }

    private void ReturnWallsToPool()
    {
        if (mazeRoot == null)
            return;
        for (int i = mazeRoot.childCount - 1; i >= 0; i--)
        {
            var child = mazeRoot.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(transform, false);
            _wallPool.Add(child);
        }
    }

    private void SpawnBugAndTarget()
    {
        if (bugPrefab == null || targetPrefab == null)
            return;

        if (bugTransform != null)
            Destroy(bugTransform.gameObject);
        if (targetTransform != null)
            Destroy(targetTransform.gameObject);

        var bugGo = Instantiate(bugPrefab, transform);
        bugGo.name = "Bug";
        bugTransform = bugGo.transform;
        bugTransform.localScale = Vector3.one * (cellSize / 1f);

        var targetGo = Instantiate(targetPrefab, transform);
        targetGo.name = "Target";
        targetTransform = targetGo.transform;
        targetTransform.localScale = Vector3.one * (cellSize / 1f);

        currentBugCell = BugStart;
        bugTransform.position = CellToWorld(currentBugCell);

        targetCell = PickRandomTargetCell();
        targetTransform.position = CellToWorld(targetCell);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    /// <summary>
    /// Chọn đích ngẫu nhiên: đường ngắn nhất (BFS) từ bug tới đích phải có độ dài
    /// <see cref="MazeGenerationParameters.MinPathLength"/> ô trở lên (path.Count ≥ min, có thể 15, 20, 30…).
    /// </summary>
    private Vector2Int PickRandomTargetCell()
    {
        int rngSeed = HashCode.Combine(_activeMazeSeed, _targetPlacementSessionSalt);
        var rng = new System.Random(rngSeed);
        int minCellsOnShortestPath = _genParams.MinPathLength;

        InGameMazeGridPathFinder.FillShortestPathCellCounts(
            walkable, BugStart, _mazeWidth, _mazeHeight, _shortestPathCellScratch);

        _pickMeetsMinScratch.Clear();
        _pickLongestScratch.Clear();
        int longestPathCellCount = 0;
        Vector2Int anyFallback = default;
        bool hasAnyFallback = false;

        for (int x = 1; x <= _mazeWidth; x++)
        for (int y = 1; y <= _mazeHeight; y++)
        {
            if (!walkable[x, y])
                continue;
            if (x == BugStart.x && y == BugStart.y)
                continue;

            int cellsOnPath = _shortestPathCellScratch[x, y];
            if (cellsOnPath < 0)
                continue;

            var c = new Vector2Int(x, y);
            anyFallback = c;
            hasAnyFallback = true;

            if (cellsOnPath >= minCellsOnShortestPath)
                _pickMeetsMinScratch.Add(c);

            if (cellsOnPath > longestPathCellCount)
            {
                longestPathCellCount = cellsOnPath;
                _pickLongestScratch.Clear();
                _pickLongestScratch.Add(c);
            }
            else if (cellsOnPath == longestPathCellCount)
            {
                _pickLongestScratch.Add(c);
            }
        }

        if (!hasAnyFallback)
            return new Vector2Int(_mazeWidth, _mazeHeight);

        if (_pickMeetsMinScratch.Count > 0)
            return _pickMeetsMinScratch[rng.Next(_pickMeetsMinScratch.Count)];

        if (_pickLongestScratch.Count > 0)
            return _pickLongestScratch[rng.Next(_pickLongestScratch.Count)];

        return anyFallback;
    }

    private void OnFindClicked()
    {
        if (bugTransform == null || targetTransform == null)
            return;

        if (!TryFindPath(currentBugCell, targetCell, out var path))
        {
            ClearHint();
            lastPath.Clear();
            return;
        }

        lastPath.Clear();
        lastPath.AddRange(path);
        RenderHintPath(path);
    }

    private void OnAutoMoveClicked()
    {
        if (bugTransform == null)
            return;

        if (autoMoveRoutine != null)
        {
            StopCoroutine(autoMoveRoutine);
            autoMoveRoutine = null;
            return;
        }

        if (lastPath.Count == 0)
        {
            OnFindClicked();
            if (lastPath.Count == 0)
                return;
        }

        autoMoveRoutine = StartCoroutine(AutoMoveAlong(lastPath));
    }

    private IEnumerator AutoMoveAlong(List<Vector2Int> path)
    {
        const float secondsPerCell = 0.12f;

        for (int i = 0; i < path.Count; i++)
        {
            var targetPos = CellToWorld(path[i]);
            var startPos = bugTransform.position;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / secondsPerCell;
                bugTransform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t));
                yield return null;
            }

            bugTransform.position = targetPos;
            currentBugCell = path[i];
        }

        autoMoveRoutine = null;
    }

    private void ClearHint()
    {
        if (hintRoot == null)
            return;
        ClearChildren(hintRoot);
    }

    private void RenderHintPath(List<Vector2Int> path)
    {
        ClearHint();
        if (hintRoot == null || hintTilePrefab == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            var p = path[i];
            var tile = Instantiate(hintTilePrefab, hintRoot);
            tile.name = $"H_{p.x}_{p.y}";
            tile.transform.position = CellToWorld(p);
            tile.transform.localScale = Vector3.one * (cellSize / 1f);
        }
    }

    private bool TryFindPath(Vector2Int start, Vector2Int goal, out List<Vector2Int> path)
    {
        return InGameMazeGridPathFinder.TryFindPath(walkable, start, goal, _mazeWidth, _mazeHeight, out path);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        float x = mazeTopLeftWorld.x + (cell.x - 1) * cellSize;
        float y = mazeTopLeftWorld.y - (cell.y - 1) * cellSize;
        return new Vector3(x, y, 0f);
    }
}
