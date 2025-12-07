using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ★★★ HasItem 함수를 위해 LINQ 사용 (필수)

/// <summary>
/// 인벤토리 슬롯 리스트를 유지하며 아이템 추가/소비 이벤트를 브로드캐스트합니다.
/// </summary>
public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    public delegate void OnSlotCountChange(int val);
    public OnSlotCountChange onSlotCountChange;

    public delegate void OnChangeItem();
    public OnChangeItem onChangeItem;

    // ★ InventoryItem List
    public List<InventoryItem> items = new List<InventoryItem>();

    public int slotCnt = 20;

    /// <summary>
    /// 필드 아이템 데이터를 받아 슬롯에 추가하거나 기존 스택을 증가시킵니다. (기존 AddItem 유지)
    /// ExchangeManager에서 보상 지급 시 호출됩니다.
    /// </summary>
    public bool AddItem(Item worldItem, int addCount = 1)
    {
        if (worldItem == null) return false;

        // 이미 존재하는 아이템이면 count 증가 (null 슬롯 방지)
        int idx = items.FindIndex(i => i != null && i.itemName == worldItem.itemName);
        if (idx != -1)
        {
            items[idx].count += addCount;
            onChangeItem?.Invoke();
            return true;
        }

        // 새 슬롯에 추가
        // InventoryItem 클래스와 Item 클래스가 프로젝트에 정의되어 있어야 합니다.
        InventoryItem newItem = new InventoryItem(worldItem, addCount);

        // 먼저 비어있는 슬롯(null)을 재사용
        int emptyIdx = items.FindIndex(i => i == null);
        if (emptyIdx != -1)
        {
            items[emptyIdx] = newItem;
        }
        else
        {
            // 공간이 모자라면 리스트 확장 (slotCnt 제한은 UI에서 처리)
            items.Add(newItem);
        }

        onChangeItem?.Invoke();
        return true;
    }

    public void AddInventoryItemInstance(InventoryItem invItem)
    {
        if (invItem == null)
            return;

        int emptyIdx = items.FindIndex(i => i == null);
        if (emptyIdx != -1)
            items[emptyIdx] = invItem;
        else
            items.Add(invItem);

        onChangeItem?.Invoke();
    }

    /// <summary>
    /// 지정 슬롯에서 개수를 차감하고 0 이하일 경우 슬롯을 비웁니다.
    /// 슬롯이 완전히 비워지면, 해당 인덱스를 참조하고 있던 퀵슬롯도 함께 정리합니다.
    /// </summary>
    public int ConsumeItemAt(int index, int amount = 1)
    {
        if (index < 0 || index >= items.Count) return 0;
        var item = items[index];
        if (item == null) return 0;

        item.count -= amount;
        if (item.count <= 0)
        {
            items[index] = null;
            item.count = 0;

            // 이 인벤토리 슬롯을 참조하던 퀵슬롯이 있으면 함께 비워준다.
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventorySlotCleared(index);
            }
        }

        onChangeItem?.Invoke();
        return item != null ? item.count : 0;
    }
    
    // ==========================================================
    // ★★★★★ 교환 시스템을 위한 필수 함수 추가 ★★★★★
    // ==========================================================
    
    // 1. 특정 아이템을 요구 수량만큼 가지고 있는지 확인 (ExchangeSlot에서 호출)
    /// <summary>
    /// 인벤토리 전체에서 해당 아이템을 요구 수량만큼 가지고 있는지 확인합니다.
    /// </summary>
    public bool HasItem(string itemName, int requiredAmount)
    {
        // 인벤토리 전체에서 해당 이름의 아이템 총 수량을 계산합니다.
        int possessed = items
            .Where(i => i != null && i.itemName == itemName)
            .Sum(i => i.count); 
            
        return possessed >= requiredAmount;
    }

    // 2. 재료 아이템 제거 (ExchangeManager에서 호출)
    /// <summary>
    /// 재료 아이템을 요구 수량만큼 인벤토리에서 제거합니다.
    /// </summary>
    public void RemoveItem(string itemName, int amount)
    {
        int remaining = amount;
        
        // 인벤토리 리스트를 역순으로 순회하며 아이템을 제거합니다.
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = items[i];
            
            if (item != null && item.itemName == itemName)
            {
                int available = item.count;
                int take = Mathf.Min(available, remaining);
                
                item.count -= take;
                remaining -= take;
                
                // 스택이 0이 되면 슬롯을 비웁니다.
                if (item.count <= 0)
                {
                    items[i] = null;
                }
            }
        }
        
        onChangeItem?.Invoke();
        
        if (remaining > 0)
        {
            Debug.LogError($"[Inventory] {itemName} 제거 오류: {remaining}개가 부족합니다. HasItem 검사를 통과했어야 합니다.");
        }
    }

    // 3. 중복되는 AddItem(Item, int) 오버로드는 기존 AddItem이 처리하므로 삭제함.
}