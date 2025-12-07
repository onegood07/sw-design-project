using UnityEngine;

public class DialogueNPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogueData; 
    // private ZombieMove[] zombies;          // ❌ 더 이상 필요하지 않음
    // private ZombieNavMove[] navZombies;    // ❌ 더 이상 필요하지 않음

    void Start()
    {
        // ❌ 좀비 찾기 로직 제거
        // zombies = FindObjectsByType<ZombieMove>(FindObjectsSortMode.None);
        // navZombies = FindObjectsByType<ZombieNavMove>(FindObjectsSortMode.None);
    }

    // 플레이어가 상호작용할 때
    public void OnInteract()
    {
        // DialogueManager.StartDialogue 내부에서 GameManager.StartInteraction() 호출
        DialogueManager.Instance.StartDialogue(dialogueData, this); // NPC 전달
        Debug.Log($"{gameObject.name}: 대화 시작");

        // ❌ 좀비 이동 잠금 로직 제거
        // foreach (var zombie in zombies) zombie.isInDialogue = true;
        // foreach (var zombie in navZombies) zombie.isInDialogue = true;
    }

    // 대화 종료 시
    public void OnDialogueEnd()
    {
        // DialogueManager.EndDialogue 내부에서 GameManager.EndInteraction() 호출
        
        // ❌ 좀비 이동 해제 로직 제거
        // foreach (var zombie in zombies) zombie.isInDialogue = false;
        // foreach (var zombie in navZombies) zombie.isInDialogue = false;
    }
}