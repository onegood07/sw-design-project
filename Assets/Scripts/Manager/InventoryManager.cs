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

    // QuestManager가 호출하는 규격에 맞춘 ItemData 기반 AddItem 함수 (대문자 A)
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
        
        // ItemData Asset 이름을 키로 사용하여 딕셔너리에 추가합니다.
        addItem(itemData.name, count); 
        
        Debug.Log($"[InventoryManager] ItemData를 통해 '{itemData.name}' {count}개를 추가했습니다. (addItem 호출 완료)");
        
        // (UI 갱신 관련 주석은 유지하거나 삭제하세요)
        // if (InventoryUI.instance != null) { InventoryUI.instance.RefreshInventoryDisplay(); }
    }

    // 소유 아이템 딕셔너리에 수량을 누적합니다.
    public void addItem(string itemName, int itemCnt)
    {
        // 디버그 강화: 실제 딕셔너리 키와 수량 확인
        if (ownedItems.ContainsKey(itemName))
        {
            ownedItems[itemName] += itemCnt;
            Debug.Log($"[InventoryManager] 딕셔너리 업데이트: 키='{itemName}' -> 현재 수량: {ownedItems[itemName]}");
        }
        else
        {
            ownedItems.Add(itemName, itemCnt);
            Debug.Log($"[InventoryManager] 딕셔너리 신규 추가: 키='{itemName}' -> 수량: {ownedItems[itemName]}");
        }
    }

    // 소지품에서 해당 이름의 개수를 조회합니다.
    public int getItemQuantity(string itemName)
    {
        if (ownedItems.ContainsKey(itemName))
        {
            // 디버그 강화: 조회 시도하는 이름과 수량 확인
            Debug.Log($"[InventoryManager Debug] '{itemName}' 조회 성공. 수량: {ownedItems[itemName]}");
            return ownedItems[itemName];
        }
        else
        {
            Debug.Log($"[InventoryManager Debug] '{itemName}' (으)로 조회 실패. 딕셔너리에 없음.");
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
            // 쿨타임 체크
            if (!(CoolTimeManager.Instance.GetCurrentCooltime(Item.getItemName) > 0))
            {
                // 임시로 그냥 ConsumeQuickSlotItem 호출
                if (Item.getItemName / 100 != 1)ConsumeQuickSlotItem(index);

                UsableItem.Use(heroT, useVec);
                CoolTimeManager.Instance.AddCooltimeQueue(Item.getItemName,Item.getCoolTime);
            }
            else
            {
                Debug.Log("쿨타임 로딩중");
            }
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

    /// <summary>
    /// 인벤토리 쪽에서 특정 슬롯이 완전히 비워졌을 때 호출해 주면,
    /// 해당 슬롯을 참조하던 퀵슬롯들을 함께 정리합니다.
    /// (예: 인벤토리에서 아이템을 버렸을 때, 연동된 퀵슬롯도 같이 비우기)
    /// </summary>
    public void OnInventorySlotCleared(int inventoryIndex)
    {
        if (inventoryIndex < 0)
            return;

        for (int i = 0; i < quickSlotInventoryIndices.Length; i++)
        {
            if (quickSlotInventoryIndices[i] == inventoryIndex)
            {
                ClearQuickSlotData(i);
            }
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

        // Inventory 클래스는 제공되지 않았지만, 인스턴스가 있다고 가정하고 로직 유지
        if (Inventory.instance != null && inventoryIndex >= 0)
        {
            // Inventory.instance.ConsumeItemAt에서 아이템을 실제로 감소시키고 최신 수량을 반환한다고 가정
            latestCount = Inventory.instance.ConsumeItemAt(inventoryIndex, 1);
            quickSlotCounts[slotIndex] = latestCount; // 인벤토리의 실제 수량을 퀵슬롯에 반영
        }

        // QuickSlot 클래스는 제공되지 않았지만, 정적 메서드가 있다고 가정하고 로직 유지
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

        // QuickSlot 클래스는 제공되지 않았지만, 정적 메서드가 있다고 가정하고 로직 유지
        var slot = QuickSlot.GetSlotByIndex(slotIndex);
        if (slot != null)
        {
            slot.ClearSlotVisual();
        }
    }

    /// <summary>
    /// 인벤토리 내부에서 아이템 순서를 앞으로 당길 때(압축할 때),
    /// 특정 인덱스에 있던 아이템이 새 인덱스로 이동했음을 퀵슬롯에 알려줍니다.
    /// </summary>
    public void OnInventorySlotMoved(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0)
            return;

        for (int i = 0; i < quickSlotInventoryIndices.Length; i++)
        {
            if (quickSlotInventoryIndices[i] == oldIndex)
            {
                quickSlotInventoryIndices[i] = newIndex;
            }
        }
    }
}