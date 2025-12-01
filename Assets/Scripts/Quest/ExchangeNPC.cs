using UnityEngine;

/// <summary>
/// 교환 기능을 제공하는 NPC입니다. 
/// 상호작용 시 교환 UI를 열고, 대화 중인 좀비들의 이동을 잠금/해제합니다.
/// NOTE: ExchangeManager의 OpenTradeUI(ExchangeNPC npc, QuestData[] recipes) 함수를 호출하도록 수정되었습니다.
/// </summary>
public class ExchangeNPC : MonoBehaviour, IInteractable
{
    // ⭐ 1. 이 NPC가 제공할 교환 레시피 목록을 Inspector에서 연결합니다.
    [Header("Trade Recipes")]
    public QuestData[] availableRecipes; 

    private ZombieMove[] zombies;          // ZombieMove 스크립트 참조
    private ZombieNavMove[] navZombies;    // ZombieNavMove 스크립트 참조

    void Start()
    {
        // 씬에서 모든 좀비 컴포넌트들을 찾습니다.
        zombies = FindObjectsByType<ZombieMove>(FindObjectsSortMode.None);
        navZombies = FindObjectsByType<ZombieNavMove>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// 플레이어가 상호작용할 때 교환 UI를 열고 좀비의 이동을 멈춥니다.
    /// </summary>
    public void OnInteract()
    {
        // 1. 모든 좀비 이동 잠금
        SetZombiesMovement(true);

        // 2. ExchangeManager를 통해 UI 열기 요청
        if (ExchangeManager.Instance != null)
        {
            // ⭐ 2. 수정: 자신의 레시피 목록(availableRecipes)을 ExchangeManager에게 전달합니다.
            ExchangeManager.Instance.OpenTradeUI(this, availableRecipes);
            Debug.Log($"[ExchangeNPC] {gameObject.name}: 교환 UI 열기 요청 성공. 레시피 개수: {availableRecipes.Length}");
        }
        else
        {
             Debug.LogError("[ExchangeNPC] ExchangeManager.Instance를 찾을 수 없습니다. 씬에 ExchangeManager 오브젝트가 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 교환 UI가 닫힐 때 ExchangeManager에 의해 호출되며, 좀비의 이동을 다시 허용합니다.
    /// </summary>
    public void OnExchangeEnd()
    {
        Debug.Log($"[ExchangeNPC] {gameObject.name}: 교환 UI 종료, 좀비 이동 허용");
        SetZombiesMovement(false);
    }
    
    /// <summary>
    /// 모든 좀비의 이동 상태를 설정합니다.
    /// </summary>
    private void SetZombiesMovement(bool lockMovement)
    {
        foreach (var zombie in zombies) 
        {
            // Null 체크는 항상 안전합니다.
            if (zombie != null) zombie.isInDialogue = lockMovement;
        }
        foreach (var zombie in navZombies) 
        {
            if (zombie != null) zombie.isInDialogue = lockMovement;
        }
    }
}