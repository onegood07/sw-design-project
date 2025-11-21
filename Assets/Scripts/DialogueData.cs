using UnityEngine;

// ScriptableObject를 사용하여 대화 데이터를 에셋으로 저장
[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public DialogueNode[] nodes;      // 대화 노드 배열 (대화 흐름 저장)
    public int startNodeIndex = 0;    // 대화 시작 노드 인덱스
}

// 개별 대화 노드 클래스
[System.Serializable]
public class DialogueNode
{
    [TextArea(2,6)]
    public string text; // 대화 텍스트

    public float typingSpeed = 0.03f; // 타이핑 효과 속도 (초 단위)

    public bool hasChoices = false; // 선택지가 있는지 여부
    public DialogueChoice[] choices; // 선택지 배열 (있을 경우)

    [Tooltip("-1이면 자동으로 종료")]
    public int nextNodeIndex = -1; // 다음 노드 인덱스 (-1이면 대화 종료)
}

// 대화 선택지 클래스
[System.Serializable]
public class DialogueChoice
{
    public string choiceText; // 선택지 텍스트
    public int nextNodeIndex = -1; // 선택지 선택 시 이동할 노드 인덱스
}
