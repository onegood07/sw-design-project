using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public enum InfoType { NowHP, NowHunger, NowPhase, NowDays }
    // HUD에 표시할 정보 타입 열거형으로 정의
    public InfoType type;
    public RectTransform TimeClock;         // TimeClock 객체 연결

    Text myText;
    Slider mySlider;
    Image myImage;
    public Sprite Clock_Day;            // 낮 이미지 스프라이트 연결 변수 추가
    public Sprite Clock_Night;          // 밤 이미지 스프라이트 연결 변수 추가


    void Awake()
    {
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
        myImage = GetComponent<Image>();        // 컴포넌트 받기
    }

    void LateUpdate()
    {
        switch (type) {
            case InfoType.NowHP: // HP 슬라이드 바
                float curHP = HeroStat.Instance.hp;
                float maxHp = HeroStat.Instance.maxHp;
                mySlider.value = curHP / maxHp;
                break;          // 히어로 스탯의 값을 받아와 변동값 계산, 적용
            case InfoType.NowHunger:    // 허기 슬라이드 바
                float curHunger = HeroStat.Instance.hunger;
                float maxHunger = HeroStat.Instance.maxHunger;
                mySlider.value = curHunger / maxHunger;
                break;          // 히어로 스탯의 값을 받아와 변동값 계산, 적용
            case InfoType.NowPhase:     // 페이즈 표시 - 시계 이미지
                Phase curDays = GameManager.Instance.CurrentPhase;
                if (curDays == Phase.Day)
                    myImage.sprite = Clock_Day;
                else
                    myImage.sprite = Clock_Night;
                break;              // 게임 매니저의 페이즈 정보를 받아와 이미지 적용
            case InfoType.NowDays:  // 날짜 표시 - 텍스트 박스
                myText.text = string.Format("Day {0}", (int)GameManager.Instance.CurrentDay + 1);
                break;              // 게임 매니저의 날짜 열거 변수를 받아와 적용
        }
    }
}
