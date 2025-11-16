using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public enum InfoType { HP, Hunger, Time, Days }
    public InfoType type;

    Text myText;
    Slider mySlider;

    void Awake()
    {
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
    }

    void LateUpdate()
    {
        switch (type) {
            case InfoType.HP:
                float curHP = HeroStat.Instance.hp;
                float maxHp = HeroStat.Instance.maxHp;
                mySlider.value = curHP / maxHp;
                break;
            case InfoType.Hunger:
                break;
            case InfoType.Time:
                break;
            case InfoType.Days:
                break;
        }
    }
}
