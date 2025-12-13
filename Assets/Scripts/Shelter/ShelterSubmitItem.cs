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

    // Item 컴포넌트를 저장
    public Item RequiredItem { get; private set; } 
    public int RequiredAmount { get; private set; }
    
    // 선택 이벤트는 이제 선택 표시 용도로만 사용됩니다.
    public Action<ShelterSubmitItem> onSelected;

    /// <summary>
    /// UI 항목 설정 (Item 컴포넌트와 수량을 받음)
    /// </summary>
    public void Setup(Item requiredItemData, int amount)
    {
        RequiredItem = requiredItemData;
        RequiredAmount = amount;
        
        if (requiredItemData == null || amount <= 0)
        {
            return;
        }

        // UI 텍스트 및 아이콘 설정 (Item 컴포넌트의 필드 사용)
        requiredText.text = $"{requiredItemData.itemName} x{RequiredAmount}";
        requiredIcon.sprite = requiredItemData.itemImage; 
        requiredIcon.enabled = requiredItemData.itemImage != null;

        if (submitSlot != null)
            // SetRequiredData에 Item 컴포넌트와 수량 전달
            submitSlot.SetRequiredData(requiredItemData, RequiredAmount); 

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            // OnClick 함수에 즉시 납입 로직이 포함됩니다.
            submitButton.onClick.AddListener(OnClick); 
            submitButton.interactable = true;
        }
        
        CheckAvailability();
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

    /// <summary>
    /// 버튼 클릭 시 즉시 납입 처리 (수정됨)
    /// </summary>
    public void OnClick()
    {
        // 1. 해당 항목 선택 이벤트 호출 (UI 선택 표시 용도)
        onSelected?.Invoke(this); 

        // 2. 납입 슬롯의 ConfirmSubmission 로직을 직접 호출하여 아이템 소모 및 카운트 증가
        if (submitSlot != null)
        {
            bool success = submitSlot.ConfirmSubmission();
            
            if (success)
            {
                // 3. 납입 성공 시 가용성을 즉시 갱신 (버튼 비활성화 또는 텍스트 업데이트)
                CheckAvailability();
                
                // 납입이 완료되었다면 선택 해제 (선택적)
                if (submitSlot.submittedCount >= submitSlot.requiredAmount)
                {
                    SetSelected(false);
                }
            }
        }
    }
}