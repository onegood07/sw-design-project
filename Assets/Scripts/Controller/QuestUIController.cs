using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀘스트 UI 패널의 활성화/비활성화 및 메인 슬롯 관리를 담당하는 싱글톤입니다.
/// </summary>
public class QuestUIController : MonoBehaviour
{
    // ⭐ 1. 싱글톤 인스턴스 변수
    public static QuestUIController instance; 
    
    [Header("UI 연결")]
    public Button submitButton; // 유니티 인스펙터에서 '아이템 납입하기' 버튼을 연결
    public Button closeButton;  // X 버튼 (창 닫기 버튼)
    public QuestSlot mainQuestSlot; // ⭐ QuestGiver가 데이터를 주입할 대상 슬롯을 연결
    public GameObject questPanel; // 퀘스트 UI 패널의 루트 오브젝트 (SubmitCanvas)
    
    // ⭐ 새로 추가: SubmitImage 오브젝트 연결 필드
    [Tooltip("퀘스트 버튼과 텍스트를 담고 있는 SubmitImage 패널을 연결하세요.")]
    public GameObject submitImagePanel; // ⬅️ Inspector에서 SubmitImage를 연결해야 합니다!

    private void Awake()
    {
        // 2. 싱글톤 초기화 로직
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // 씬이 로드될 때마다 퀘스트 UI가 닫힌 상태로 시작하도록 설정
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 버튼 클릭 이벤트에 함수 연결
        if (submitButton != null)
        {
            // QuestManager의 로직을 호출하도록 변경
            submitButton.onClick.AddListener(QuestManager.instance.OnSubmitButtonClicked); 
        }
        else
        {
            Debug.LogError("[QuestUIController] Submit Button이 연결되지 않았습니다.");
        }
        
        // 닫기 버튼 이벤트에 ClosePanel() 연결
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
    
    /// <summary>
    /// QuestManager에서 호출하여 UI 패널을 엽니다. (대화 중 퀘스트 요청 시 호출)
    /// </summary>
    public void OpenPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(true); // 1. 전체 캔버스 활성화
        }
        
        // ⭐ 핵심 수정: SubmitImage Panel 강제 활성화 (버튼 안 보이는 문제 해결)
        if (submitImagePanel != null)
        {
            submitImagePanel.SetActive(true); // 2. 배경 패널 및 모든 자식 요소 활성화
        }
        else
        {
            // 안전 장치: Inspector 연결을 잊은 경우를 대비하여 하위 요소를 찾아서 켭니다.
            Transform submitImageTransform = questPanel.transform.Find("SubmitImage");
             if (submitImageTransform != null)
             {
                 submitImagePanel = submitImageTransform.gameObject;
                 submitImagePanel.SetActive(true);
             } else {
                 Debug.LogWarning("[QuestUIController] SubmitImagePanel을 찾을 수 없습니다. UI 구성 확인이 필요합니다.");
             }
        }
        
        Debug.Log("Quest UI Panel Opened.");
    }
    
    /// <summary>
    /// 창 닫기 함수
    /// </summary>
    public void ClosePanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false); // UI 패널 비활성화
        }
        
        // 중요: 창이 닫힐 때 임시 배치 상태를 초기화하여 UI 오류 방지
        if (mainQuestSlot != null)
        {
            mainQuestSlot.ClearTemporarySlot();
        }
        Debug.Log("Quest UI Panel Closed.");
    }
}