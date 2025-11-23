using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    Inventory inven;
    public GameObject inventoryPanel;
    bool activeInventory = false;

    public Slot[] slots;
    public Transform slotHolder;

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

        // 처음엔 인벤토리 비활성
        inventoryPanel.SetActive(activeInventory);
    }

    // 슬롯 개수와 관계없이 항상 전부 활성화
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
            activeInventory = !activeInventory;
            inventoryPanel.SetActive(activeInventory);
        }
    }

    public void AddSlot()
    {
        // 슬롯 제한 안 쓸 거면 사실상 의미 없지만, 일단 맞춰줌
        inven.slotCnt++;
        SlotChange(slots.Length);
    }

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
