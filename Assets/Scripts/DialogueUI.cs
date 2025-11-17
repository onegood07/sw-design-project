using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI Elements")]
    public GameObject rootPanel;         // 전체 대화 UI
    public Text dialogueText;           
    public Transform choicesParent;      // 선택지 부모
    public GameObject choiceButtonPrefab;

    private Coroutine typingCoroutine;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show()
    {
        rootPanel.SetActive(true);
    }

    public void Hide()
    {
        rootPanel.SetActive(false);
    }

    public void DisplayNode(DialogueNode node)
    {
        // 선택지 초기화
        foreach (Transform child in choicesParent)
            Destroy(child.gameObject);

        // 텍스트를 타이핑
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(node.text, node.typingSpeed));

        // 선택지가 있을 경우 버튼 생성
        if (node.hasChoices && node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                GameObject btnObj = Instantiate(choiceButtonPrefab, choicesParent);
                Text btnText = btnObj.GetComponentInChildren<Text>(); 
                btnText.text = choice.choiceText;

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    DialogueManager.Instance.GoToNextNode(choice.nextNodeIndex);
                });
            }
        }
        else
        {
            // 선택지가 없으면 nextNodeIndex로 넘어감
            this.GetComponent<Button>().onClick.RemoveAllListeners();
            this.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager.Instance.GoToNextNode(node.nextNodeIndex);
            });
        }
    }

    IEnumerator TypeText(string text, float speed)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(speed);
        }
    }
}
