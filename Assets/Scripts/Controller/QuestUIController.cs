using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 UI 패널의 활성화/비활성화 및 메인 슬롯 관리를 담당하는 싱글톤입니다.
/// </summary>
public class QuestUIController : MonoBehaviour
{
    public static QuestUIController instance;

    [Header("UI 연결")]
    public Button submitButton;
    public Button closeButton;
    public QuestSlot mainQuestSlot; // QuestGiver가 데이터를 주입할 대상 슬롯
    public GameObject questPanel;

    [Tooltip("퀘스트 버튼과 텍스트를 담고 있는 SubmitImage 패널을 연결하세요.")]
    public GameObject submitImagePanel;

    private void Awake()
    {
        // 싱글톤 초기화
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // if (questPanel != null)
        // {
        //     questPanel.SetActive(false);
        // }

        // mainQuestSlot 자동 참조 (Inspector에 연결 안 됐을 때)
        if (mainQuestSlot == null && questPanel != null)
        {
            mainQuestSlot = questPanel.GetComponentInChildren<QuestSlot>();
            if (mainQuestSlot != null)
            {
                Debug.Log("[QuestUIController] MainQuestSlot 자동 연결 완료.");
            }
            else
            {
                Debug.LogWarning("[QuestUIController] MainQuestSlot 자동 연결 실패. UI 구조 확인 필요.");
            }
        }
    }

    private void Start()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(QuestManager.instance.OnSubmitButtonClicked);
        }
        else
        {
            Debug.LogError("[QuestUIController] Submit Button이 연결되지 않았습니다.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            Debug.LogError("[QuestUIController] Close Button이 연결되지 않았습니다.");
        }

        if (mainQuestSlot == null)
        {
            Debug.LogError("[QuestUIController] Main Quest Slot이 연결되지 않았습니다.");
        }
    }

    public void OpenPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }

        if (submitImagePanel != null)
        {
            submitImagePanel.SetActive(true);
        }
        else if (questPanel != null)
        {
            // Inspector 연결 안 했을 때 자동 참조
            Transform submitImageTransform = questPanel.transform.Find("SubmitImage");
            if (submitImageTransform != null)
            {
                submitImagePanel = submitImageTransform.gameObject;
                submitImagePanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[QuestUIController] SubmitImagePanel을 찾을 수 없습니다. UI 구성 확인 필요.");
            }
        }

        Debug.Log("Quest UI Panel Opened.");
    }

    public void ClosePanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        if (mainQuestSlot != null)
        {
            mainQuestSlot.ClearTemporarySlot();
        }

        Debug.Log("Quest UI Panel Closed.");
    }
}
