using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualOffice.Core
{
    /// WASD 이동 + 마우스 시점의 순수 1인칭 컨트롤러. 아바타 모델이 없으므로
    /// 이 오브젝트 자신은 카메라 피벗만 자식으로 둔다(손/몸 메시 없음).
    /// 신규 Input System을 InputActionAsset 없이, 디바이스 직접 폴링으로 사용한다 —
    /// 이 세션에는 Unity 에디터가 없어 .inputactions 자산의 GUID 참조가 깨지기 쉬우므로
    /// 가장 단순하고 안전한 방식을 택했다.
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("시점")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        /// FacilityController가 플레이어를 코드로 만들 때, 카메라도 같이 코드로 만들고
        /// 나서 바로 이 메서드로 연결한다(인스펙터 드래그 연결을 대신함).
        public void SetCameraPivot(Transform pivot)
        {
            cameraPivot = pivot;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraPivot == null && Camera.main != null)
            {
                cameraPivot = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cameraPivot == null) return;

            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;

            transform.Rotate(Vector3.up, delta.x);

            _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 move = (transform.right * input.x + transform.forward * input.y) * walkSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _controller.Move(move * Time.deltaTime);
        }
    }
}
