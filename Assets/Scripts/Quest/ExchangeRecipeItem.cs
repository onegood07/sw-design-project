using UnityEngine;
using UnityEngine.UI;
using System;

public class ExchangeRecipeItem : MonoBehaviour
{
    [Header("Display Elements")]
    public Text requiredText;
    public Image requiredIcon;
    public Text rewardText;
    public Image rewardIcon;

    [Header("Selection UI")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Slot Reference")]
    public ExchangeSubmitSlot ExchangeSlot;

    [Header("Submit Button")]
    public Button submitButton;

    public QuestData Recipe { get; private set; }

    public Action<ExchangeRecipeItem> onSelected;

    public void Setup(QuestData recipe)
    {
        Recipe = recipe;

        if (recipe == null)
        {
            Debug.LogError("[ExchangeRecipeItem] Setup()에 null recipe 전달됨");
            return;
        }

        // UI 세팅
        requiredText.text = $"{recipe.requiredItemName} x{recipe.requiredAmount}";
        requiredIcon.sprite = recipe.requiredItemIcon;
        requiredIcon.enabled = recipe.requiredItemIcon != null;

        string rewardItemName = recipe.rewardItem != null ? recipe.rewardItem.itemName : "N/A";
        Sprite rewardItemIcon = recipe.rewardItem != null ? recipe.rewardItem.itemImage : null;

        rewardText.text = $"{rewardItemName} x{recipe.rewardCount}";
        rewardIcon.sprite = rewardItemIcon;
        rewardIcon.enabled = rewardItemIcon != null;

        // 슬롯에도 레시피 정보 전달
        if (ExchangeSlot != null)
            ExchangeSlot.SetRequiredData(recipe);

        // 버튼 클릭 리스너 연결
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
    }

    public void CheckAvailability()
    {
        // 필요 시 교환 가능 여부 업데이트
        // 예: 인벤토리에 충분한 아이템이 있으면 버튼 활성화
        if (submitButton != null && ExchangeSlot != null && Recipe != null)
        {
            submitButton.interactable = true; // 간단히 항상 활성화
        }
    }

    // ===========================
    // 선택/해제
    // ===========================
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    // ===========================
    // 클릭 처리
    // ===========================
    public void OnClick()
    {
        onSelected?.Invoke(this);
    }

    // ===========================
    // 제출 버튼 클릭 처리
    // ===========================
    private void OnSubmitClicked()
    {
        if (ExchangeSlot == null)
        {
            Debug.LogWarning("[ExchangeRecipeItem] ExchangeSlot이 할당되지 않음");
            return;
        }

        bool success = ExchangeSlot.ConfirmSubmission();
        if (success)
        {
            Debug.Log($"[ExchangeRecipeItem] '{Recipe.questName}' 교환 성공!");
            // 성공 시 UI 갱신
            ExchangeManager.Instance.NotifyTradeSuccess();
        }
        else
        {
            Debug.LogWarning($"[ExchangeRecipeItem] '{Recipe.questName}' 교환 실패. 레시피 또는 아이템 부족?");
        }
    }
}
