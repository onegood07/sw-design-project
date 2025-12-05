using UnityEngine;

/// <summary>
/// 특정 QuestData를 QuestSlot에 할당하고 UI를 여는 스크립트입니다.
/// NPC 또는 UI 버튼에 연결하여 사용합니다.
/// </summary>
public class QuestGiver : MonoBehaviour
{
    [Header("퀘스트 데이터")]
    // Inspector에서 할당할 QuestData
    public QuestData questToAssign;

    // QuestUIController의 메인 슬롯 참조
    private QuestSlot targetSlot;

    private void Start()
    {
        // QuestUIController 싱글톤에서 메인 슬롯 참조
        if (QuestUIController.instance != null)
        {
            targetSlot = QuestUIController.instance.mainQuestSlot;
        }

        if (targetSlot == null)
        {
            Debug.LogWarning("[QuestGiver] QuestUIController의 mainQuestSlot을 찾을 수 없습니다. UI 구조 확인 필요.");
        }
    }

    /// <summary>
    /// NPC 상호작용 또는 버튼 클릭 시 호출
    /// </summary>
    public void OpenQuestUIAndAssign()
    {
        if (questToAssign == null)
        {
            Debug.LogError("[QuestGiver] 할당할 QuestData가 Inspector에 연결되지 않았습니다.");
            return;
        }

        if (targetSlot != null)
        {
            // QuestData를 슬롯에 세팅
            targetSlot.SetupSlot(questToAssign);

            // UI 열기 (SubmitImagePanel 포함)
            if (QuestUIController.instance != null)
            {
                QuestUIController.instance.OpenPanel();
                Debug.Log($"[QuestGiver] 퀘스트 '{questToAssign.questName}' 할당 및 UI 열기 완료.");
            }
            else
            {
                Debug.LogWarning("[QuestGiver] QuestUIController 인스턴스를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[QuestGiver] Main QuestSlot이 존재하지 않아 UI를 열 수 없습니다.");
        }
    }
}
