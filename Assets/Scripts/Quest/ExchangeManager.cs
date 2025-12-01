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
    // ExchangeUI가 ExchangeSubmitSlot 및 ExchangeRecipeItem 목록을 모두 관리합니다.
    public ExchangeUI exchangeUI; 

    // ⭐ 핵심: ExchangeSubmitSlot 참조 (private으로 관리)
    public ExchangeSubmitSlot submitSlot; 

    // 상호작용 중인 NPC 참조 
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
        
        // ⭐ 핵심 연결 로직: ExchangeUI 하위에서 ExchangeSubmitSlot을 자동으로 찾아서 연결합니다.
        // true 인자는 비활성화된 오브젝트에서도 찾게 해줍니다.
        submitSlot = exchangeUI.gameObject.GetComponentInChildren<ExchangeSubmitSlot>(true);
        
        if (submitSlot == null)
        {
            // ⭐ 문제 진단용 로그: 슬롯을 찾지 못하면 강력하게 에러 로그를 띄웁니다.
            Debug.LogError("[ExchangeManager] ExchangeSubmitSlot 컴포넌트를 ExchangeUI 하위에서 찾을 수 없습니다! 계층 구조를 확인해주세요.");
        } else {
            Debug.Log("[ExchangeManager] ExchangeSubmitSlot을 성공적으로 찾았습니다.");
        }
        
        exchangeUI.Hide();
    }

    /// <summary>
    /// ExchangeNPC로부터 호출되며, 교환 UI를 열고 레시피 목록을 전달합니다.
    /// </summary>
    public void OpenTradeUI(ExchangeNPC npc, QuestData[] recipes) 
    {
        if (exchangeUI != null)
        {
            currentInteractingNPC = npc; 
            currentRecipes = recipes; 
            
            exchangeUI.Show(currentRecipes); 
            
            // 교환 UI 열 때 제출 슬롯을 초기화합니다.
            if(submitSlot != null) submitSlot.ClearSlot();

            Debug.Log($"[ExchangeManager] 교환 UI 활성화 및 {recipes.Length}개의 레시피 로드.");
        }
    }

    /// <summary>
    /// ExchangeUI의 닫기 버튼 또는 내부 로직에 의해 호출됩니다.
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
            // UI 닫을 때 제출 슬롯을 초기화합니다.
            if(submitSlot != null) submitSlot.ClearSlot();

            Debug.Log("[ExchangeManager] 교환 UI 비활성화 및 NPC 종료 알림");
        }
    }
    
    /// <summary>
    /// ExchangeRecipeItem에서 호출됩니다. 선택된 레시피 정보를 제출 슬롯에 설정합니다.
    /// </summary>
    public void SelectRecipeForSubmission(QuestData recipe)
    {
        if (submitSlot == null)
        {
            Debug.LogError("[ExchangeManager] 제출 슬롯(ExchangeSubmitSlot)이 연결되어 있지 않아 레시피를 설정할 수 없습니다. (Start() 함수에서 찾기 실패)");
            return;
        }
        
        if (recipe == null)
        {
            Debug.LogError("[ExchangeManager] 전달된 레시피 데이터가 null입니다. ExchangeRecipeItem.cs를 확인하세요.");
            return;
        }
        
        // ExchangeSubmitSlot의 SetRequiredData를 호출하여 요구 아이템 정보를 전달합니다.
        submitSlot.SetRequiredData(recipe);
        Debug.Log($"[ExchangeManager] 제출 슬롯에 레시피 '{recipe.questName}' 설정 완료.");
    }


    /// <summary>
    /// ExchangeSubmitSlot의 ConfirmSubmission 성공 후 호출됩니다.
    /// ExchangeUI에게 레시피 목록의 가용성을 갱신하도록 알립니다.
    /// </summary>
    public void NotifyTradeSuccess()
    {
        if (exchangeUI != null)
        {
            // 이 함수는 ExchangeUI 내에서 모든 ExchangeRecipeItem의 CheckAvailability를 다시 호출해야 합니다.
            exchangeUI.UpdateAllRecipeItemsAvailability();
        }
    }

    // TryTrade 레거시 함수는 삭제되었습니다. 교환은 이제 SubmitSlot.ConfirmSubmission을 통해서만 이루어집니다.
}