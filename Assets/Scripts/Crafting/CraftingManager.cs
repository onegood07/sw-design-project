using UnityEngine;
using System.Collections; // Coroutine은 제거되지만, 혹시 다른 곳에서 사용될까봐 일단 유지
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

    // ⭐ 코루틴 관련 변수 및 메서드는 제거되었습니다.
    // private Coroutine delayCoroutine; 

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
        // 초기에는 UI를 숨깁니다.
        craftingUI.Hide();
    }

    // ───────────────────────────────
    
    public void OpenCraftingUI(RecipeNPC npc, RecipeData[] recipes) 
    {
        if (craftingUI != null)
        {
            // ⭐ 1. [핵심 수정] GameManager 상태를 즉시 잠급니다. (ExchangeManager와 동일하게)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartInteraction();
                Debug.Log("[CraftingManager] GameManager 상태 즉시 잠금 완료.");
            }
            
            currentInteractingNPC = npc;
            currentRecipes = recipes;

            // 2. 조합 UI를 열고 레시피 데이터를 로드합니다. (여기서 CraftingUI는 InventoryUI도 열어야 함)
            craftingUI.Show(currentRecipes); 
            
            // ⭐ 3. InventoryUI.OpenInventory() 호출 로직 제거됨!
        }
    }

    // ⭐ DelayStartInteraction 코루틴 제거됨!
    /*
    private IEnumerator DelayStartInteraction()
    {
        yield return new WaitForSeconds(0.1f); 
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartInteraction();
            Debug.Log("[CraftingManager] 상호작용 상태가 0.1초 지연 후 잠겼습니다.");
        }
    }
    */

    public void CloseCraftingUI()
    {
        if (craftingUI != null)
        {
            // 1. 조합 UI 닫기 (여기서 CraftingUI는 InventoryUI도 닫아야 함)
            craftingUI.Hide();

            if (currentInteractingNPC != null)
            {
                currentInteractingNPC.OnExchangeEnd(); 
                currentInteractingNPC = null;
            }

            currentRecipes = null;
            
            // ⭐ 2. InventoryUI.CloseInventory() 호출 로직 제거됨!
            
            // 3. GameManager에 상호작용 종료를 알립니다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndInteraction();
            }
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

            // 아이템 이름으로 인벤토리 내 현재 수량을 합산
            // [주의] InventoryItem 클래스가 item, count, itemName 필드를 가지고 있다고 가정
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

        // 재료 소모 및 결과 아이템 추가 후, 인벤토리 빈 칸을 앞으로 당겨 정렬
        if (Inventory.instance != null)
        {
            Inventory.instance.CompactItems();
        }

        return true;
    }
}