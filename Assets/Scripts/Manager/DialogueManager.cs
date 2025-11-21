using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentData; // 현재 대화 데이터
    private int currentNodeIndex; // 현재 보고 있는 노드(대화 분기점) 인덱스

    void Awake()
    {
        // 싱글톤 초기화
        Instance = this;
    }

    // 새로운 대화를 시작할 때 호출
    public void StartDialogue(DialogueData data)
    {
        currentData = data; // 대화 데이터를 현재 대화 데이터로 저장
        currentNodeIndex = data.startNodeIndex; // 시작 노드 지정

        DialogueUI.Instance.Show(); // 해당 대화 UI 열기
        ShowNode(currentNodeIndex); // 현재 노드 표기
    }

    // 특정 노드를 UI에 표기
    public void ShowNode(int nodeIndex)
    {
        var node = currentData.nodes[nodeIndex]; // 노드 정보 가져오기

        DialogueUI.Instance.DisplayNode(node); // UI에 출력
    }

    // 다음 노드로 넘어갈 때 호출
    public void GoToNextNode(int index)
    {
        // -1 등 음수면 대화 종료 (현재는 대화 종료를 -1로 설정한 상태)
        if (index < 0)
        {
            EndDialogue();
            return;
        }

        currentNodeIndex = index; // 다음 노드로 이동
        ShowNode(currentNodeIndex); // 노드 출력
    }

    // 대화 종료 처리
    public void EndDialogue()
    {
        DialogueUI.Instance.Hide(); // UI 닫기
        currentData = null; // 현재 대화 데이터 비움
    }
}
