using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }
    // 전체 인벤토리 
    private Dictionary<string, int> ownedItems = new Dictionary<string, int>();
    public ItemData[] quickSlotItems = new ItemData[6];
    public int[] quickSlotCounts = new int[6];
    public int[] quickSlotInventoryIndices = new int[6];
    // itemUse를 위해 플레이어 정보, viewDirection 을 사용해야 하므로 인벤토리 매니저에서 관리함
    // viewDirection은 최신 값 반영을 위해 다이렉트로 인수로 넣음
    public Transform HeroTransform;
    public HeroMoveControl HeroMoveControl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < quickSlotInventoryIndices.Length; i++)
            quickSlotInventoryIndices[i] = -1;
    }
    private void Start() {
        HeroMoveControl = HeroTransform.GetComponent<HeroMoveControl>();
        if(HeroMoveControl == null)Debug.Log("currentViewDirection 참조 불가");
    }
    public void setQuickSlot(ItemData item, int index, int count, int inventoryIndex)
    {
        if (index < 0 || index >= quickSlotItems.Length)
        {
            Debug.LogWarning($"QuickSlot index {index} out of range.");
            return;
        }
        quickSlotItems[index] = item;
        quickSlotCounts[index] = count;
        quickSlotInventoryIndices[index] = inventoryIndex;
    }
    public void addItem(string itemName, int itemCnt)
    {
        if (ownedItems.ContainsKey(itemName))
        {
            ownedItems[itemName] += itemCnt;
            Debug.Log(itemName);
        }
        else
        {
            ownedItems.Add(itemName, itemCnt);
            Debug.Log($"{itemName} 추가");
        }
    }
    public int getItemQuantity(string itemName)
    {
        if (ownedItems.ContainsKey(itemName))
        {
            return ownedItems[itemName];
        }
        else
        {
            return 0;
        }
    }
    public void useQuickSlotItem(int index)
    {
        index--; // 1~6 으로 인풋이 들어옴 하나 깎고 시작
        if (index < 0 || index >= quickSlotItems.Length)
        {
            Debug.LogWarning($"QuickSlot {index + 1} 는 범위를 벗어났습니다.");
            return;
        }
        ItemData Item = quickSlotItems[index];
        Debug.Log(Item);
        if (HeroTransform == null) Debug.Log("HeroTransform 참조 불가");
        if (Item == null)
        {
            Debug.Log("빈 슬롯");
            return;
        }
        if (quickSlotCounts[index] <= 0)
        {
            Debug.Log("퀵슬롯 아이템이 소진되었습니다.");
            ClearQuickSlotData(index);
            return;
        }
        if (Item is IUsable UsableItem)
        {
            UsableItem.Use(HeroTransform, HeroMoveControl.CurrentViewDirection);
            ConsumeQuickSlotItem(index);
        }
        else Debug.Log("사용할 수 없는 아이템");
    }
    // 슬롯 넘버로 일단은 구현
    public ItemData selectItem(int slotNum)
    {
        if (slotNum < 1 || slotNum > quickSlotItems.Length)
        {
            Debug.LogWarning($"QuickSlot {slotNum} 는 범위를 벗어났습니다.");
            return null;
        }
        ItemData Item = quickSlotItems[slotNum-1];
        if(Item == null)
        {
            Debug.Log("빈 슬롯");
            return null;
        }
        if(Item is IUsable UsableItem) return Item;
        else {
            Debug.Log("사용할 수 없는 아이템");
            return null;
        }
    }

    public int GetQuickSlotCount(int slotNum)
    {
        if (slotNum < 1 || slotNum > quickSlotCounts.Length)
        {
            Debug.LogWarning($"QuickSlot {slotNum} 는 범위를 벗어났습니다.");
            return 0;
        }
        return quickSlotCounts[slotNum - 1];
    }

    private void ConsumeQuickSlotItem(int slotIndex)
    {
        quickSlotCounts[slotIndex] = Mathf.Max(quickSlotCounts[slotIndex] - 1, 0);

        int inventoryIndex = quickSlotInventoryIndices[slotIndex];
        int latestCount = quickSlotCounts[slotIndex];

        if (Inventory.instance != null && inventoryIndex >= 0)
        {
            latestCount = Inventory.instance.ConsumeItemAt(inventoryIndex, 1);
            quickSlotCounts[slotIndex] = latestCount;
        }

        var slot = QuickSlot.GetSlotByIndex(slotIndex);
        if (slot != null)
        {
            slot.UpdateLinkedCount(latestCount);
        }

        if (latestCount <= 0)
        {
            ClearQuickSlotData(slotIndex);
        }
    }

    public void ClearQuickSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotItems.Length) return;
        quickSlotItems[slotIndex] = null;
        quickSlotCounts[slotIndex] = 0;
        quickSlotInventoryIndices[slotIndex] = -1;

        var slot = QuickSlot.GetSlotByIndex(slotIndex);
        if (slot != null)
        {
            slot.ClearSlotVisual();
        }
    }
}