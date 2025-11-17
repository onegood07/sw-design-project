using UnityEngine;

public class DialogueNPC : MonoBehaviour
{
    public DialogueData dialogueData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DialogueManager.Instance.StartDialogue(dialogueData);
        }
    }
}
