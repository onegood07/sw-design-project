using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널 열기/닫기와 슬롯 UI 갱신을 담당하는 프리젠테이션 계층입니다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // ✅ 싱글톤 인스턴스
    public static InventoryUI instance;
    
    Inventory inven;
    public GameObject inventoryPanel;
    bool activeInventory = false;

    public Slot[] slots;
    public Transform slotHolder;
    
    private void Awake()
    {
        // ✅ 싱글톤 초기화
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
        slots = slotHolder.GetComponentsInChildren<Slot>();

        // 각 슬롯에 인덱스 부여
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotIndex = i;
        }

        inven.onSlotCountChange += SlotChange;
        inven.onChangeItem += RedrawSlotUI;

        // 시작하자마자 모든 슬롯 활성화
        SlotChange(slots.Length);

         RedrawSlotUI(); 

        // 처음엔 인벤토리 비활성
        inventoryPanel.SetActive(activeInventory);
    }

    // 슬롯 개수와 관계없이 항상 전부 활성화
    /// <summary>
    /// 슬롯 개수 변경 이벤트를 받아 모든 슬롯을 활성화합니다.
    /// </summary>
    private void SlotChange(int val)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].GetComponent<Button>().interactable = true;
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
    /// 인벤토리 활성화 상태를 토글합니다. (탭 키 입력 처리)
    /// </summary>
    public void ToggleInventory()
    {
        activeInventory = !activeInventory;
        inventoryPanel.SetActive(activeInventory);
        
        // 인벤토리가 열릴 때, 슬롯 UI를 갱신합니다. (선택적)
        if(activeInventory) RedrawSlotUI();
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
            RedrawSlotUI();
            Debug.Log("[InventoryUI] 외부 요청으로 인벤토리 열림.");
        }
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
        // 슬롯 제한 안 쓸 거면 사실상 의미 없지만, 일단 맞춰줌
        inven.slotCnt++;
        SlotChange(slots.Length);
    }

    /// <summary>
    /// 인벤토리 데이터와 UI 슬롯 간 내용을 동기화합니다.
    /// </summary>
    void RedrawSlotUI()
    {
        // 모든 슬롯 초기화
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].RemoveSlot();
        }

        // 인벤토리 아이템 다시 채우기
        for (int i = 0; i < inven.items.Count && i < slots.Length; i++)
        {
            slots[i].item = inven.items[i];
            slots[i].UpdateSlotUI();
        }
    }
}