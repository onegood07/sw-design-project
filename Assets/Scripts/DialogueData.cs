using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public DialogueNode[] nodes;
    public int startNodeIndex = 0;
}

[System.Serializable]
public class DialogueNode
{
    [TextArea(2,6)]
    public string text;             
    // 타이핑 속도
    public float typingSpeed = 0.03f;

    public bool hasChoices = false;  
    public DialogueChoice[] choices; 

    [Tooltip("-1이면 자동으로 종료")]
    public int nextNodeIndex = -1;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;     
    public int nextNodeIndex = -1; 
}
