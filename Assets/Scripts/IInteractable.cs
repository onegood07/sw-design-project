// 규칙 추가 IInteractable 을 가진 컴포넌트는 OnInteract 함수를 필수적으로 가지고 있어야 하며
// 가지고 있지 않은 경우 에러가 발생한다. HeroInteraction 에서 Object의 OnInteract를 실행시킬 수 있음.
public interface IInteractable
{
    void OnInteract();
}