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
    //     UI Show
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
    }

    // ===========================
    //     UI Hide
    // ===========================
    public void Hide()
    {
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false);

        ClearRecipeItems();
        selectedRecipeItem = null;
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

        bool success = selectedRecipeItem.ExchangeSlot.ConfirmSubmission();

        if (success)
        {
            Debug.Log($"[ExchangeUI] '{selectedRecipeItem.Recipe.questName}' 교환 성공!");
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
