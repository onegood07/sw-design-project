using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour 
{
    [Header("UI Root References")]
    // ⭐ 캔버스 전체를 활성화/비활성화할 최상위 오브젝트 (인스펙터에 CraftingCanvas를 연결)
    public GameObject rootCanvasObject; 
    
    // 이 UI 컴포넌트 내부의 시각적 루트 패널
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
    //     UI Show / Hide (인벤토리 활성화 로직 추가)
    // ===========================
    public void Show(RecipeData[] recipes) 
    {
        // ⭐ 1. 캔버스 루트 오브젝트를 강제로 활성화합니다.
        if (rootCanvasObject != null)
        {
            rootCanvasObject.SetActive(true);
            Debug.Log("[CraftingUI] Root Canvas Object 활성화 완료.");
        }
        
        currentRecipes = recipes;
        if (visualRootPanel != null)
            visualRootPanel.SetActive(true); // 내부 패널 활성화

        GenerateRecipes(recipes);

        if (currentRecipeItems.Count > 0)
        {
            SelectRecipeItem(currentRecipeItems[0]); 
        }
        
        // ⭐ 2. [추가] 인벤토리 UI 활성화 (ExchangeUI와 동일한 방식)
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.OpenInventory();
            Debug.Log("[CraftingUI] 조합 UI가 열리면서 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[CraftingUI] InventoryUI 인스턴스를 찾을 수 없습니다.");
        }
    }

    public void Hide()
    {
        // ⭐ 1. 캔버스 루트 오브젝트를 비활성화합니다.
        if (rootCanvasObject != null)
        {
            rootCanvasObject.SetActive(false);
            Debug.Log("[CraftingUI] Root Canvas Object 비활성화 완료.");
        }
        
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false); // 내부 패널 비활성화

        ClearRecipeItems();
        selectedRecipeItem = null;
        
        // ⭐ 2. [추가] 인벤토리 UI 비활성화
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.CloseInventory();
            Debug.Log("[CraftingUI] 조합 UI가 닫히면서 인벤토리 UI를 닫았습니다.");
        }
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