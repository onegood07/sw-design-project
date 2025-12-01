using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems; // Layout Rebuilder 사용을 위해 필요

/// <summary>
/// 아이템 교환 UI를 관리하고 ExchangeRecipeItem 프리팹을 동적으로 생성하며,
/// 고정된 ExchangeSubmitSlot을 참조합니다.
/// </summary>
public class ExchangeUI : MonoBehaviour
{
    // ★★★ 가장 중요한 변수: 실제 UI 화면 전체를 담는 시각적 패널을 연결합니다. ★★★
    [Header("Visual Root Panel (시각적 UI의 최상위 패널)")]
    public GameObject visualRootPanel; 

    [Header("UI References")]
    public GameObject contentParent; // Scroll View의 Content GameObject
    // ⭐ ExchangeSlot 대신 ExchangeRecipeItem 스크립트로 변경합니다.
    public ExchangeRecipeItem exchangeRecipePrefab; // 교환 목록의 한 줄 프리팹
    public Button closeButton; // UI 닫기 버튼
    
    // ⭐ 새로 추가: 아이템 드래그/드롭을 위한 제출 슬롯과 버튼
    [Header("Submission Slot References")]
    public ExchangeSubmitSlot submitSlot; // 단 하나만 존재할 제출 전용 슬롯
    public Button submitButton;           // 제출 슬롯의 아이템을 교환하는 버튼

    [Header("Runtime Data")]
    // ExchangeSlot 대신 ExchangeRecipeItem 목록을 저장합니다.
    private List<ExchangeRecipeItem> currentRecipeItems = new List<ExchangeRecipeItem>();
    private QuestData[] currentRecipes;
    
    // ⭐ 현재 선택된 레시피를 저장합니다.
    private QuestData selectedRecipe;

    private void Start()
    {
        // 닫기 버튼 리스너 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => ExchangeManager.Instance.CloseTradeUI());
        }
        
        // ⭐ 제출 버튼 리스너 연결
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners(); // 기존 리스너 제거 (안전)
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        // 초기에는 숨깁니다.
        Hide();
        
        // 제출 슬롯이 항상 초기화된 상태로 있도록 보장
        if (submitSlot != null)
        {
            submitSlot.ClearSlot();
        }
    }

    /// <summary>
    /// UI를 표시하고 레시피 목록에 따라 슬롯을 생성합니다.
    /// </summary>
    public void Show(QuestData[] recipes)
    {
        this.currentRecipes = recipes;
        
        if (visualRootPanel != null)
        {
            visualRootPanel.SetActive(true);
        }
        
        // UI가 열릴 때 제출 슬롯과 선택 레시피를 초기화합니다.
        selectedRecipe = null; 
        if (submitSlot != null) submitSlot.ClearSlot();

        GenerateRecipes(recipes); // 슬롯 대신 레시피 아이템 생성
    }

    /// <summary>
    /// UI를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        if (visualRootPanel != null)
        {
            visualRootPanel.SetActive(false);
        }
        // UI 닫힐 때 레시피 목록도 정리
        ClearRecipeItems();
        selectedRecipe = null;
        if (submitSlot != null) submitSlot.ClearSlot();
    }

    /// <summary>
    /// 기존 레시피 아이템을 제거하고 새 레시피 목록으로 아이템을 생성합니다.
    /// </summary>
    private void GenerateRecipes(QuestData[] recipes)
    {
        // 1. 기존 아이템 모두 제거
        ClearRecipeItems();

        if (recipes == null || exchangeRecipePrefab == null || contentParent == null)
        {
            Debug.LogError("[ExchangeUI] 레시피, 프리팹, 또는 Content가 설정되지 않았습니다.");
            return;
        }

        // 2. 새 레시피 아이템 생성
        foreach (QuestData recipe in recipes)
        {
            // 프리팹을 Content 아래에 인스턴스화
            ExchangeRecipeItem newItem = Instantiate(exchangeRecipePrefab, contentParent.transform);
            
            // Setup 함수 호출
            newItem.Setup(recipe);
            
            // ⭐ 핵심: 레시피 목록의 버튼 클릭 시, 해당 레시피를 선택 상태로 만듭니다.
            newItem.tradeButton.onClick.RemoveAllListeners();
            // 람다를 사용하여 현재 레시피 데이터를 전달합니다.
            newItem.tradeButton.onClick.AddListener(() => OnRecipeSelected(recipe));

            currentRecipeItems.Add(newItem);
        }
        
        // 생성 후 전체 레시피 아이템 가용성 업데이트 (Inventory 확인)
        UpdateAllRecipeItemsAvailability();
        RefreshLayout();
    }
    
    private void ClearRecipeItems()
    {
        foreach (ExchangeRecipeItem item in currentRecipeItems)
        {
            Destroy(item.gameObject);
        }
        currentRecipeItems.Clear();
    }

    /// <summary>
    /// 모든 레시피 아이템의 교환 가능 여부(버튼 상태)를 새로고침합니다.
    /// 교환 성공/실패 시 ExchangeManager에서 호출됩니다.
    /// </summary>
    public void UpdateAllRecipeItemsAvailability()
    {
        foreach (ExchangeRecipeItem item in currentRecipeItems)
        {
            item.CheckAvailability();
        }
    }
    
    /// <summary>
    /// 레시피 목록에서 항목을 선택했을 때 호출됩니다.
    /// </summary>
    public void OnRecipeSelected(QuestData recipe)
    {
        selectedRecipe = recipe;
        if (submitSlot != null)
        {
            // 선택된 레시피의 요구사항을 제출 슬롯에 설정하여 드래그를 허용합니다.
            submitSlot.SetRequiredData(recipe);
        }
        Debug.Log($"[ExchangeUI] 레시피 선택됨: {recipe.questName}. 제출 슬롯에 요구사항 설정 완료.");
    }
    
    // ⭐ 제출 버튼 클릭 핸들러 (제출 슬롯에 드래그된 아이템을 확정)
    private void OnSubmitButtonClicked()
    {
        if (selectedRecipe == null)
        {
            Debug.LogWarning("[ExchangeUI] 교환할 레시피가 선택되지 않았습니다.");
            return;
        }
        if (submitSlot == null)
        {
            Debug.LogError("[ExchangeUI] 제출 슬롯이 연결되지 않았습니다.");
            return;
        }
        
        // 제출 슬롯에 있는 아이템을 기반으로 교환 시도
        // ExchangeSubmitSlot의 ConfirmSubmission 함수는 이제 인자를 받지 않도록 수정되었습니다.
        bool tradeSuccess = submitSlot.ConfirmSubmission(); // <-- 인자 제거
        
        if (tradeSuccess)
        {
            // 성공 시 현재 레시피 선택을 해제하고 UI를 초기화합니다.
            selectedRecipe = null;
        }
    }
    
    /// <summary>
    /// 스크롤 뷰의 레이아웃을 강제로 갱신합니다. (스크롤 안 되는 문제 해결)
    /// </summary>
    private void RefreshLayout()
    {
        if (contentParent == null) return;
        
        RectTransform contentRect = contentParent.GetComponent<RectTransform>();

        // LayoutGroup을 가진 Content 자체를 강제 갱신합니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        
        Transform viewportTransform = contentRect.parent;
        if (viewportTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportTransform.GetComponent<RectTransform>());
        }
    }
}