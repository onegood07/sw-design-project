using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 교환 로직을 담당하는 싱글톤 관리자입니다.
/// 교환 UI 활성화/비활성화 및 NPC와의 연동을 관리합니다.
/// </summary>
public class ExchangeManager : MonoBehaviour
{
    public static ExchangeManager Instance;

    [Header("UI References")]
    public ExchangeUI exchangeUI;

    // 상호작용 중인 NPC
    public ExchangeNPC currentInteractingNPC;
    public QuestData[] currentRecipes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (exchangeUI == null)
        {
            Debug.LogError("[ExchangeManager] ExchangeUI 참조가 누락되었습니다. 인스펙터에 연결해주세요!");
            return;
        }

        exchangeUI.Hide();
    }

    // ───────────────────────────────
    /// <summary>
    /// 대상 NPC & 레시피 전달 후 UI 활성화
    /// </summary>
    public void OpenTradeUI(ExchangeNPC npc, QuestData[] recipes)
    {
        if (exchangeUI != null)
        {
            currentInteractingNPC = npc;
            currentRecipes = recipes;

            exchangeUI.Show(currentRecipes);

            // ⭐ 추가: GameManager에 상호작용 시작 알림 (좀비/플레이어 행동 정지)
            GameManager.Instance?.StartInteraction();
            Debug.Log("[ExchangeManager] GameManager.StartInteraction() 호출 완료.");

            Debug.Log($"[ExchangeManager] 교환 UI 활성화 및 {recipes.Length}개의 레시피 로드.");
        }
    }

    // ───────────────────────────────
    /// <summary>
    /// 교환 UI 종료 요청
    /// </summary>
    public void CloseTradeUI()
    {
        if (exchangeUI != null)
        {
            exchangeUI.Hide();

            if (currentInteractingNPC != null)
            {
                currentInteractingNPC.OnExchangeEnd();
                currentInteractingNPC = null;
            }

            currentRecipes = null;
            
            // ⭐ 추가: GameManager에 상호작용 종료 알림 (좀비/플레이어 행동 재개)
            GameManager.Instance?.EndInteraction();
            Debug.Log("[ExchangeManager] GameManager.EndInteraction() 호출 완료.");

            Debug.Log("[ExchangeManager] 교환 UI 비활성화 및 NPC 종료 알림");
        }
    }

    // ───────────────────────────────
    /// <summary>
    /// 교환 성공 시 레시피 목록에서 가용성 갱신
    /// </summary>
    public void NotifyTradeSuccess()
    {
        if (exchangeUI != null)
        {
            exchangeUI.UpdateAllRecipeItemsAvailability();
        }
    }
}