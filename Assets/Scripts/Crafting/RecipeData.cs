using UnityEngine;
using System.Collections.Generic;

// 1. 조합 재료 구조체는 그대로 사용
[System.Serializable]
public struct RequiredItem
{
    public Item item;
    public int amount;
}

/// <summary>
/// 조합 시스템에서 사용될 레시피 데이터입니다.
/// </summary>
// 메뉴 이름을 'Crafting System/Recipe Data'로 변경합니다.
[CreateAssetMenu(fileName = "NewRecipeData", menuName = "Crafting System/Recipe Data", order = 1)]
public class RecipeData : ScriptableObject
{
    [Header("1. 조합 레시피 기본 정보")]
    public string recipeName = "새로운 레시피";
    [TextArea]
    public string recipeDescription = "조합에 필요한 재료와 결과 아이템을 정의합니다.";
    
    // 
    
    [Header("2. 요구 사항 (조합 재료 목록)")]
    // 조합에 필요한 여러 재료들
    public List<RequiredItem> requiredMaterials;
    
    [Header("3. 결과 (제작 결과 아이템)")]
    // 제작 결과 아이템 (보상 Item)
    public Item resultItem; 
    // 결과 수량
    public int resultCount = 1;
}