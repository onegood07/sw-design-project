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
    public GameObject submitImagePanel; // (QuestManager의 submitImagePanel과 동일한 오브젝트여야 함)

    private void Awake()
    {
        // 싱글톤 초기화
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

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
            if (QuestManager.instance != null)
            {
                submitButton.onClick.AddListener(QuestManager.instance.OnSubmitButtonClicked);
            }
            else
            {
                 Debug.LogError("[QuestUIController] QuestManager 인스턴스를 찾을 수 없습니다. SubmitButton 리스너 연결 실패.");
            }
        }
        else
        {
            Debug.LogError("[QuestUIController] Submit Button이 연결되지 않았습니다.");
        }

        if (closeButton != null)
        {
            // ClosePanel 함수를 X 버튼에 연결합니다.
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
            // Inspector 연결 안 했을 때 자동 참조 시도
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

    /// <summary>
    /// UI 닫기 버튼 클릭 시 호출 (대화 상태 정리 로직 포함)
    /// </summary>
    public void ClosePanel()
    {
        // 퀘스트 UI 컨트롤러의 내부 패널을 끄는 것은 옵션. QuestManager가 최종 정리 담당.
        if (questPanel != null)
            questPanel.SetActive(false); 

        if (mainQuestSlot != null)
            mainQuestSlot.ClearTemporarySlot();

        Debug.Log("[QuestUIController] Quest UI Panel Closed via X button. QuestManager에게 정리 요청.");

        // ✅ 핵심: QuestManager에게 제출 상태와 대화 상태를 모두 정리하도록 요청
        if (QuestManager.instance != null)
        {
            QuestManager.instance.CloseSubmitUI(); 
        }
    }

}