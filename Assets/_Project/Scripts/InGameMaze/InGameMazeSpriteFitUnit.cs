using UnityEngine;

/// <summary>
/// Đặt trên child chứa SpriteRenderer: scale local để kích thước world của sprite = targetWorldSize (mặc định 1 unit, khớp PPU 100).
/// Root prefab chỉ nhận vị trí / scale cell từ InGameMazeController.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class InGameMazeSpriteFitUnit : MonoBehaviour
{
    [SerializeField] private float targetWorldSize = 1f;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    private void Apply()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return;

        Vector3 b = sr.sprite.bounds.size;
        if (b.x <= 0f || b.y <= 0f)
            return;

        transform.localScale = new Vector3(
            targetWorldSize / b.x,
            targetWorldSize / b.y,
            1f);
    }
}
