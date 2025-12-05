using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ExchangeUI : MonoBehaviour
{
    [Header("Visual Root Panel")]
    public GameObject visualRootPanel;

    [Header("UI References")]
    public GameObject contentParent;
    public ExchangeRecipeItem exchangeRecipePrefab;
    public Button closeButton;
    public Button submitButton;

    private List<ExchangeRecipeItem> currentRecipeItems = new List<ExchangeRecipeItem>();
    private QuestData[] currentRecipes;
    private ExchangeRecipeItem selectedRecipeItem;

    private void Start()
    {
        if (closeButton != null)
        {
            // ExchangeManager.Instance.CloseTradeUI()를 호출하면 ExchangeUI.Hide()가 실행됩니다.
            closeButton.onClick.AddListener(() => ExchangeManager.Instance.CloseTradeUI());
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        Hide();
    }

    // ===========================
    //     UI Show (✅ 인벤토리 열기 추가)
    // ===========================
    public void Show(QuestData[] recipes)
    {
        currentRecipes = recipes;

        if (visualRootPanel != null)
            visualRootPanel.SetActive(true);

        GenerateRecipes(recipes);

        // 🔥 첫 번째 레시피 자동 선택
        if (currentRecipeItems.Count > 0)
        {
            SelectRecipeItem(currentRecipeItems[0]);
        }
        
        // ✅ 인벤토리 UI 활성화
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.OpenInventory();
            Debug.Log("[ExchangeUI] 교환 UI가 열리면서 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[ExchangeUI] InventoryUI 인스턴스를 찾을 수 없습니다. 인벤토리를 열 수 없습니다.");
        }
    }

    // ===========================
    //     UI Hide (✅ 인벤토리 닫기 추가)
    // ===========================
    public void Hide()
    {
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false);

        ClearRecipeItems();
        selectedRecipeItem = null;

        // ✅ 인벤토리 UI 비활성화
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.CloseInventory();
            Debug.Log("[ExchangeUI] 교환 UI가 닫히면서 인벤토리 UI를 닫았습니다.");
        }
    }

    // ===========================
    //     레시피 목록 생성
    // ===========================
    private void GenerateRecipes(QuestData[] recipes)
    {
        ClearRecipeItems();

        if (recipes == null || exchangeRecipePrefab == null || contentParent == null)
        {
            Debug.LogError("[ExchangeUI] 레시피 / 프리팹 / Content 누락");
            return;
        }

        foreach (QuestData recipe in recipes)
        {
            ExchangeRecipeItem newItem = Instantiate(exchangeRecipePrefab, contentParent.transform);
            newItem.Setup(recipe);
            newItem.onSelected += SelectRecipeItem;
            currentRecipeItems.Add(newItem);
        }

        UpdateAllRecipeItemsAvailability();
        RefreshLayout();
    }

    private void ClearRecipeItems()
    {
        foreach (ExchangeRecipeItem item in currentRecipeItems)
        {
            item.onSelected -= SelectRecipeItem;
            Destroy(item.gameObject);
        }

        currentRecipeItems.Clear();
    }

    public void UpdateAllRecipeItemsAvailability()
    {
        foreach (ExchangeRecipeItem item in currentRecipeItems)
            item.CheckAvailability();
    }

    // ===========================
    //     레시피 선택
    // ===========================
    private void SelectRecipeItem(ExchangeRecipeItem recipeItem)
    {
        if (selectedRecipeItem != null)
            selectedRecipeItem.SetSelected(false);

        selectedRecipeItem = recipeItem;
        selectedRecipeItem.SetSelected(true);

        Debug.Log($"[ExchangeUI] 선택된 레시피: {selectedRecipeItem.Recipe.questName}");
    }

    // ===========================
    //     교환 버튼 클릭
    // ===========================
    private void OnSubmitButtonClicked()
    {
        if (selectedRecipeItem == null)
        {
            Debug.LogWarning("[ExchangeUI] 제출할 레시피가 선택되지 않음");
            return;
        }

        bool success = selectedRecipeItem.exchangeSlot.ConfirmSubmission();

        if (success)
        {
            Debug.Log($"[ExchangeUI] '{selectedRecipeItem.Recipe.questName}' 교환 성공!");
            // 교환 성공 후 ExchangeManager를 통해 레시피 가용성 갱신을 요청할 수 있습니다.
            ExchangeManager.Instance.NotifyTradeSuccess(); 
        }
    }

    // ===========================
    //     Layout Fix
    // ===========================
    private void RefreshLayout()
    {
        if (contentParent == null) return;

        RectTransform rect = contentParent.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        Transform viewport = rect.parent;
        if (viewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport.GetComponent<RectTransform>());
        }
    }
}