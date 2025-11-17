using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public enum InfoType { NowHP, NowHunger, NowPhase, NowDays }
    public InfoType type;
    public RectTransform TimeClock;

    Text myText;
    Slider mySlider;
    Image myImage;
    public Sprite Clock_Day;
    public Sprite Clock_Night;


    void Awake()
    {
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
        myImage = GetComponent<Image>();
    }

    void LateUpdate()
    {
        switch (type) {
            case InfoType.NowHP:
                float curHP = HeroStat.Instance.hp;
                float maxHp = HeroStat.Instance.maxHp;
                mySlider.value = curHP / maxHp;
                break;
            case InfoType.NowHunger:
                float curHunger = HeroStat.Instance.hunger;
                float maxHunger = HeroStat.Instance.maxHunger;
                mySlider.value = curHunger / maxHunger;
                break;
            case InfoType.NowPhase:
                Phase curDays = GameManager.Instance.CurrentPhase;
                if (curDays == Phase.Day)
                    myImage.sprite = Clock_Day;
                else
                    myImage.sprite = Clock_Night;
                break;
            case InfoType.NowDays:
                myText.text = string.Format("Day {0}", (int)GameManager.Instance.CurrentDay + 1);
                break;
        }
    }
}
