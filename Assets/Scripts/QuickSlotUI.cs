// using UnityEngine;
// using UnityEngine.UI;

// public class QuickSlotUI: MonoBehaviour
// {
//     private ItemData[] curSlotStatus;
//     public GameObject equipSlot;
//     void Start()
//     {
//         UpdateQuickSlotUI();
//     }
//     private void OnEnable()
//     {
//         InventoryManager.OnQuickSlotChanged += UpdateQuickSlotUI;
//     }
//     private void OnDisable()
//     {
//         InventoryManager.OnQuickSlotChanged -= UpdateQuickSlotUI;
//     }
//     private void UpdateQuickSlotUI()
//     {
//         curSlotStatus = InventoryManager.Instance.getQuickSlot();
//         for(int i = 0; i < 4; i++)
//         {

//             var childTransform = equipSlot.transform.GetChild(i);
//             var childImage = childTransform.GetChild(1);
//             var imageObject = childImage.gameObject;

//             var ImageComponent = imageObject.GetComponent<Image>();
//             Debug.Log(childImage);
//             if(ImageComponent == null)
//             {
//                 ImageComponent.sprite = null;
//                 ImageComponent.enabled = false;
//             }
//             ImageComponent.sprite = curSlotStatus[i].Icon;  
//         }
//     }
// }