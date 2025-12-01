using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData; // 현재 DialogueData
    private int currentNodeIndex; // 현재 노드 인덱스
    private DialogueNPC currentNPC; // 현재 NPC
    
    // 현재 대화가 활성 상태인지 추적
    public bool IsDialogueActive { get; private set; } = false;

    void Awake() { Instance = this; }

    public void StartDialogue(DialogueData data, DialogueNPC npc)
    {
        currentData = data;
        currentNodeIndex = data.startNodeIndex;
        currentNPC = npc; 

        IsDialogueActive = true; // 대화 시작 시 상태 업데이트 (좀비 움직이지 못하게 하는 용도)

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
        if (index < 0) { EndDialogue(); return; }

        currentNodeIndex = index;
        ShowNode(currentNodeIndex);
    }

    public void EndDialogue()
    {
        DialogueUI.Instance.Hide();
        currentData = null;

        // 대화 종료 시 NPC 호출
        if (currentNPC != null)
        {
            currentNPC.OnDialogueEnd();
            currentNPC = null;
        }

        IsDialogueActive = false;
    }
}