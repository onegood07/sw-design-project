using UnityEngine;
using UnityEngine.UI;
using System;

public class ShelterSubmitItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Text requiredText;
    public Image requiredIcon;

    [Header("Selection")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Slot")]
    public ShelterSubmitSlot submitSlot;

    [Header("Submit Button")]
    public Button submitButton;

    public QuestData Recipe { get; private set; }
    public Action<ShelterSubmitItem> onSelected;

    public void Setup(QuestData recipe)
    {
        Recipe = recipe;
        if (recipe == null)
            return;

        requiredText.text = $"{recipe.requiredItemName} x{recipe.requiredAmount}";
        requiredIcon.sprite = recipe.requiredItemIcon;
        requiredIcon.enabled = recipe.requiredItemIcon != null;

        if (submitSlot != null)
            submitSlot.SetRequiredData(recipe);

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
            submitButton.interactable = true;
        }
    }

    public void CheckAvailability()
    {
        if (submitButton != null && submitSlot != null)
            submitButton.interactable = submitSlot.submittedCount < submitSlot.requiredAmount;
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    private void OnSubmitClicked()
    {
        if (submitSlot == null || Recipe == null)
            return;

        bool success = submitSlot.ConfirmSubmission();
        if (success)
        {
            if (submitSlot.submittedCount >= submitSlot.requiredAmount)
                submitButton.interactable = false;

            ShelterSubmitManager.Instance.NotifySubmitSuccess();
        }
    }

    public void OnClick()
    {
        onSelected?.Invoke(this);
    }
}
