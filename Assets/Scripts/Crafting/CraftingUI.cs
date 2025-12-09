using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour 
{
    [Header("Visual Root Panel")]
    public GameObject visualRootPanel;

    [Header("UI References")]
    // 스크롤 뷰의 Content 트랜스폼
    public GameObject contentParent;
    // 목록에 생성될 프리팹
    public CraftingRecipeItem craftingRecipePrefab; 
    public Button closeButton;
    
    private List<CraftingRecipeItem> currentRecipeItems = new List<CraftingRecipeItem>();
    private RecipeData[] currentRecipes; 
    private CraftingRecipeItem selectedRecipeItem; // 선택 하이라이트 처리를 위해 유지

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => CraftingManager.Instance.CloseCraftingUI());
        }
        Hide();
    }

    // ===========================
    //     UI Show / Hide
    // ===========================
    public void Show(RecipeData[] recipes) 
    {
        currentRecipes = recipes;
        if (visualRootPanel != null)
            visualRootPanel.SetActive(true);

        GenerateRecipes(recipes);

        if (currentRecipeItems.Count > 0)
        {
            // 첫 번째 항목 선택 (하이라이트용)
            SelectRecipeItem(currentRecipeItems[0]); 
        }
    }

    public void Hide()
    {
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false);

        ClearRecipeItems();
        selectedRecipeItem = null;
    }

    // ===========================
    //     레시피 목록 생성 및 관리
    // ===========================
    private void GenerateRecipes(RecipeData[] recipes) 
    {
        ClearRecipeItems();

        if (recipes == null || craftingRecipePrefab == null || contentParent == null) return;

        foreach (RecipeData recipe in recipes) 
        {
            CraftingRecipeItem newItem = Instantiate(craftingRecipePrefab, contentParent.transform);
            newItem.Setup(recipe);
            // 항목 클릭 시 선택 상태만 변경하도록 리스너 연결
            newItem.onSelected += SelectRecipeItem; 
            currentRecipeItems.Add(newItem);
        }

        UpdateAllRecipeItemsAvailability();
    }

    private void ClearRecipeItems()
    {
        foreach (CraftingRecipeItem item in currentRecipeItems)
        {
            if (item != null)
            {
                item.onSelected -= SelectRecipeItem;
                Destroy(item.gameObject);
            }
        }
        currentRecipeItems.Clear();
    }

    /// <summary>
    /// 조합 성공 후, 목록의 모든 항목의 UI를 갱신합니다.
    /// </summary>
    public void UpdateAllRecipeItemsAvailability()
    {
        foreach (CraftingRecipeItem item in currentRecipeItems)
            item.CheckAvailability();
    }

    /// <summary>
    /// 레시피 항목 선택 (시각적 하이라이트용)
    /// </summary>
    private void SelectRecipeItem(CraftingRecipeItem recipeItem)
    {
        if (selectedRecipeItem != null)
            selectedRecipeItem.SetSelected(false);

        selectedRecipeItem = recipeItem;
        selectedRecipeItem.SetSelected(true);
    }
}