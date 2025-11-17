using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData;
    private int currentNodeIndex;

    void Awake()
    {
        Instance = this;
    }

    public void StartDialogue(DialogueData data)
    {
        currentData = data;
        currentNodeIndex = data.startNodeIndex;

        DialogueUI.Instance.Show();
        ShowNode(currentNodeIndex);
    }

    public void ShowNode(int nodeIndex)
    {
        var node = currentData.nodes[nodeIndex];

        DialogueUI.Instance.DisplayNode(node);
    }

    public void GoToNextNode(int index)
    {
        if (index < 0)
        {
            EndDialogue();
            return;
        }

        currentNodeIndex = index;
        ShowNode(currentNodeIndex);
    }

    public void EndDialogue()
    {
        DialogueUI.Instance.Hide();
        currentData = null;
    }
}
