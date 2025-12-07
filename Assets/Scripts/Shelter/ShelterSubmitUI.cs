using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShelterSubmitUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject visualRootPanel;
    public GameObject contentParent;
    public ShelterSubmitItem itemPrefab;
    public Button closeButton;

    private List<ShelterSubmitItem> currentItems = new List<ShelterSubmitItem>();
    private ShelterSubmitItem selectedItem;

    private void Start()
    {
        if (closeButton != null && ShelterSubmitManager.Instance != null)
            closeButton.onClick.AddListener(() => ShelterSubmitManager.Instance.CloseSubmitUI());
        
        Hide();
    }

    public void Show() 
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ShelterSubmitUI] GameManager가 초기화되지 않았습니다.");
            return;
        }

        var requiredItemsData = GameManager.Instance.CurrentRequiredItemsData; 

        if (visualRootPanel != null)
            visualRootPanel.SetActive(true);

        GenerateItems(requiredItemsData);

        if (currentItems.Count > 0)
            SelectItem(currentItems[0]);
    }

    public void Hide()
    {
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false);

        ClearItems();
        selectedItem = null;
    }

    // Item Dictionary를 기반으로 아이템 생성 (수정됨)
    private void GenerateItems(Dictionary<Item, int> requiredItemsData)
    {
        ClearItems();

        if (requiredItemsData == null || itemPrefab == null || contentParent == null || GameManager.Instance == null)
            return;

        foreach (var entry in requiredItemsData)
        {
            Item requiredItem = entry.Key;
            int requiredAmount = entry.Value;

            ShelterSubmitItem newItem = Instantiate(itemPrefab, contentParent.transform);
            
            // 1. 기본 요구 데이터 설정
            newItem.Setup(requiredItem, requiredAmount); 
            newItem.onSelected += SelectItem;

            // 2. ✅ GameManager에서 저장된 제출 수량 반영
            int submittedCount = 0;
            if (GameManager.Instance.CurrentSubmittedData.TryGetValue(requiredItem, out submittedCount))
            {
                // ShelterSubmitSlot에 저장된 수량을 반영하도록 요청
                newItem.submitSlot.ReflectSubmittedCount(submittedCount);
            }

            currentItems.Add(newItem);
        }

        UpdateAllItemsAvailability();
    }

    private void ClearItems()
    {
        foreach (var item in currentItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        currentItems.Clear();
    }

    public void UpdateAllItemsAvailability()
    {
        // 모든 납입 항목에 대해 납입 가능 여부(UI)를 갱신
        foreach (var item in currentItems)
            item.CheckAvailability();
    }

    private void SelectItem(ShelterSubmitItem item)
    {
        if (selectedItem != null)
            selectedItem.SetSelected(false);

        selectedItem = item;
        selectedItem.SetSelected(true);
    }

    // ❌ OnSubmitButtonClicked 함수 제거
    // private void OnSubmitButtonClicked()
    // {
    //     // 이 로직은 이제 ShelterSubmitItem.OnClick() 내부에서 처리됩니다.
    // }
}