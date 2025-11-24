using UnityEngine;

public class LeaderNPC : MonoBehaviour, IInteractable
{
   public void OnInteract()
    {
        // ItemSubmitManager의 싱글톤 인스턴스가 존재하는지 확인
        if (ItemSubmitManager.Instance != null)
        {
            // 납입품 UI 창 열기 함수 호출
            ItemSubmitManager.Instance.OpenExchangeUI();
            
            Debug.Log($"{gameObject.name}: 납입품 창이 열렸습니다.");
        }
        else
        {
            Debug.LogError("ItemSubmitManager 인스턴스를 찾을 수 없습니다.");
        }
    }
}
