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
    public Button submitButton;

    private List<ShelterSubmitItem> currentItems = new List<ShelterSubmitItem>();
    private ShelterSubmitItem selectedItem;
    private QuestData[] currentRecipes;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => ShelterSubmitManager.Instance.CloseSubmitUI());

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        Hide();
    }

    // ===========================
    //     UI Show (✅ 인벤토리 열기 추가)
    // ===========================
    public void Show(QuestData[] recipes)
    {
        currentRecipes = recipes;
        if (visualRootPanel != null)
            visualRootPanel.SetActive(true);

        GenerateItems(recipes);

        if (currentItems.Count > 0)
            SelectItem(currentItems[0]);

        // ✅ 인벤토리 UI 활성화
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.OpenInventory();
            Debug.Log("[ShelterSubmitUI] 납입 UI가 열리면서 인벤토리 UI를 열었습니다.");
        }
        else
        {
            Debug.LogWarning("[ShelterSubmitUI] InventoryUI 인스턴스를 찾을 수 없습니다. 인벤토리를 열 수 없습니다.");
        }
    }

    // ===========================
    //     UI Hide (✅ 인벤토리 닫기 추가)
    // ===========================
    public void Hide()
    {
        if (visualRootPanel != null)
            visualRootPanel.SetActive(false);

        ClearItems();
        selectedItem = null;
        
        // ✅ 인벤토리 UI 비활성화
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.CloseInventory();
            Debug.Log("[ShelterSubmitUI] 납입 UI가 닫히면서 인벤토리 UI를 닫았습니다.");
        }
    }

    private void GenerateItems(QuestData[] recipes)
    {
        ClearItems();

        if (recipes == null || itemPrefab == null || contentParent == null)
            return;

        foreach (QuestData recipe in recipes)
        {
            ShelterSubmitItem newItem = Instantiate(itemPrefab, contentParent.transform);
            newItem.Setup(recipe);
            newItem.onSelected += SelectItem;
            currentItems.Add(newItem);
        }

        UpdateAllItemsAvailability();
    }

    private void ClearItems()
    {
        foreach (var item in currentItems)
        {
            item.onSelected -= SelectItem;
            Destroy(item.gameObject);
        }
        currentItems.Clear();
    }

    public void UpdateAllItemsAvailability()
    {
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

    private void OnSubmitButtonClicked()
    {
        if (selectedItem == null)
            return;

        bool success = selectedItem.submitSlot.ConfirmSubmission();

        if (success)
            Debug.Log($"[ShelterSubmitUI] '{selectedItem.Recipe.questName}' 납입 성공!");
            
            // 납입 성공 후 가용성 갱신 (선택적)
            UpdateAllItemsAvailability();
    }
}