using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemOccupancy : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // 충돌/그리드 기준 타일맵
    private Vector2Int myCell;
    private bool reserved = false;

    void Awake()
    {
        // 타일맵 자동 지정
        if (tilemap == null)
        {
            var colGo = GameObject.Find("collision");
            if (colGo != null) tilemap = colGo.GetComponent<Tilemap>();
        }

        if (tilemap == null)
        {
            Debug.LogError("[ItemOccupancy] Tilemap not assigned.");
        }
    }

    void Start()
    {
        ReserveMyCell();
    }

    void OnDestroy()
    {
        ReleaseMyCell();
    }

    // ===============================
    //   좌표 점유 (생성 시 자동 호출)
    // ===============================
    private void ReserveMyCell()
    {
        if (tilemap == null) return;

        Vector3Int cell = tilemap.WorldToCell(transform.position);
        myCell = new Vector2Int(cell.x, cell.y);

        if (GridOccupancy.TryReserve(tilemap, myCell, this))
        {
            reserved = true;
            //Debug.Log($"[Item] Reserved cell {myCell}");
        }
        else
        {
            Debug.LogWarning($"[Item] Failed to reserve cell {myCell}");
        }
    }

    // ===============================
    //   좌표 반납 (삭제 시 자동 호출)
    // ===============================
    private void ReleaseMyCell()
    {
        if (!reserved || tilemap == null) return;

        GridOccupancy.Release(tilemap, myCell, this);
        reserved = false;
        //Debug.Log($"[Item] Released cell {myCell}");
    }
}
