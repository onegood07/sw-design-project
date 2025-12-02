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

    public void Show(QuestData[] recipes)
    {
        currentRecipes = recipes;
        if (visualRootPanel != null)
            visualRootPanel.SetActive(true);

        GenerateItems(recipes);

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
    }
}
