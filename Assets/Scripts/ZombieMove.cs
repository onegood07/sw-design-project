using UnityEngine;
using UnityEngine.Tilemaps;

public class ZombieMove : MonoBehaviour
{
    [SerializeField] public float speed = 2.5f;     // 추적 속도(유닛/초)
    [SerializeField] public int grid = 5;        // 타겟을 쫓기 시작/유지할 그리드
    [SerializeField] public int limitGrid = 3;   // 스폰 위치에서 허용하는 최대 이동 그리드
    [SerializeField] float attackDelay = 1f; // 공격 딜레이(초). 요구사항: 1로 줄임(에디터에서 조정 가능)
    [SerializeField] Tilemap collisionTilemap;
    
    float Timer = 0f; 
    float stepDuration = 0.5f; // 한 칸 이동 시간
    float stepElapsed = 0f;
    float attackTimer = 0f; // 공격 쿨 관리용
    public float MoveInterval = 1f;

    bool isLive = true;
    bool findTarget = false;
    bool find = false;
    bool stepping = false;

    public Rigidbody2D target;
    Rigidbody2D zombie;
    Vector2Int spawnGrid;
    Vector2 stepStartPos, stepTargetPos;
    ZombieStat zombieStat;

    // ─────────────────────────────────────────────────────────────
    // [추가] 항상 셀 중앙으로 스냅하기 위한 헬퍼
    //  - 왜? 경계선/모서리에 걸린 상태에서 WorldToCell 판정이 들쭉날쭉해 벽 아래서 막히거나
    //    라인을 타고 이동하는 문제가 생김. 중앙 정렬 후에만 이동을 시작하도록 강제.
    // ─────────────────────────────────────────────────────────────
    Vector2 GetCellCenter(Vector2 worldPos)
    {
        // 타일맵이 있으면 타일 기준 센터, 없으면 0.5 오프셋(셀 사이즈 1 가정)
        if (collisionTilemap != null)
        {
            var cell = collisionTilemap.WorldToCell(worldPos);
            Vector3 c = collisionTilemap.GetCellCenterWorld(cell);
            return new Vector2(c.x, c.y);
        }
        return new Vector2(Mathf.Floor(worldPos.x) + 0.5f, Mathf.Floor(worldPos.y) + 0.5f);
    }

    // ─────────────────────────────────────────────────────────────
    // [기존] 타일 막힘 체크
    //  - 유지: 포인트 판정(최소 수정)
    //  - 스냅 로직과 결합되면 중앙만 밟으므로 안정적으로 동작
    // ─────────────────────────────────────────────────────────────
    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell); // 타일 있으면 '막힘'
    }

    // ─────────────────────────────────────────────────────────────
    // [수정] CanStep: 다음 칸의 "셀 중앙" 기준으로 막힘 판정
    //  - 왜? 현재 위치가 중앙에 정렬되어 있으면 +moveDir 하면 정확히 다음 셀 중앙이 됨.
    //  - 중앙만 밟게 강제하므로 라인 타기/코너 긁힘이 크게 줄어듦.
    // ─────────────────────────────────────────────────────────────
    bool CanStep(Vector2Int moveDir)
    {
        Vector2 nextCenter = GetCellCenter((Vector2)zombie.position + (Vector2)moveDir);
        return !IsBlockedCell(nextCenter);
    }
    void DoAttack()
    {
        // TODO: 애니메이션 트리거, 히트박스 활성화, 데미지 적용 등
        Debug.Log("Zombie Attack!");
    }

    void Awake()
    {
        zombieStat = GetComponent<ZombieStat>();
        zombie = GetComponent<Rigidbody2D>();
        spawnGrid = Vector2Int.FloorToInt(zombie.position);
        collisionTilemap = GameObject.Find("collision").GetComponent<Tilemap>();
        target = GameObject.Find("Hero").GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!isLive) return;

        // ─────────────────────────────────────────────────────────
        // [추가] 스텝 시작 전에 항상 "그리드 중앙으로 스냅"
        //  - 왜? 경계선/충돌영역 아래에 걸려 있는 상태에서 바로 이동을 시도하면
        //       한 칸 아래부터 막히거나 라인을 타는 현상이 발생.
        //  - stepping==false일 때 먼저 중앙으로 붙이고, 도중이면 보간 우선.
        // ─────────────────────────────────────────────────────────
        if (!stepping)
        {
            Vector2 snapped = GetCellCenter(zombie.position);
            // 약간 떨어져 있으면 먼저 중앙에 붙이고 그 프레임은 종료(최소 텔레포트 느낌 방지용 MoveTowards)
            if (Vector2.Distance(zombie.position, snapped) > 0.001f)
            {
                Vector2 next = Vector2.MoveTowards(zombie.position, snapped, 100f * Time.fixedDeltaTime);
                zombie.MovePosition(next);
                zombie.linearVelocity = Vector2.zero; // linearVelocity 대신 velocity 권장
                return;
            }
        }

        // 한 칸 이동 보간 중
        if (stepping)
        {
            stepElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(stepElapsed / stepDuration);
            Vector2 p = Vector2.Lerp(stepStartPos, stepTargetPos, t);
            zombie.MovePosition(p);
            zombie.linearVelocity = Vector2.zero;

            if (t >= 1f) stepping = false;
            return;
        }

        // 현재/타겟 그리드 좌표
        Vector2Int targetGrid = Vector2Int.FloorToInt(target.position);
        Vector2Int zombieGrid = Vector2Int.FloorToInt(zombie.position);

        Vector2Int diff = targetGrid - zombieGrid;
        int gridDist = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

        if (gridDist <= 1)
        {
            stepping = false;               // 이동 중단
            zombie.linearVelocity = Vector2.zero; // 멈춤

            if (attackTimer > 0f)
            {
                attackTimer -= Time.fixedDeltaTime;
            }
            else
            {
                DoAttack();                 // 실제 데미지/이펙트는 여기서
                attackTimer = attackDelay;  // 쿨다운 리셋
            }
            return; // 공격 상태에서는 이동 로직 생략
        }

        findTarget = (gridDist <= grid);

        if (!findTarget && find)
        {
            // 추적 끊긴 지점에서 리쉬 중심 갱신(의도 유지)
            spawnGrid = Vector2Int.FloorToInt(zombie.position);
        }
        find = findTarget;

        if (findTarget)
        {
            // Debug.Log("find!!!");
            // ─────────────────────────────────────────────────────
            // [수정] 추격 시에도 "연속 밀기" 대신 "한 칸 스텝"으로 통일
            //  - 왜? 연속 MovePosition은 라인을 타거나 코너에서 관통 위험이 있음.
            //       스텝 방식(셀 중앙 → 셀 중앙)으로 바꾸면 그리드 정렬이 유지됨.
            // ─────────────────────────────────────────────────────
            Vector2Int primary, secondary;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            {
                primary   = new Vector2Int(diff.x > 0 ? 1 : -1, 0);            // x 우선
                secondary = new Vector2Int(0,            diff.y > 0 ? 1 : -1); // y 보조
            }
            else
            {
                primary   = new Vector2Int(0,            diff.y > 0 ? 1 : -1); // y 우선
                secondary = new Vector2Int(diff.x > 0 ? 1 : -1, 0);            // x 보조
            }

            Vector2Int orthoA, orthoB;
            if (primary.x != 0) { orthoA = Vector2Int.up; orthoB = Vector2Int.down; }
            else                 { orthoA = Vector2Int.left; orthoB = Vector2Int.right; }

            Vector2Int[] candidates = { primary, secondary, orthoA, orthoB };

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2Int tryDir = candidates[i];
                if (!CanStep(tryDir)) continue;

                // 한 칸 스텝 시작(셀 중앙 → 셀 중앙)
                stepStartPos  = GetCellCenter(zombie.position);                    // 중앙에서 시작
                stepTargetPos = GetCellCenter(zombie.position + (Vector2)tryDir);  // 다음 셀 중앙
                stepElapsed   = 0f;
                stepping      = true;
                zombie.linearVelocity = Vector2.zero;
                break;
            }
            return;
        }
        else
        {
            // Debug.Log("haa....");
            // 랜덤 배회: 기존 로직 유지(단, CanStep/스냅의 효과를 그대로 받음)
            if (stepping) return;

            Timer += Time.fixedDeltaTime;
            if (Timer < MoveInterval)
            {
                zombie.linearVelocity = Vector2.zero;
                return;
            }
            Timer = 0f;

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            int start = Random.Range(0, 4);
            Vector2Int moveDir = Vector2Int.zero;
            bool foundValidDir = false;

            for (int i = 0; i < 4; i++)
            {
                Vector2Int tryDir = dirs[(start + i) % 4];

                Vector2Int nextGrid = zombieGrid + tryDir;
                int nextFromSpawn =
                    Mathf.Abs(nextGrid.x - spawnGrid.x) +
                    Mathf.Abs(nextGrid.y - spawnGrid.y);

                if (nextFromSpawn > limitGrid) continue;
                if (!CanStep(tryDir)) continue;

                moveDir = tryDir;
                foundValidDir = true;
                break;
            }

            if (!foundValidDir)
            {
                zombie.linearVelocity = Vector2.zero;
                return;
            }

            // 한 칸 스텝 시작(셀 중앙 → 셀 중앙)
            stepStartPos  = GetCellCenter(zombie.position);                   // [중요] 시작 중앙
            stepTargetPos = GetCellCenter(zombie.position + (Vector2)moveDir); // 다음 셀 중앙
            stepElapsed   = 0f;
            stepping      = true;

            zombie.linearVelocity = Vector2.zero;
            return;
        }
    }
}
