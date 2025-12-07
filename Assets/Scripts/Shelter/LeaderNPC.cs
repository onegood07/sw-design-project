using UnityEngine;

public class LeaderNPC : MonoBehaviour, IInteractable
{
    // [Header("납입 레시피")]
    // public QuestData[] submitRecipes; // ❌ 이제 사용되지 않으므로 제거하거나 주석 처리합니다.

    public void OnInteract()
    {
          Debug.Log($"[LeaderNPC] 상호작용 시도됨.");

        if (ShelterSubmitManager.Instance != null)
        {
            // ✅ 인자를 제거하고, OpenSubmitUI()를 호출합니다.
            Debug.Log($"[LeaderNPC] 상호작용 시도됨!!!!!!!!!!!!1");
            ShelterSubmitManager.Instance.OpenSubmitUI(); 
            
            Debug.Log($"{gameObject.name}: 납입품 UI가 열렸습니다.");
        }
        else
        {
            Debug.LogError("ShelterSubmitManager 인스턴스를 찾을 수 없습니다.");
        }
    }
}