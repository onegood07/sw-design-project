using UnityEngine;
using UnityEngine.UI;

public class QuestUIController : MonoBehaviour
{
    public Button submitButton; // 유니티 인스펙터에서 '아이템 납입하기' 버튼을 연결
    public QuestSlot mainQuestSlot; // 유니티 인스펙터에서 제출 대상 QuestSlot을 연결

    private void Start()
    {
        // 버튼 클릭 이벤트에 함수 연결
        if (submitButton != null && mainQuestSlot != null)
        {
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }
    }

    // ⭐ 두 개의 OnSubmitButtonClicked 정의 중, 이 로직 하나만 남겨야 합니다.
    private void OnSubmitButtonClicked()
    {
        // 퀘스트 슬롯의 확정 제출 함수 호출
        mainQuestSlot.ConfirmSubmission();
        
        // (선택 사항) 제출 성공/실패 메시지 팝업 등을 추가할 수 있습니다.
    }
}