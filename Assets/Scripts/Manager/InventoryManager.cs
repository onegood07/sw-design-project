using UnityEngine;
using System.Collections.Generic;

// =========================================================================
// 사용자님 프로젝트의 Item 클래스 (ItemData가 아닌 Item에 정의되어 있어 참조 불가)
// public class Item : MonoBehaviour
// {
//     public string itemName;
//     // ...
// }
// =========================================================================

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

        DontDestroyOnLoad(this.gameObject);

        for (int i = 0; i < quickSlotInventoryIndices.Length; i++)
            quickSlotInventoryIndices[i] = -1;
    }
    private void Start() {
        HeroMoveControl = HeroTransform.GetComponent<HeroMoveControl>();
        if(HeroMoveControl == null)Debug.Log("currentViewDirection 참조 불가");
    }

    // 드래그된 아이템 데이터를 지정한 퀵슬롯 인덱스에 등록합니다.
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

    // ⭐ [수정된 함수]: QuestManager가 호출하는 규격에 맞춘 ItemData 기반 AddItem 함수 (대문자 A)
    /// <summary>
    /// ItemData 객체를 받아 소유 아이템 딕셔너리에 수량을 누적합니다. (QuestManager에서 호출)
    /// </summary>
    /// <param name="itemData">추가할 아이템 데이터</param>
    /// <param name="count">추가할 수량</param>
    public void AddItem(ItemData itemData, int count)
    {
        if (itemData == null)
        {
            Debug.LogError("[InventoryManager] 추가하려는 ItemData가 null입니다.");
            return;
        }
        // [수정] ItemData에 'itemName'이 없다는 오류(CS1061)를 해결하기 위해 
        // Unity Object의 기본 'name' 속성을 사용하도록 변경했습니다.
        addItem(itemData.name, count); 
        
        Debug.Log($"[InventoryManager] ItemData를 통해 '{itemData.name}' {count}개를 추가했습니다.");
        // 여기에 UI 업데이트 로직을 추가할 수 있습니다.
    }

    // 소유 아이템 딕셔너리에 수량을 누적합니다.
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

    // 소지품에서 해당 이름의 개수를 조회합니다.
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

    // 단축키 입력으로 호출되어 퀵슬롯 아이템을 사용합니다.
    public void useQuickSlotItem(int index,Transform heroT,Vector2 useVec)
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
            UsableItem.Use(heroT, useVec);
            if(Item.getItemName / 100 != 1)ConsumeQuickSlotItem(index);
        }
        else Debug.Log("사용할 수 없는 아이템");
    }
    // 슬롯 넘버로 일단은 구현
    // UI에서 선택한 퀵슬롯 데이터를 반환하며 사용 가능 여부를 확인합니다.
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

    // 퀵슬롯에 남아 있는 개수를 반환합니다.
    public int GetQuickSlotCount(int slotNum)
    {
        if (slotNum < 1 || slotNum > quickSlotCounts.Length)
        {
            Debug.LogWarning($"QuickSlot {slotNum} 는 범위를 벗어났습니다.");
            return 0;
        }
        return quickSlotCounts[slotNum - 1];
    }

    // 사용 후 퀵슬롯 수량을 줄이고 인벤토리와 UI를 동기화합니다.
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

    // 퀵슬롯 정보를 비우고 UI 연동을 초기화합니다.
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