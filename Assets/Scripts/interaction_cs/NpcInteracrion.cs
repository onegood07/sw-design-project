using UnityEngine;

// npc 인터렉션을 위해 임시로 만들어둔 스크립트
// 로그로만 인터렉션을 확인 가능하고 세부 기능은 추가해야 함.
public class NpcInteraction : MonoBehaviour, IInteractable
{
    public void OnInteract()
    {
        if(InventoryManager.Instance != null)
        {
            Debug.Log("npc 상호작용");
        }
    }
}
