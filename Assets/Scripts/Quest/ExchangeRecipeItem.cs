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
    public ExchangeSubmitSlot exchangeSlot;

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
        if (exchangeSlot != null)
            exchangeSlot.SetRequiredData(recipe);

        // 버튼 클릭 리스너 연결
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
            submitButton.interactable = true; // 항상 초기 활성화
        }
    }

    public void CheckAvailability()
    {
        if (submitButton != null && exchangeSlot != null && Recipe != null)
        {
            submitButton.interactable = exchangeSlot.submittedCount < exchangeSlot.requiredAmount;
        }
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    public void OnClick()
    {
        onSelected?.Invoke(this);
    }

    private void OnSubmitClicked()
    {
        if (exchangeSlot == null || Recipe == null)
        {
            Debug.LogWarning("[ExchangeRecipeItem] 슬롯 또는 레시피 없음");
            return;
        }

        bool success = exchangeSlot.ConfirmSubmission();

        if (success)
        {
            Debug.Log($"[ExchangeRecipeItem] '{Recipe.questName}' 교환 성공!");

            // 교환 완료 시 버튼 비활성화
            if (exchangeSlot.submittedCount >= exchangeSlot.requiredAmount && submitButton != null)
            {
                submitButton.interactable = false;
            }
        }
    }
}
