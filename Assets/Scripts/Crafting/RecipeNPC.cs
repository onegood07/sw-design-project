using UnityEngine;


/// 조합(Crafting) 기능을 제공하는 NPC입니다. 
/// </summary>
public class RecipeNPC : MonoBehaviour, IInteractable
{
    [Header("Crafting Recipes")]
    public RecipeData[] availableRecipes; 

    public void OnInteract()
    {
        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OpenCraftingUI(this, availableRecipes);
        }
        else
        {
             Debug.LogError("[RecipeNPC] CraftingManager.Instance를 찾을 수 없습니다.");
        }
    }

    public void OnExchangeEnd() 
    {
        // UI 종료 시 필요한 로직
    }
}