using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }
    // 전체 인벤토리 
    private Dictionary<string, int> Inventory = new Dictionary<string, int>();
    [SerializeField]
    private ItemData[] QuickSlot = new ItemData[4];
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
    }
    private void Start() {
        HeroMoveControl = HeroTransform.GetComponent<HeroMoveControl>();
        if(HeroMoveControl == null)Debug.Log("currentViewDirection 참조 불가");
    }
    public void setQuickSlot(ItemData item, int index)
    {
        QuickSlot[index] = item;
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
}