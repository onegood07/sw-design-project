using UnityEngine;

public class DialogueNPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogueData; 
    private ZombieMove[] zombies;          // ZombieMove 스크립트 참조
    private ZombieNavMove[] navZombies;    // ZombieNavMove 스크립트 참조

    void Start()
    {
        zombies = FindObjectsByType<ZombieMove>(FindObjectsSortMode.None);
        navZombies = FindObjectsByType<ZombieNavMove>(FindObjectsSortMode.None);
    }

    // 플레이어가 상호작용할 때
    public void OnInteract()
    {
        DialogueManager.Instance.StartDialogue(dialogueData, this); // NPC 전달
        Debug.Log($"{gameObject.name}: 대화 시작");

        // 모든 좀비 이동 잠금
        foreach (var zombie in zombies) zombie.isInDialogue = true;
        foreach (var zombie in navZombies) zombie.isInDialogue = true;
    }

    // 대화 종료 시
    public void OnDialogueEnd()
    {
        foreach (var zombie in zombies) zombie.isInDialogue = false;
        foreach (var zombie in navZombies) zombie.isInDialogue = false;
    }
}
