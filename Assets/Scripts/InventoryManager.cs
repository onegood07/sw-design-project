using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using Unity.VisualScripting;
public class InventoryManager : MonoBehaviour
{
    public static event System.Action OnQuickSlotChanged;
    public static InventoryManager Instance { get; private set; }
    private Dictionary<string, int> Inventory = new Dictionary<string, int>();
    [SerializeField]
    private ItemData[] QuickSlot = new ItemData[4];
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
    }
    private void Start() {
        HeroMoveControl = HeroTransform.GetComponent<HeroMoveControl>();
        if(HeroMoveControl == null)Debug.Log("currentViewDirection 참조 불가");
    }
    public void setQuickSlot(ItemData item, int index)
    {
        QuickSlot[index] = item;
    }
    public ItemData[] getQuickSlot()
    {
        return (ItemData[])QuickSlot.Clone();
    }
    public void addItem(string itemName, int itemCnt)
    {
        if (Inventory.ContainsKey(itemName))
        {
            Inventory[itemName] += itemCnt;
            Debug.Log(itemName);
        }
        else
        {
            Inventory.Add(itemName, itemCnt);
            Debug.Log($"{itemName} 추가");
        }
    }
    public int getItemQuantity(string itemName)
    {
        if (Inventory.ContainsKey(itemName))
        {
            return Inventory[itemName];
        }
        else
        {
            return 0;
        }
    }
    public void useQuickSlotItem(int index)
    {
        index--; // 1,2,3,4로 인풋이 들어옴 하나 깎고 시작
        ItemData Item = QuickSlot[index];
        Debug.Log(Item);
        if (HeroTransform == null) Debug.Log("HeroTransform 참조 불가");
        if (Item == null)
        {
            Debug.Log("빈 슬롯");
            return;
        }
        if (Item is IUsable UsableItem) UsableItem.Use(HeroTransform, HeroMoveControl.CurrentViewDirection);
        else Debug.Log("사용할 수 없는 아이템");

    }
    public void changeQuickSlot(int index, ItemData inputItem)
    {
        QuickSlot[index] = inputItem;
        if(OnQuickSlotChanged != null)
        {
            OnQuickSlotChanged.Invoke();
        }
    }
}