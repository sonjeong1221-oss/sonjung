using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualOffice.Core
{
    /// 1인칭 화면 중앙 기준 Raycast로 상호작용 가능한 오브젝트를 찾고,
    /// "E" 키 입력 시 해당 오브젝트의 Interact()를 호출한다.
    [RequireComponent(typeof(Camera))]
    public class InteractionSystem : MonoBehaviour
    {
        [Header("감지")]
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private LayerMask interactableMask = ~0;

        [Header("UI (선택 — 씬에 TMP_Text를 만들어 연결)")]
        [SerializeField] private TMP_Text promptLabel;

        private Camera _camera;
        private FirstPersonController _owner;
        private IInteractable _current;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _owner = GetComponentInParent<FirstPersonController>();
        }

        private void Update()
        {
            UpdateFocus();
            HandleInteractInput();
        }

        private void UpdateFocus()
        {
            IInteractable found = null;

            bool hitSomething = Physics.Raycast(
                _camera.transform.position,
                _camera.transform.forward,
                out RaycastHit hit,
                interactRange,
                interactableMask,
                QueryTriggerInteraction.Collide);

            if (hitSomething)
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (!ReferenceEquals(found, _current))
            {
                _current = found;
                RefreshPrompt();
            }
        }

        private void RefreshPrompt()
        {
            if (promptLabel == null) return;

            bool show = _current != null && _current.CanInteract;
            promptLabel.gameObject.SetActive(show);
            if (show)
            {
                promptLabel.text = _current.InteractionPrompt;
            }
        }

        private void HandleInteractInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.eKey.wasPressedThisFrame && _current != null && _current.CanInteract)
            {
                _current.Interact(_owner);
            }
        }
    }
}
