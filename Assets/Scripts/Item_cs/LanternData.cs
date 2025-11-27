using UnityEngine;

[CreateAssetMenu(fileName = "NewLantern", menuName = "ItemData/LanternData")]
public class LanternData : ItemData, IUsable
{
    public void Use(Transform HeroTransform, Vector2 viewDirection)
    {
        if(PlayerLightControl.Instance != null)
        {
            PlayerLightControl.Instance.SetLanternActive();
        }
    }
}