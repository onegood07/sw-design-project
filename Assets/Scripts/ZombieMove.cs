using UnityEngine;
using UnityEngine.Tilemaps;

public class ZombieMove : MonoBehaviour
{
    [SerializeField] public float speed = 2.5f;
    [SerializeField] public int grid = 5;
    [SerializeField] public int limitGrid = 3;
    [SerializeField] float attackDelay = 1f;
    [SerializeField] Tilemap collisionTilemap;

    float Timer = 0f;
    float stepDuration = 0.5f;
    float stepElapsed = 0f;
    float attackTimer = 0f;
    public float MoveInterval = 1f;

    bool isLive = true;
    bool findTarget = false;
    bool find = false;
    bool stepping = false;
    private GameObject heroGo;

    public Rigidbody2D target;
    Rigidbody2D zombie;

    // 내가 현재 점유 중인 셀과, 이동 중 예약해둔 다음 셀
    Vector2Int currentCell;
    Vector2Int reservedTargetCell; 
    bool hasReservedNext = false;
    
    Vector2Int spawnGrid;
    Vector2 stepStartPos, stepTargetPos;

    Vector2 GetCellCenter(Vector2 worldPos)
    {
        if (collisionTilemap != null)
        {
            var cell = collisionTilemap.WorldToCell(worldPos);
            Vector3 c = collisionTilemap.GetCellCenterWorld(cell);
            return new Vector2(c.x, c.y);
        }
        return new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
    }

    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell);
    }

    bool CanStep(Vector2Int moveDir)
    {
        Vector2 nextCenter = GetCellCenter((Vector2)zombie.position + (Vector2)moveDir);
        if (IsBlockedCell(nextCenter)) return false;

        Vector3Int cell3 = collisionTilemap.WorldToCell(nextCenter);
        Vector2Int nextCell = new Vector2Int(cell3.x, cell3.y);

        if (GridOccupancy.IsOccupied(collisionTilemap, nextCell)) return false;

        return true;
    }



    void Awake()
    {
        zombie = GetComponent<Rigidbody2D>();

        // 널가드
        var colGo = GameObject.Find("collision");
        if (colGo != null) collisionTilemap = colGo.GetComponent<Tilemap>();

        // 널가드
        heroGo = GameObject.Find("Hero");
        if (heroGo != null) target = heroGo.GetComponent<Rigidbody2D>();

        // 현재 위치를 셀 중앙으로 스냅하고, 점유 등록 시 명시 변환 사용
        Vector2 snapped = GetCellCenter(transform.position);
        transform.position = snapped;

        // (Vector2Int) 캐스트 금지 → 명시 변환으로 교체
        Vector3Int cur3 = collisionTilemap.WorldToCell(snapped);              
        currentCell = new Vector2Int(cur3.x, cur3.y);                           
        GridOccupancy.TryReserve(collisionTilemap, currentCell, this);

        spawnGrid = Vector2Int.FloorToInt(zombie.position);
    }
    void DoAttack()
    {
        heroGo.GetComponent<HeroStat>().decreaseHp(GetComponent<ZombieStat>().power);
        Debug.Log(heroGo.GetComponent<HeroStat>().hp);
        Debug.Log("Zombie Attack!"); 
    }
    void OnDestroy()
    {
        // 파괴 시 점유 반납 (예약해둔 셀도 동시 반납)
        GridOccupancy.Release(collisionTilemap, currentCell, this);
        if (hasReservedNext)
        {
            GridOccupancy.Release(collisionTilemap, reservedTargetCell, this);
            hasReservedNext = false;
        }
    }

    void FixedUpdate()
    {
        if (!isLive) return;

        // 스텝 중이면 보간
        if (stepping)
        {
            stepElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(stepElapsed / stepDuration);
            Vector2 p = Vector2.Lerp(stepStartPos, stepTargetPos, t);
            zombie.MovePosition(p);
            zombie.linearVelocity = Vector2.zero;

            if (t >= 1f)
            {
                //스텝 완료 후에만 이전 셀 반납 + 새 셀 확정
                GridOccupancy.Release(collisionTilemap, currentCell, this);
                currentCell = reservedTargetCell;
                hasReservedNext = false;
                stepping = false;
            }
            return;
        }

        // 스텝 시작 전 항상 중앙으로 스냅(코너/라인 타기 방지)
        Vector2 snapped = GetCellCenter(zombie.position);
        if (Vector2.Distance(zombie.position, snapped) > 0.001f)
        {
            Vector2 next = Vector2.MoveTowards(zombie.position, snapped, 100f * Time.fixedDeltaTime);
            zombie.MovePosition(next);
            zombie.linearVelocity = Vector2.zero;
            return;
        }

        // 타깃/거리 계산
        Vector2Int targetGrid = Vector2Int.FloorToInt(target.position);
        Vector2Int zombieGrid = Vector2Int.FloorToInt(zombie.position);
        Vector2Int diff = targetGrid - zombieGrid;
        int gridDist = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

        // 공격 거리
        if (gridDist <= 1)
        {
            zombie.linearVelocity = Vector2.zero;
            if (attackTimer > 0f) attackTimer -= Time.fixedDeltaTime;
            else { DoAttack(); attackTimer = attackDelay; }
            return;
        }

        findTarget = (gridDist <= grid);
        if (!findTarget && find) spawnGrid = Vector2Int.FloorToInt(zombie.position);
        find = findTarget;

        if (findTarget)
        {
            speed = 1100f;
            Vector2Int primary, secondary;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            {
                primary   = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
                secondary = new Vector2Int(0,            diff.y > 0 ? 1 : -1);
            }
            else
            {
                primary   = new Vector2Int(0,            diff.y > 0 ? 1 : -1);
                secondary = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
            }
            Vector2Int orthoA = (primary.x != 0) ? Vector2Int.up : Vector2Int.left;
            Vector2Int orthoB = (primary.x != 0) ? Vector2Int.down : Vector2Int.right;
            Vector2Int[] candidates = { primary, secondary, orthoA, orthoB };

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2Int tryDir = candidates[i];
                if (!CanStep(tryDir)) continue;

                // 이동 시작 전 다음 셀 예약
                // 좀비랑 플레이어가 동시에 같은 셀을 점유하게 될것을 방지 (낑김)
                Vector2Int nextCell = currentCell + tryDir;
                if (!GridOccupancy.TryReserve(collisionTilemap, nextCell, this)) continue;

                // 예약 성공 → 스텝 시작
                hasReservedNext    = true;                
                reservedTargetCell = nextCell;             

                stepStartPos  = GetCellCenter(zombie.position);
                stepTargetPos = GetCellCenter(zombie.position + (Vector2)tryDir);
                stepElapsed   = 0f;
                stepping      = true;
                zombie.linearVelocity = Vector2.zero;
                return;
            }

            // 여기까지 오면 이번 프레임에는 움직일 수 있는 방향이 없음(막힘/선점)
            zombie.linearVelocity = Vector2.zero;
            return;
        }
        else
        {
            speed = 2.5f;
            if (stepping) return;

            Timer += Time.fixedDeltaTime;
            if (Timer < MoveInterval) { zombie.linearVelocity = Vector2.zero; return; }
            Timer = 0f;

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            int start = Random.Range(0, 4);

            for (int i = 0; i < 4; i++)
            {
                Vector2Int tryDir = dirs[(start + i) % 4];

                // 움직임 체크
                Vector2Int nextGrid = zombieGrid + tryDir;
                int nextFromSpawn = Mathf.Abs(nextGrid.x - spawnGrid.x) + Mathf.Abs(nextGrid.y - spawnGrid.y);
                if (nextFromSpawn > limitGrid) continue;
                if (!CanStep(tryDir)) continue;

                // 배회도 동일하게 다음 셀 예약 
                Vector2Int nextCell = currentCell + tryDir;
                if (!GridOccupancy.TryReserve(collisionTilemap, nextCell, this)) continue;

                hasReservedNext    = true;                
                reservedTargetCell = nextCell;             

                // 스텝 시작
                stepStartPos  = GetCellCenter(zombie.position);
                stepTargetPos = GetCellCenter(zombie.position + (Vector2)tryDir);
                stepElapsed   = 0f;
                stepping      = true;
                zombie.linearVelocity = Vector2.zero;
                return;
            }

            zombie.linearVelocity = Vector2.zero;
        }
    }
}
