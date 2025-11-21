using UnityEngine;

// 사용 가능한 아이템 스크립트는 전부 해당 규칙을 상속받아야 한다
public interface IUsable
{
    // 인수 추가
    void Use(Transform HeroTransform, Vector2 viewDirection);
}