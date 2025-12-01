using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 교환 목록 스크롤 뷰의 개별 아이템 (레시피 한 줄)입니다.
/// 버튼 클릭 시 ExchangeManager를 통해 제출 슬롯에 레시피 정보를 전달합니다.
/// </summary>
public class ExchangeRecipeItem : MonoBehaviour
{
    [Header("Display Elements")]
    public Text requiredText;    // 요구 아이템 이름 및 수량
    public Image requiredIcon;   // 요구 아이템 아이콘
    public Text rewardText;      // 보상 아이템 이름 및 수량
    public Image rewardIcon;     // 보상 아이템 아이콘
    public Button tradeButton;   // 교환 버튼
    public Text tradeButtonText; // 교환 버튼 텍스트 (버튼의 자식 Text 컴포넌트)

    public QuestData currentRecipe;

    /// <summary>
    /// 슬롯을 QuestData를 사용하여 설정합니다. ExchangeUI에서 호출됩니다.
    /// </summary>
    public void Setup(QuestData recipe)
    {
        currentRecipe = recipe;
        
        if (recipe == null) 
        {
            Debug.LogError("[ExchangeRecipeItem] Setup 호출 시 recipe 데이터가 null입니다.");
            return;
        }

        // --- 요구 아이템 설정 (재료) ---
        requiredText.text = $"{recipe.requiredItemName} x{recipe.requiredAmount}";
        requiredIcon.sprite = recipe.requiredItemIcon;
        requiredIcon.enabled = recipe.requiredItemIcon != null;

        // --- 보상 아이템 설정 (결과물) ---
        string rewardItemName = recipe.rewardItem != null ? recipe.rewardItem.itemName : "N/A";
        Sprite rewardItemIcon = recipe.rewardItem != null ? recipe.rewardItem.itemImage : null;
        
        rewardText.text = $"{rewardItemName} x{recipe.rewardCount}";
        rewardIcon.sprite = rewardItemIcon;
        rewardIcon.enabled = rewardItemIcon != null;

        // 버튼 리스너 연결
        tradeButton.onClick.RemoveAllListeners();
        // ⭐ 핵심 수정: 버튼 클릭 시 Submit Slot에 데이터 전달 요청 (직접 교환 로직 제거)
        tradeButton.onClick.AddListener(OnRecipeSelected);
        
        // 초기 가용성 확인
        CheckAvailability();
    }

    /// <summary>
    /// ⭐ 수정된 함수: 교환 버튼 클릭 시 ExchangeManager에게 해당 레시피를
    /// 제출 슬롯에 '선택'하도록 요청합니다.
    /// </summary>
    private void OnRecipeSelected()
    {
        if (ExchangeManager.Instance == null)
        {
            Debug.LogError("[ExchangeRecipeItem] ExchangeManager.Instance가 Null입니다.");
            return;
        }
        
        // 레시피 데이터 유효성 확인
        if (currentRecipe == null)
        {
            Debug.LogError("[ExchangeRecipeItem] currentRecipe가 null이므로 데이터를 전달할 수 없습니다.");
            return;
        }
        
        // ExchangeManager를 통해 ExchangeSubmitSlot에 이 레시피 데이터를 설정합니다.
        // ExchangeManager는 SelectRecipeForSubmission 내부에서 SubmitSlot을 찾아 SetRequiredData를 호출합니다.
        ExchangeManager.Instance.SelectRecipeForSubmission(currentRecipe);
        Debug.Log($"[ExchangeRecipeItem] '{currentRecipe.questName}' 레시피 데이터를 제출 슬롯으로 전달했습니다.");
    }

    /// <summary>
    /// 플레이어의 인벤토리를 기반으로 교환 가능 여부를 확인하고 버튼 UI를 업데이트합니다.
    /// </summary>
    public void CheckAvailability()
    {
        // ⭐ 레시피를 '선택'하는 버튼이므로 항상 상호작용 가능하게 유지합니다.
        tradeButton.interactable = true; 
        
        // 버튼 텍스트 업데이트
        if (tradeButtonText != null)
        {
            // 사용자의 이전 요청을 반영하여, 이 버튼은 '선택' 기능을 담당하도록 텍스트를 변경합니다.
            tradeButtonText.text = "선택"; 
            tradeButtonText.color = Color.white; 
        }
    }
}