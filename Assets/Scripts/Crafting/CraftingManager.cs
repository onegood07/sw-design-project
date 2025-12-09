using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

/// <summary>
/// 아이템 조합(크래프팅) 로직을 담당하는 싱글톤 관리자입니다.
/// 조합 UI 활성화/비활성화 및 제작 실행을 관리합니다.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("UI References")]
    public CraftingUI craftingUI; 

    public RecipeNPC currentInteractingNPC; 
    public RecipeData[] currentRecipes; 

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
        if (craftingUI == null)
        {
            Debug.LogError("[CraftingManager] CraftingUI 참조가 누락되었습니다. 인스펙터에 연결해주세요!");
            return;
        }
        craftingUI.Hide();
    }

    // ───────────────────────────────
    
    public void OpenCraftingUI(RecipeNPC npc, RecipeData[] recipes) 
    {
        if (craftingUI != null)
        {
            currentInteractingNPC = npc;
            currentRecipes = recipes;

            craftingUI.Show(currentRecipes); 
        }
    }

    public void CloseCraftingUI()
    {
        if (craftingUI != null)
        {
            craftingUI.Hide();

            if (currentInteractingNPC != null)
            {
                currentInteractingNPC.OnExchangeEnd(); 
                currentInteractingNPC = null;
            }

            currentRecipes = null;
        }
    }

    // ───────────────────────────────
    
    /// <summary>
    /// 조합 성공 시 레시피 목록에서 가용성 갱신을 요청합니다.
    /// </summary>
    public void NotifyCraftSuccess()
    {
        if (craftingUI != null)
        {
            // 모든 RecipeItem의 CheckAvailability()를 호출하여 재료 수량 변경을 반영
            craftingUI.UpdateAllRecipeItemsAvailability();
        }
    }

    // ───────────────────────────────
    
    /// <summary>
    /// 인벤토리 전체를 검사하여 조합 가능 여부를 체크합니다.
    /// </summary>
    public bool CanCraft(RecipeData recipe)
    {
        if (Inventory.instance == null || recipe == null) return false;
        Inventory inven = Inventory.instance;

        foreach (var req in recipe.requiredMaterials)
        {
            if (req.item == null) continue;

            // Inventory 클래스가 Items를 List<InventoryItem>으로 가지고 있다고 가정
            // 아이템 이름으로 인벤토리 내 현재 수량을 합산
            int currentCount = inven.items.Where(i => i != null && i.itemName == req.item.itemName)
                                          .Sum(i => i.count);

            if (currentCount < req.amount)
            {
                return false;
            }
        }
        return true;
    }
    
    // ───────────────────────────────
    
    /// <summary>
    /// 선택된 레시피의 재료를 인벤토리에서 소모하고 결과물을 지급합니다. (핵심 트랜잭션)
    /// </summary>
    public bool TryCraft(RecipeData recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning("[CraftingManager] 조합 실패: 재료가 부족합니다.");
            return false;
        }

        // 1. 재료 소모
        Inventory inven = Inventory.instance;
        foreach (var req in recipe.requiredMaterials)
        {
            // Inventory 클래스에 RemoveItem 함수가 정의되어 있어야 함
            inven.RemoveItem(req.item.itemName, req.amount); 
        }

        // 2. 보상 지급
        inven.AddItem(recipe.resultItem, recipe.resultCount);

        Debug.Log($"[CraftingManager] 조합 성공: {recipe.resultItem.itemName} x{recipe.resultCount} 지급");
        
        // UI 갱신 요청
        NotifyCraftSuccess(); 
        
        return true;
    }
}