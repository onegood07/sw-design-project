using UnityEngine;

public class DialogueNPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogueData; // 이 NPC와 연결된 대화 데이터
    public void OnInteract()
    {
        // 스페이스바를 통해 상호작용이 감지되면 대화 시작
        DialogueManager.Instance.StartDialogue(dialogueData);
        Debug.Log($"{gameObject.name}: 대화 시작");
    }
}
