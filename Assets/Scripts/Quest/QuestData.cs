using UnityEngine;

/// <summary>
/// 퀘스트의 요구 사항 및 보상 정보를 담는 ScriptableObject입니다.
/// 이 클래스를 통해 Unity Inspector에서 다양한 퀘스트 데이터를 미리 생성하고 편집할 수 있습니다.
/// </summary>
// Unity 에디터 메뉴에 'Quest System/Quest Data' 항목을 추가하여 파일을 쉽게 생성할 수 있게 합니다.
[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quest System/Quest Data", order = 1)]
public class QuestData : ScriptableObject
{
    [Header("1. 퀘스트 기본 정보")]
    public string questName = "새로운 미션";
    [TextArea]
    public string questDescription = "요구 아이템과 보상 아이템이 설정됩니다.";

    [Header("2. 요구 사항 (제출/납입 품목)")]
    // 퀘스트 슬롯이 처리할 요구 아이템의 이름 (Item.itemName 필드와 일치해야 함)
    public string requiredItemName; 
     [Tooltip("퀘스트 UI의 납입 슬롯에 표시될 요구 아이템의 아이콘 (Sprite)을 연결하세요.")]
    public Sprite requiredItemIcon; 
    
    // 요구 수량
    public int requiredAmount;      
    
    [Header("3. 보상 (돌아오는 품목)")]
    // 보상 아이템 (Item ScriptableObject를 여기에 연결). 
    // 물물교환, 퀘스트 보상 등에 사용됩니다.
    public Item rewardItem; 
    // 보상 수량
    public int rewardCount = 1;
    

}