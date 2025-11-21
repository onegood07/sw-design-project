using UnityEngine;

public class DialogueNPC : MonoBehaviour
{
    public DialogueData dialogueData; // 이 NPC와 연결된 대화 데이터

    // 플레이어가 NPC 범위에 들어왔을 때 대화 시작
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))  // 충돌한 객체가 Player인지 확인
        {
            // DialogueManager를 통해 대화 시작
            DialogueManager.Instance.StartDialogue(dialogueData);
        }
    }
}
