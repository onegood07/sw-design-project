using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic; // List를 사용하기 위해 추가 (과거 버전에서 가져옴)

/// <summary>
/// 인벤토리 패널 열기/닫기와 슬롯 UI 갱신을 담당하는 프리젠테이션 계층입니다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static InventoryUI instance;
    
    Inventory inven;
    public GameObject inventoryPanel;
    bool activeInventory = false;

    public Slot[] slots;
    public Transform slotHolder;
    
    private void Awake()
    {
        // 싱글톤 초기화
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        inven = Inventory.instance;
        // 슬롯 컴포넌트를 모두 가져와 저장
        slots = slotHolder.GetComponentsInChildren<Slot>();

        // 각 슬롯에 인덱스 부여
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotIndex = i;
        }

        // Inventory 데이터 변경 이벤트 구독
        inven.onSlotCountChange += SlotChange;
        inven.onChangeItem += RedrawSlotUI;

        // 시작하자마자 모든 슬롯 활성화
        SlotChange(slots.Length);

        RedrawSlotUI(); 

        // 처음엔 인벤토리 비활성 (과거 버전의 안전성 로직 반영)
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(activeInventory);
        }
        else
        {
            Debug.LogError("[InventoryUI] inventoryPanel 참조 누락!");
        }
    }

    // 슬롯 개수와 관계없이 항상 전부 활성화
    /// <summary>
    /// 슬롯 개수 변경 이벤트를 받아 모든 슬롯을 활성화합니다.
    /// </summary>
    private void SlotChange(int val)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            // 과거 버전에서 사용하던 안전성 로직을 유지
            if (slots[i] != null && slots[i].GetComponent<Button>() != null)
            {
                slots[i].GetComponent<Button>().interactable = true;
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // 인벤토리 활성화 상태를 토글합니다.
            ToggleInventory();
        }
    }
    
    /// <summary>
    /// 외부(ExchangeUI 등)에서 인벤토리 패널을 열 때 호출됩니다.
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryPanel != null && !activeInventory)
        {
            activeInventory = true;
            inventoryPanel.SetActive(true);
            
            // 패널을 열 때 RedrawSlotUI를 호출해 인벤토리 데이터를 강제로 반영한다.
            RedrawSlotUI(); 
            
            Debug.Log("[InventoryUI] 외부 요청으로 인벤토리 열림.");
        }
    }
  
    /// <summary>
    /// 인벤토리 패널의 표시 여부를 토글합니다.
    /// </summary>
    public void ToggleInventory()
    {
        activeInventory = !activeInventory;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(activeInventory);
    
        if (activeInventory) 
            RedrawSlotUI(); 
    }
    
    /// <summary>
    /// 외부(ExchangeUI 등)에서 인벤토리 패널을 닫을 때 호출됩니다.
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryPanel != null && activeInventory)
        {
            activeInventory = false;
            inventoryPanel.SetActive(false);
            Debug.Log("[InventoryUI] 외부 요청으로 인벤토리 닫힘.");
        }
    }

    /// <summary>
    /// 외부 버튼에서 호출돼 슬롯 수를 늘리고 UI를 갱신합니다.
    /// </summary>
    public void AddSlot()
    {
        inven.slotCnt++;
        SlotChange(slots.Length);
    }

    /// <summary>
    /// 인벤토리 데이터와 UI 슬롯 간 내용을 동기화합니다. (과거 버전의 안전성 로직 반영)
    /// </summary>
    public void RedrawSlotUI()
    {
        if (inven == null || slots == null) return;
        
        // 1. 모든 슬롯을 순회하며 초기화 및 갱신
        for (int i = 0; i < slots.Length; i++)
        {
            Slot currentSlot = slots[i];

            // Slot 인스턴스가 파괴되었는지 확인
            if (currentSlot == null)
            {
                Debug.LogWarning($"[InventoryUI] slots[{i}] 인스턴스가 파괴되어 건너뜁니다.");
                continue;
            }

            // 2. 인벤토리 아이템을 채웁니다.
            if (i < inven.items.Count)
            {
                // 인벤토리 데이터를 슬롯에 할당하고 UI 갱신
                currentSlot.item = inven.items[i];
                currentSlot.UpdateSlotUI();
            }
            else
            {
                // 인벤토리 데이터가 없는 슬롯은 내용을 비웁니다.
                currentSlot.RemoveSlot();
            }
        }
    }

    /// <summary>
    /// 버튼 등에서 호출하여 인벤토리 아이템들을 앞쪽으로 압축(빈칸 제거)합니다.
    /// </summary>
    public void CompactInventory()
    {
        if (inven == null) return;

        inven.CompactItems();
    }
}