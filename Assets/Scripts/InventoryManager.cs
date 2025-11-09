using UnityEngine;
using System.Collections.Generic;
public class InventoryManager : MonoBehaviour
{
    // 읽기만 가능하고 쓰기는 클래스 내부에서만 가능하도록
    public static InventoryManager Instance { get; private set; }
    private Dictionary<string, int> Inventory = new Dictionary<string, int>();

    private void Awake() {
        if (Instance != null && Instance != this)
        {
            // 이미 존재한다면, 새로 생성된 자신을 파괴하여 중복 방지
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
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
}
