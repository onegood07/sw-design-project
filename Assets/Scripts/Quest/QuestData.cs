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
    
    [Header("3. 보상 설정")]
    [Tooltip("이 퀘스트를 완료했을 때 지급되는 모든 보상을 설정합니다.")]

    // --- 3-A. 아이템 보상 설정 ---
    [Header("3-A. 아이템 보상")]
    public bool giveItemReward = false; // 아이템 보상 지급 여부
    [Tooltip("giveItemReward가 true일 때 사용됩니다.")]
    public Item rewardItem; 
    public int rewardCount = 1;
    
    // --- 3-B. 생존자 보상 설정 ---
    [Header("3-B. 생존자 보상")]
    public bool increaseSurvivors = false; // 생존자 증가 보상 지급 여부
    [Tooltip("increaseSurvivors가 true일 때 사용됩니다.")]
    public int survivorIncreaseAmount = 1; // 증가시킬 생존자 수
    
    // 이 필드는 이전 코드에서 사용되었으므로 구조 유지를 위해 남겨둡니다.
    // 실제 사용 시에는 위에 새로 추가된 필드를 사용해야 합니다.
    // public Item rewardItem; 
    // public int rewardCount = 1;
}