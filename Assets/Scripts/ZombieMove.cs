using UnityEngine;
using UnityEngine.Tilemaps;

public class ZombieMove : MonoBehaviour
{
    [SerializeField] public float speed = 1f;   // speed를 "속도 배수"로 사용 (1 = 기본속도)
    [SerializeField] public int grid = 5;
    [SerializeField] public int limitGrid = 5;
    [SerializeField] float attackDelay = 1f;
    [SerializeField] Tilemap collisionTilemap;

    Animator animator;
    float baseDuration = 0.666f;   // 애니메이션 기준 한 칸 이동 기본 시간
    float stepDuration = 0.5f;     // 실제 한 칸 이동에 사용할 시간 (매번 baseDuration / speed 로 세팅)
    float stepElapsed = 0f;
    float attackTimer = 0f;
    float attackMoveLockTime = 1f; // 공격 후 이동 금지 시간(초)
    float attackMoveLockTimer = 0f; // 남은 이동 금지 시간

    public float MoveInterval = 1f;
    float Timer = 0f;

    bool isLive = true;
    bool findTarget = false;
    bool find = false;
    bool stepping = false;
    private GameObject heroGo;

    public Rigidbody2D target;
    Rigidbody2D zombie;

    Vector2Int lastMoveDir = Vector2Int.down;  // 마지막 이동 방향 유지

    // 내가 현재 점유 중인 셀과, 이동 중 예약해둔 다음 셀
    Vector2Int currentCell;
    Vector2Int reservedTargetCell;
    bool hasReservedNext = false;

    Vector2Int spawnGrid;
    Vector2 stepStartPos, stepTargetPos;

    // 직전 프레임에 공격 범위 안에 있었는지
    bool inAttackRange = false;

    public bool isInDialogue = false; // 플레이어가 NPC와 대화 중인지 확인

    // 이동 방향에 따라 애니메이션 bool을 설정하는 함수
    void SetAnimDirection(Vector2Int dir)
    {
        bool left  = false;
        bool up    = false;
        bool down  = false;
        bool right = false;

        // ★ 수정: X, Y 각각 단순하게 방향 판별
        // X축 이동 판단
        if (dir.x < 0)
            left = true;
        else if (dir.x > 0)
            right = true;

        // Y축 이동 판단
        if (dir.y > 0)
            up = true;
        else if (dir.y < 0)
            down = true;

        animator.SetBool("RIGHT", right);
        animator.SetBool("LEFT",  left);
        animator.SetBool("UP",    up);
        animator.SetBool("DOWN",  down);
    }

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

    // currentCell 같은 정수 셀을 바로 월드좌표로 바꾸는 함수
    Vector2 CellCenterWorld(Vector2Int cell)
    {
        if (collisionTilemap == null)
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        Vector3 c = collisionTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        return new Vector2(c.x, c.y);
    }

    bool IsBlockedCell(Vector2 worldPos)
    {
        if (collisionTilemap == null) return false;
        Vector3Int cell = collisionTilemap.WorldToCell(worldPos);
        return collisionTilemap.HasTile(cell);
    }

    // 좀비의 "현재 셀(currentCell)" 기준으로만 한 칸 이동 가능 여부 체크
    bool CanStep(Vector2Int moveDir)
    {
        Vector2Int nextCell = currentCell + moveDir;          // 다음 셀(정수)
        Vector2 nextCenter = CellCenterWorld(nextCell);       // 그 셀의 센터

        if (IsBlockedCell(nextCenter)) return false;          // 타일(벽) 체크
        if (GridOccupancy.IsOccupied(collisionTilemap, nextCell)) return false; // 점유 체크

        return true;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        zombie   = GetComponent<Rigidbody2D>();

        var colGo = GameObject.Find("collision");
        if (colGo != null) collisionTilemap = colGo.GetComponent<Tilemap>();

        heroGo = GameObject.Find("Hero");
        if (heroGo != null) target = heroGo.GetComponent<Rigidbody2D>();

        // 시작 위치를 그리드 센터에 스냅 + currentCell 초기화
        Vector2 snapped = GetCellCenter(transform.position);
        transform.position = snapped;
        zombie.MovePosition(snapped);

        Vector3Int cur3 = collisionTilemap.WorldToCell(snapped);
        currentCell = new Vector2Int(cur3.x, cur3.y);
        GridOccupancy.TryReserve(collisionTilemap, currentCell, this);

        spawnGrid = currentCell;

        SetAnimDirection(Vector2Int.zero);
    }

    void OnDestroy()
    {
        GridOccupancy.Release(collisionTilemap, currentCell, this);
        if (hasReservedNext)
        {
            GridOccupancy.Release(collisionTilemap, reservedTargetCell, this);
            hasReservedNext = false;
        }
    }
    // 임의 해제 필요 시 호출할 함수
    public void CallDestroy()
    {
        GridOccupancy.Release(collisionTilemap, currentCell, this);
        if (hasReservedNext)
        {
            GridOccupancy.Release(collisionTilemap, reservedTargetCell, this);
            hasReservedNext = false;
        }
    }

    void DoAttack()
    {
        heroGo.GetComponent<HeroStat>().decreaseHp(GetComponent<ZombieStat>().power);
        Debug.Log(heroGo.GetComponent<HeroStat>().hp);
        Debug.Log("Zombie Attack!");

        attackMoveLockTimer = attackMoveLockTime;
    }

    // 메인 루프
    void FixedUpdate()
    {
       if (!isLive || isInDialogue) // isInDialogue일 때 좀비 움직임 정지
    {
        // Rigidbody 속도 초기화 (물리적 움직임 방지)
        if (zombie != null)
            zombie.linearVelocity = Vector2.zero;
        
        // 멈춘 상태에서 이전 방향으로 애니메이션 유지 (Idle 상태처럼 보이게)
        SetAnimDirection(lastMoveDir); 
        
        return;
    }
        // 공격 후 이동 잠금
        if (attackMoveLockTimer > 0f)
        {
            attackMoveLockTimer -= Time.fixedDeltaTime;
            zombie.linearVelocity = Vector2.zero;

            // 플레이어 보고 있었다면 마지막 방향 유지
            SetAnimDirection(lastMoveDir);
            return;
        }

        // 한 칸 이동 중(스텝)
        if (stepping)
        {
            stepElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(stepElapsed / stepDuration);
            Vector2 p = Vector2.Lerp(stepStartPos, stepTargetPos, t);
            zombie.MovePosition(p);
            zombie.linearVelocity = Vector2.zero;

            if (t >= 1f)
            {
                // 이전 셀 점유 해제 + 현재 셀 교체
                GridOccupancy.Release(collisionTilemap, currentCell, this);
                currentCell = reservedTargetCell;   // 새 셀로 교체
                hasReservedNext = false;
                stepping = false;

                // 최종 위치를 셀 센터로 딱 고정
                zombie.MovePosition(stepTargetPos);
            }
            return;
        }

        // 플레이어 위치/거리 계산
        if (target == null)
        {
            zombie.linearVelocity = Vector2.zero;
            SetAnimDirection(Vector2Int.zero);
            inAttackRange = false;
            return;
        }

        Vector2Int targetGrid = Vector2Int.FloorToInt(target.position);
        Vector2Int diff       = targetGrid - currentCell;
        int gridDist          = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

        // 1칸 거리 이내면 공격
        if (gridDist <= 1)
        {
            zombie.linearVelocity = Vector2.zero;
            SetAnimDirection(Vector2Int.zero);

            // 이번 프레임에 처음으로 공격 범위에 들어온 경우 → 즉시 공격
            if (!inAttackRange)
            {
                DoAttack();                 // 바로 1타
                attackTimer = attackDelay;  // 이후 타격은 딜레이 적용
                inAttackRange = true;
                return;
            }

            // 이미 범위 안에 있었던 상태라면 → 쿨타임 보고 추가 공격
            if (attackTimer > 0f)
            {
                attackTimer -= Time.fixedDeltaTime;
            }
            else
            {
                DoAttack();
                attackTimer = attackDelay;
            }

            inAttackRange = true;
            return;
        }
        else
        {
            // 범위 밖으로 나가면 상태 리셋
            inAttackRange = false;
        }

        // 추적 범위 판단
        findTarget = (gridDist <= grid);
        if (!findTarget && find) spawnGrid = currentCell;
        find = findTarget;

        // 1) 플레이어 추적 모드
        if (findTarget)
        {
            // 우선 방향(가로/세로)과 보조 방향
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

            Vector2Int orthoA = (primary.x != 0) ? Vector2Int.up   : Vector2Int.left;
            Vector2Int orthoB = (primary.x != 0) ? Vector2Int.down : Vector2Int.right;
            Vector2Int[] candidates = { primary, secondary, orthoA, orthoB };

            // currentCell 기준으로만 다음 셀/타깃 계산
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2Int tryDir = candidates[i];
                if (!CanStep(tryDir)) continue;

                Vector2Int nextCell = currentCell + tryDir;
                if (!GridOccupancy.TryReserve(collisionTilemap, nextCell, this)) continue;

                hasReservedNext    = true;
                reservedTargetCell = nextCell;

                stepStartPos  = CellCenterWorld(currentCell);  // 현재 셀 센터
                stepTargetPos = CellCenterWorld(nextCell);     // 다음 셀 센터
                stepElapsed   = 0f;
                stepDuration  = baseDuration / speed;  // speed를 반영해 한 칸 이동 시간 계산
                stepping      = true;
                zombie.linearVelocity = Vector2.zero;

                SetAnimDirection(tryDir);
                lastMoveDir = tryDir;

                return;
            }

            // 네 방향 모두 막힌 경우: 그 자리에서 멈추되, 방향은 유지
            zombie.linearVelocity = Vector2.zero;
            SetAnimDirection(lastMoveDir);
            return;
        }

        // 2) 배회(랜덤 이동) 모드
        if (stepping) return;

        Timer += Time.fixedDeltaTime;
        if (Timer < MoveInterval)
        {
            zombie.linearVelocity = Vector2.zero;
            SetAnimDirection(Vector2Int.zero);
            return;
        }
        Timer = 0f;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int start = Random.Range(0, 4);

        for (int i = 0; i < 4; i++)
        {
            Vector2Int tryDir = dirs[(start + i) % 4];

            Vector2Int nextGrid = currentCell + tryDir;
            int nextFromSpawn = Mathf.Abs(nextGrid.x - spawnGrid.x) + Mathf.Abs(nextGrid.y - spawnGrid.y);
            if (nextFromSpawn > limitGrid) continue;
            if (!CanStep(tryDir)) continue;

            Vector2Int nextCell = currentCell + tryDir;
            if (!GridOccupancy.TryReserve(collisionTilemap, nextCell, this)) continue;

            hasReservedNext    = true;
            reservedTargetCell = nextCell;

            stepStartPos  = CellCenterWorld(currentCell);
            stepTargetPos = CellCenterWorld(nextCell);
            stepElapsed   = 0f;
            stepDuration  = baseDuration / speed;  // ★ 수정: 배회 모드에서도 동일하게 speed 반영
            stepping      = true;
            zombie.linearVelocity = Vector2.zero;

            SetAnimDirection(tryDir);
            lastMoveDir = tryDir;

            return;
        }

        zombie.linearVelocity = Vector2.zero;
        SetAnimDirection(Vector2Int.zero);
    }
}
