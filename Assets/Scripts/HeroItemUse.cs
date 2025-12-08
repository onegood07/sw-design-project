using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic; // 리스트 사용을 위해 필요

// 인벤토리 매니저 기능 과다로 인한 기능분리
// 아이템 사용 시, 아이템 내용은 매니저로부터 참조하여 해당 스크립트에서 사용한다.
public class HeroItemUse : MonoBehaviour
{

    private HeroMoveControl HeroMoveControl;
    private HeroAttackFlash heroAttackFlash;
    private HeroWeaponVisual heroWeaponVisual;
    private ItemData selectedItem;
    private int selectedItemIndex;
    private Vector2 mouseDirection;
    void Start()
    {
        // 이중으로 받아옴. 인스펙터에 지정하지 않아도 됨.
        HeroMoveControl = GetComponent<HeroMoveControl>();
        if (HeroMoveControl == null)
        {
            Debug.Log("heroMoveControl 참조 불가");
        }

        heroAttackFlash = GetComponent<HeroAttackFlash>();
        if (heroAttackFlash == null)
        {
            Debug.Log("HeroAttackFlash 참조 불가 (Hero 하위 Hero_S/N/W/E 이미지를 사용한 공격 플래시를 쓰지 않음)");
        }

        heroWeaponVisual = GetComponent<HeroWeaponVisual>();
        if (heroWeaponVisual == null)
        {
            Debug.Log("HeroWeaponVisual 참조 불가 (총 스프라이트 회전/표시 관리 안 함)");
        }
    }
    public void OnItemUse(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (IsPointerOverUIObject())
            {
                return;
            }
            // 스크린 포인트 값을 월드 포지션 값으로 변환함.
            // 스크린 포인트 값은 화면 왼쪽 아래가 0,0 임.
            Vector3 mouseWorldPosition3D = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 mouseWorldPosition = new Vector2(mouseWorldPosition3D.x, mouseWorldPosition3D.y);

            // 마우스 위치 - Hero 위치
            mouseDirection = mouseWorldPosition - (Vector2)transform.position;
            // 정규화
            mouseDirection.Normalize();

            // 현재 선택된 퀵슬롯 아이템이 무기일 경우에만 HeroAttackFlash / 총 임시 방향 전환 실행
            var currentData = GetSelectedQuickSlotItem();
            bool isWeapon = currentData != null && IsWeaponData(currentData);
            if (isWeapon)
            {
                if (heroAttackFlash != null)
                {
                    heroAttackFlash.ShowAttack(mouseDirection);
                }

                if (heroWeaponVisual != null)
                {
                    // 공격 방향(마우스 방향)을 기준으로 잠깐 다른 방향/각도로 총을 보여줬다가
                    // duration 이후에 원래 상태로 자동 복귀
                    heroWeaponVisual.ShowAttackOverride(mouseDirection, 0.1f);
                }
            }

            UseQuickSlot(selectedItemIndex,transform,mouseDirection);
            // // 사용 가능한 아이템일 경우(IUsable 규칙을 상속받은 데이터의 경우) Use 메서드가 존재함
            // if(selectedItem is IUsable usableData)usableData.Use(transform,mouseDirection);
            // else Debug.Log("사용할 수 없는 아이템");
        }
    }
    public void OnQuickSlot1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(1);
        }
    }
    public void OnQuickSlot2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(2);
        }
    }
    public void OnQuickSlot3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(3);
        }
    }
    public void OnQuickSlot4(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(4);
        }
    }

    public void OnQuickSlot5(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(5);
        }
    }

    public void OnQuickSlot6(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleQuickSlotSelection(6);
        }
    }

    private void UseQuickSlot(int slotNumber,Transform heroT, Vector2 useVec)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        InventoryManager.Instance.useQuickSlotItem(slotNumber,heroT,useVec);
    }

    // 실시간 마우스 위치를 얻어와서 해당 위치에 ui가 있는지 확인하기 위함.
    // EventSystem.current.IsPointerOverGameObject() 사용 시 이전 프레임에서의 값을 참조하여 경고 로그가 발생함.
    private bool IsPointerOverUIObject()
    {
        // 1. EventSystem 및 PointerData 생성
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        
        // 2. 현재 마우스 위치를 EventData에 설정
        eventData.position = Mouse.current.position.ReadValue();
        
        // 3. Raycast 결과를 담을 리스트
        List<RaycastResult> results = new List<RaycastResult>();
        
        // 4. UI Raycast 실행 (현재 마우스 위치에 UI 요소가 있는지 검사)
        EventSystem.current.RaycastAll(eventData, results);
        
        // results 리스트에 무언가 있다면 (UI 요소가 있다면) true 반환
        return results.Count > 0;
    }

    void ToggleQuickSlotSelection(int slotNumber)
    {
        if (selectedItemIndex == slotNumber)
        {
            selectedItemIndex = 0;
            QuickSlot.HighlightSlotByNumber(0);
        }
        else
        {
            selectedItemIndex = slotNumber;
            QuickSlot.HighlightSlotByNumber(selectedItemIndex);
        }

        UpdateAttackParameter();
    }

    ItemData GetSelectedQuickSlotItem()
    {
        if (InventoryManager.Instance == null || selectedItemIndex <= 0)
            return null;

        int idx = selectedItemIndex - 1;
        var quickSlots = InventoryManager.Instance.quickSlotItems;
        if (idx < 0 || idx >= quickSlots.Length)
            return null;

        return quickSlots[idx];
    }

    bool IsWeaponData(ItemData data)
    {
        return data is WeaponData;
    }

    void UpdateAttackParameter()
    {
        if (HeroMoveControl == null)
            return;

        var currentData = GetSelectedQuickSlotItem();
        bool isWeapon = currentData != null && IsWeaponData(currentData);
        Debug.Log($"[HeroItemUse] QuickSlot {selectedItemIndex} -> {(currentData != null ? currentData.name : "Empty")} | Weapon: {isWeapon}");
        HeroMoveControl.SetAttackEquipState(isWeapon);
    }
}