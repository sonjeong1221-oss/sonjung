using UnityEngine;
using UnityEngine.Events;

namespace VirtualOffice.Core
{
    /// 코드 없이 아무 오브젝트에나 붙여서 바로 상호작용 가능하게 만드는 범용 컴포넌트.
    /// 샘플룸 그레이박스 검증용으로 쓰고, 추후 실제 가구에도 재사용할 수 있다.
    public class SimpleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "E키로 상호작용";
        [SerializeField] private bool canInteract = true;
        [SerializeField] private UnityEvent onInteract;

        public string InteractionPrompt => prompt;
        public bool CanInteract => canInteract;

        public void Interact(FirstPersonController interactor)
        {
            onInteract?.Invoke();
        }
    }
}
