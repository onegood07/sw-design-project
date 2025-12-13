using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("Hero Spawn Settings")]
    public Tilemap spawnTilemap; // Hero 스폰용 타일맵
    public float heroMinDistance = 0.1f; // Hero 최소 스폰 거리
    public Vector3 outsidePosition = new Vector3(10.5f, 2.5f, 0f); // 쉘터 밖 위치 (쉘터 퇴장 시 설정할 좌표)

    [Header("NPC Prefab")]
    public GameObject survivorPrefab; // 생존자 NPC 프리팹
    public GameObject leaderPrefab; // 리더 NPC 프리팹

    [Header("Tilemaps")]
    public Tilemap groundTilemap; // 참고용 타일맵
    public Tilemap collisionTilemap; // 충돌 불가 타일맵 (벽, 장애물)
    public Tilemap leaderTilemap; // 리더 스폰 위치 타일맵
    public Tilemap survivorTilemap; // 생존자 스폰 위치 타일맵

    [Header("Spawn Settings")]
    public float minDistanceFromHero = 1.5f; // 생존자 스폰 시 Hero와 최소 거리

    [Header("Shelter Healing Settings")]
    [SerializeField] private float healPerSurvivor = 5f;   // 생존자 1명당 회복량
    [SerializeField] private float healInterval = 1.0f;    // 회복 주기 (초)

    private Coroutine healingRoutine;

    IEnumerator Start()
    {
        while (GameManager.Instance == null)
            yield return null;

        yield return new WaitForEndOfFrame();

        SpawnHero();
        SpawnLeader();
        SpawnSurvivors();
        
        StartShelterHealing(); 
    }

    void StartShelterHealing()
    {
        if (healingRoutine != null)
        {
            StopCoroutine(healingRoutine);
        }
        
        if (HeroStat.Instance != null)
        {
            healingRoutine = StartCoroutine(HealOverTimeCoroutine());
            Debug.Log($"[InsideShelterManager] 쉘터 HP 회복 시작. {healInterval}초마다 (생존자 수 x {healPerSurvivor})만큼 회복.");
        }
        else
        {
            Debug.LogWarning("[InsideShelterManager] HeroStat 인스턴스를 찾을 수 없어 쉘터 회복을 시작할 수 없습니다.");
        }
    }
    
    IEnumerator HealOverTimeCoroutine()
    {
        while (HeroStat.Instance != null && HeroStat.Instance.isSurvival)
        {
            yield return new WaitForSeconds(healInterval); 

            if (HeroStat.Instance != null && HeroStat.Instance.isSurvival)
            {
                int survivorCount = (GameManager.Instance != null) ? GameManager.Instance.SurvivorCount : 0;
                float amount = survivorCount * healPerSurvivor;

                if (amount > 0f)
                {
                    HeroStat.Instance.Heal(amount);
                    Debug.Log($"[InsideShelterManager] 쉘터 회복: 생존자 {survivorCount}명, {amount} HP 회복.");
                }
            }
        }
        healingRoutine = null;
        Debug.Log("[InsideShelterManager] HeroStat이 null이 되거나 사망하여 쉘터 회복 코루틴이 종료되었습니다.");
    }

    // MARK: 영웅 스폰 
    void SpawnHero()
    {
        if (spawnTilemap == null || HeroMoveControl.Instance == null) return;

        Vector3 spawnPos = Vector3.zero;

        // spawnTilemap에서 스폰 가능한 타일 찾기
        foreach (var pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (!spawnTilemap.HasTile(pos)) continue;
            spawnPos = spawnTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0f); // 중앙 좌표로 변환
            break; // 첫 번째 타일 위치 사용
        }

        // 강제 위치 이동
        HeroMoveControl.Instance.ForceMove(spawnPos);
        Debug.Log($"Hero 위치 재설정 완료: {spawnPos}");
    }

    // MARK: 리더 NPC 스폰 
    void SpawnLeader()
    {
        if (leaderPrefab == null)
        {
            Debug.LogError("[InsideShelterManager] Leader Prefab이 없습니다!");
            return;
        }

        List<Vector3> leaderPositions = new List<Vector3>();
        BoundsInt bounds = leaderTilemap.cellBounds;

        // leaderTilemap 내 유효 위치 수집
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!leaderTilemap.HasTile(pos)) continue;        // 타일이 없으면 제외
            if (collisionTilemap.HasTile(pos)) continue;     // 충돌 타일이면 제외

            Vector3 worldPos = leaderTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0); // 중앙 좌표
            leaderPositions.Add(worldPos);
        }

        if (leaderPositions.Count == 0)
        {
            Debug.LogWarning("[InsideShelterManager] 리더 스폰 타일이 없습니다!");
            return;
        }

        // 첫 번째 위치에 리더 스폰
        Vector3 spawnPos = leaderPositions[0];
        Instantiate(leaderPrefab, spawnPos, Quaternion.identity);
    }

    // MARK: 생존자 NPC 스폰 
    void SpawnSurvivors()
    {
        if (survivorPrefab == null)
        {
            Debug.LogError("[InsideShelterManager] Survivor Prefab이 없습니다!");
            return;
        }

        int survivorCount = GameManager.Instance.SurvivorScore; // 스폰할 생존자 수
        
        List<Vector3> survivorPositions = new List<Vector3>();
        BoundsInt bounds = survivorTilemap.cellBounds;

        // survivorTilemap 내 유효 위치 수집
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!survivorTilemap.HasTile(pos)) continue;
            if (collisionTilemap.HasTile(pos)) continue;

            Vector3 worldPos = survivorTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

            // 영웅과 가까운 위치는 제외
            if (HeroMoveControl.Instance != null &&
                Vector3.Distance(worldPos, HeroMoveControl.Instance.transform.position) < minDistanceFromHero)
                continue;

            survivorPositions.Add(worldPos);
        }

        if (survivorPositions.Count == 0)
        {
            Debug.LogWarning("[InsideShelterManager] 생존자 스폰 가능한 타일이 없습니다!");
            return;
        }

        // 랜덤 위치로 생존자 스폰
        for (int i = 0; i < survivorCount; i++)
        {
            Vector3 spawnPos = survivorPositions[Random.Range(0, survivorPositions.Count)];
            Instantiate(survivorPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[InsideShelterManager] {survivorCount}명의 생존자를 스폰했습니다.");
    }

    // MARK: Hero가 쉘터 밖으로 이동했을 때
    public void OnHeroExitShelter()
    {
        // 🌟 [수정된 부분] 쉘터 밖으로 나갈 때 회복 코루틴 중지
        if (healingRoutine != null)
        {
            StopCoroutine(healingRoutine);
            healingRoutine = null;
            Debug.Log("[InsideShelterManager] 쉘터 퇴장: HP 회복 코루틴 중지.");
        }
        
        if (HeroMoveControl.Instance != null)
        {
            HeroMoveControl.Instance.ExitShelter(outsidePosition); // 지정 위치로 이동
        }
    }
}