using UnityEngine;

public class LeaderNPC : MonoBehaviour, IInteractable
{
    [Header("납입 레시피")]
    public QuestData[] submitRecipes;

    public void OnInteract()
    {
        if (ShelterSubmitManager.Instance != null)
        {
            ShelterSubmitManager.Instance.OpenSubmitUI(submitRecipes);
            Debug.Log($"{gameObject.name}: 납입품 UI가 열렸습니다.");
        }
        else
        {
            Debug.LogError("ShelterSubmitManager 인스턴스를 찾을 수 없습니다.");
        }
    }
}
