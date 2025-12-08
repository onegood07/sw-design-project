using UnityEngine;

/// <summary>
/// Hero 하위에 있는 Pistol_S / Pistol_N / Pistol_W / Pistol_E 오브젝트의
/// 활성/비활성을 관리해서, 무기 장착 여부와 바라보는 방향에 따라
/// 한 방향만 보이도록 제어하는 스크립트.
/// 
/// 사용법:
/// - Hero 오브젝트에 이 스크립트를 붙인다.
/// - 인스펙터에서 pistol_S/N/W/E 에 Hero 자식 오브젝트를 연결한다.
/// - HeroMoveControl 에서 SetWeaponEquipped / UpdateDirection 을 호출한다.
/// </summary>
public class HeroWeaponVisual : MonoBehaviour
{
    [Header("총 방향별 오브젝트 (Hero 하위 자식)")]
    [SerializeField] private GameObject pistol_S;
    [SerializeField] private GameObject pistol_N;
    [SerializeField] private GameObject pistol_W;
    [SerializeField] private GameObject pistol_E;

    private bool hasWeapon = false;
    private bool isRolling = false; // 구르는 중에는 총을 숨기기 위한 플래그

    private enum Direction
    {
        South,
        North,
        West,
        East
    }

    private Direction facingDir = Direction.South; // 기본은 아래 방향

    private void Awake()
    {
        // 시작할 때는 항상 전부 끄고, hasWeapon=false 상태 유지
        UpdatePistolVisible();
    }

    /// <summary>
    /// 무기 장착/해제 상태를 갱신한다.
    /// </summary>
    public void SetWeaponEquipped(bool equipped)
    {
        hasWeapon = equipped;
        UpdatePistolVisible();
    }

    /// <summary>
    /// 구르기 시작/종료 상태를 갱신한다.
    /// 구르는 동안에는 무기 장착 여부와 상관없이 총을 숨긴다.
    /// </summary>
    public void SetRolling(bool rolling)
    {
        isRolling = rolling;
        UpdatePistolVisible();
    }

    /// <summary>
    /// Hero 가 바라보는 방향 벡터를 전달받아
    /// South / North / West / East 중 하나로 스냅하고, 해당 방향만 켠다.
    /// </summary>
    public void UpdateDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return;

        // 대각선 입력이 들어와도, 더 큰 축을 기준으로 4방향으로 스냅
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            facingDir = dir.x < 0f ? Direction.West : Direction.East;
        }
        else
        {
            facingDir = dir.y < 0f ? Direction.South : Direction.North;
        }

        UpdatePistolVisible();
    }

    /// <summary>
    /// 현재 hasWeapon + facingDir 상태에 맞춰 Pistol_* 오브젝트들의
    /// SetActive 여부를 갱신한다.
    /// </summary>
    private void UpdatePistolVisible()
    {
        // 전부 끄고 시작
        if (pistol_S != null) pistol_S.SetActive(false);
        if (pistol_N != null) pistol_N.SetActive(false);
        if (pistol_W != null) pistol_W.SetActive(false);
        if (pistol_E != null) pistol_E.SetActive(false);

        // 무기 미장착이거나, 구르는 중이면 그대로 종료 (전부 비활성 유지)
        if (!hasWeapon || isRolling)
            return;

        // 무기 장착 상태면 현재 바라보는 방향만 켜기
        switch (facingDir)
        {
            case Direction.South:
                if (pistol_S != null) pistol_S.SetActive(true);
                break;
            case Direction.North:
                if (pistol_N != null) pistol_N.SetActive(true);
                break;
            case Direction.West:
                if (pistol_W != null) pistol_W.SetActive(true);
                break;
            case Direction.East:
                if (pistol_E != null) pistol_E.SetActive(true);
                break;
        }
    }
}


