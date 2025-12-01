using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : MonoBehaviour
{
    // ⭐ 1. 싱글톤 인스턴스 변수 추가
    public static QuestUIController instance; 
    
    public Button submitButton; // 유니티 인스펙터에서 '아이템 납입하기' 버튼을 연결
    public QuestSlot mainQuestSlot; // 유니티 인스펙터에서 제출 대상 QuestSlot을 연결
    public GameObject questPanel; // ⭐ 퀘스트 UI 패널의 루트 오브젝트 (ClosePanel에 필요)

    private void Awake()
    {
        // 2. 싱글톤 초기화 로직 추가
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        // 버튼 클릭 이벤트에 함수 연결
        if (submitButton != null && mainQuestSlot != null)
        {
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }
    }

    private void OnSubmitButtonClicked()
    {
        // 퀘스트 슬롯의 확정 제출 함수 호출
        mainQuestSlot.ConfirmSubmission();
    }
    
    // ⭐ 3. QuestManager에서 호출하는 창 닫기 함수 추가
    public void ClosePanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false); // UI 패널 비활성화
        }
        
        // 4. 중요: 창이 닫힐 때 임시 배치 상태를 초기화하여 UI 오류 방지
        if (mainQuestSlot != null)
        {
            mainQuestSlot.ClearTemporarySlot();
        }
        Debug.Log("Quest UI Panel Closed.");
    }
}