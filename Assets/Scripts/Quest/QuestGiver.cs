using UnityEngine;
/// <summary>

/// 특정 QuestData를 QuestSlot에 할당하고 UI를 여는 테스트용 스크립트입니다.

/// NPC 또는 UI 버튼에 연결하여 사용합니다.

/// </summary>

public class QuestGiver : MonoBehaviour

{

[Header("퀘스트 데이터")]

// ⭐ Unity Inspector에서 생성한 QuestData 파일을 연결합니다.

public QuestData questToAssign;


// QuestUIController에 연결된 메인 슬롯

private QuestSlot targetSlot;



private void Start()

{

// QuestUIController의 싱글톤 인스턴스에서 메인 슬롯 참조를 가져옵니다.

if (QuestUIController.instance != null)

{

targetSlot = QuestUIController.instance.mainQuestSlot;

}


if (targetSlot == null)

{

Debug.LogWarning("[QuestGiver] QuestUIController의 mainQuestSlot을 찾을 수 없습니다. Quest UI 구조를 확인하세요.");

}

}


/// <summary>

/// NPC와 상호작용하거나 버튼을 눌렀을 때 호출되어 퀘스트를 할당하고 UI를 엽니다.

/// 이 함수를 버튼의 OnClick() 이벤트 등에 연결하여 테스트할 수 있습니다.

/// </summary>

public void OpenQuestUIAndAssign()

{

if (questToAssign == null)

{

Debug.LogError("[QuestGiver] 할당할 QuestData가 인스펙터에 연결되지 않았습니다.");

return;

}


if (targetSlot != null)

{

// ⭐ 핵심: QuestData 객체를 통째로 QuestSlot에 주입합니다.

targetSlot.SetupSlot(questToAssign);


// UI 패널 열기

if (QuestUIController.instance != null)

{

QuestUIController.instance.questPanel.SetActive(true);

Debug.Log($"[QuestGiver] 퀘스트 '{questToAssign.questName}' 할당 및 UI 열기 완료.");

}

}

}

}