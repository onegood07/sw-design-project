using UnityEngine;

/// <summary>
/// 교환 기능을 제공하는 NPC입니다. 
/// NOTE: ExchangeManager의 OpenTradeUI(ExchangeNPC npc, QuestData[] recipes) 함수를 호출하도록 수정되었습니다.
/// </summary>
public class ExchangeNPC : MonoBehaviour, IInteractable
{
    [Header("Trade Recipes")]
    public QuestData[] availableRecipes; 

    // ❌ 좀비 관련 변수 및 Start 함수 제거
    // private ZombieMove[] zombies;          
    // private ZombieNavMove[] navZombies;    

    // ❌ Start() 함수 제거 (좀비를 찾을 필요가 없어짐)
    // void Start() { }

    /// <summary>
    /// 플레이어가 상호작용할 때 교환 UI를 열고 좀비의 이동을 멈춥니다.
    /// </summary>
    public void OnInteract()
    {
        // 1. ❌ 모든 좀비 이동 잠금 로직 제거 (ExchangeManager가 처리)
        // SetZombiesMovement(true);

        // 2. ExchangeManager를 통해 UI 열기 요청
        if (ExchangeManager.Instance != null)
        {
            // 자신의 레시피 목록(availableRecipes)을 ExchangeManager에게 전달합니다.
            // ExchangeManager 내부에서 GameManager.StartInteraction() 호출됨
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
        Debug.Log($"[ExchangeNPC] {gameObject.name}: 교환 UI 종료");
        // ❌ 좀비 이동 허용 로직 제거 (ExchangeManager가 처리)
        // SetZombiesMovement(false);
    }
    
    // ❌ SetZombiesMovement 함수 제거
    // private void SetZombiesMovement(bool lockMovement) { ... }
}