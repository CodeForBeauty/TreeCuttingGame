using UnityEngine;
using UnityEngine.InputSystem;

namespace FP {

    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    public class FPMovement : MonoBehaviour {
        [SerializeField] private Transform _camera;

        public bool isControllable = true;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        public float inAirControl = 0.5f;
        public float sprintMultiplier = 1.5f;

        private bool _isSprinting = false;

        [Header("Rotation")]
        [SerializeField] private float _rotSpeed = 10f;
        [Range(0f, 2f)]
        public float lookSensitivity = 1f;

        [SerializeField] private float _minPitch = -45f;
        [SerializeField] private float _maxPitch =  45f;

        [Header("Jump")]
        [SerializeField] private float _jumpStrength = 10f;

        public int maxJumps = 1;
        private int _currentJump = 0;

        [SerializeField] private float _gravityMultiplier = 1f;

        private const float _gravity = 9.814f;

        [Header("Chrouch")]
        [SerializeField] private float _crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float _crouchOffset = 0.5f;

        [SerializeField] private float _crouchSize = 0.5f;
        [SerializeField] private float _crouchTime = 0.5f;

        private float _startHeight = 0f;
        private float _currentHeight = 0f;

        private bool _isCrouching = false;
        public bool IsCrouching { get { return _isCrouching; } }

        private float _crouchT = 1f;


        private Vector3 _cameraRot = Vector3.zero;

        private Vector3 _inputVelocity = Vector3.zero;
        private Vector2 _inputTorque = Vector2.zero;
        private Vector3 _jumpVelocity = Vector3.zero;
        private Vector3 _velocity = Vector3.zero;
        public Vector3 Velocity { get { return _velocity; } }

        private CharacterController _controller;
        private CapsuleCollider _collider;
        public bool IsGrounded { get { return _controller.isGrounded; } }


        private void Awake() {
            _controller = GetComponent<CharacterController>();
            _collider = GetComponent<CapsuleCollider>();
        }

        private void Start() {
            _startHeight = _collider.height;
            _currentHeight = _startHeight;
        }

        private void Update() {
            if (!isControllable) {
                _inputVelocity = Vector3.zero;
                _inputTorque = Vector2.zero;
                _jumpVelocity = Vector3.zero;
            }


            // Movement
            if (IsGrounded) {
                _velocity = _moveSpeed * (transform.rotation * _inputVelocity);
            }
            else {
                _velocity = _moveSpeed * inAirControl * (transform.rotation * _inputVelocity);

                ForceStand();
            }
            // Crouch
            if (_crouchT <= 1) {
                if (_isCrouching) {
                    _currentHeight = Mathf.Lerp(_startHeight, _startHeight * _crouchSize, _crouchT);

                    _velocity *= _crouchSpeedMultiplier;

                    _controller.Move(Vector3.down * (_crouchOffset / _crouchTime * Time.deltaTime));
                }
                else {
                    _currentHeight = Mathf.Lerp(_startHeight * _crouchSize, _startHeight, _crouchT);

                    _controller.Move(Vector3.up * (_crouchOffset / _crouchTime * Time.deltaTime));
                }
                _collider.height = _currentHeight;
                _controller.height = _currentHeight;
                _crouchT += Time.deltaTime / _crouchTime;
            }

            // Apply movement
            if (_isSprinting) {
                _velocity *= sprintMultiplier;
            }
            _velocity += _jumpVelocity;

            _controller.Move(_velocity * Time.deltaTime);

            // Rotation
            transform.Rotate(0, _inputTorque.x * _rotSpeed * lookSensitivity * Time.deltaTime, 0);

            _cameraRot.x = Mathf.Max(_minPitch,
                                Mathf.Min(_maxPitch,
                                    _cameraRot.x + -_inputTorque.y * _rotSpeed * lookSensitivity * Time.deltaTime
                    ));
            _camera.localEulerAngles = _cameraRot;

            //Gravity
            if (_jumpVelocity.y <= -_gravity * _gravityMultiplier) {
                _jumpVelocity = new Vector3(0, -_gravity * _gravityMultiplier, 0);
            }
            else {
                _jumpVelocity.y -= _gravity * _gravityMultiplier * Time.deltaTime;
            }
        }

        public void Move(InputAction.CallbackContext context) {
            Vector2 value = context.ReadValue<Vector2>();
            _inputVelocity.x = value.x;
            _inputVelocity.z = value.y;
        }

        public void Jump(InputAction.CallbackContext context) {
            if (context.started) {
                if (IsGrounded) {
                    if (_isCrouching) {
                        ForceStand();
                        return;
                    }
                    _currentJump = 0;
                }
                if (_currentJump >= maxJumps) {
                    return;
                }
                Vector3 input = transform.rotation * _inputVelocity;
                input.y = _jumpStrength;

                _jumpVelocity = input.normalized * _jumpStrength;
                _currentJump++;
            }
        }

        public void Look(InputAction.CallbackContext context) {
            _inputTorque = context.ReadValue<Vector2>();
        }

        public void Sprint(InputAction.CallbackContext context) {
            _isSprinting = context.phase == InputActionPhase.Performed;
        }

        public void Crouch(InputAction.CallbackContext context) {
            if (context.started && IsGrounded) {
                _isCrouching = !_isCrouching;
                _crouchT = 0f;
            }
        }

        public void ForceStand() {
            if (_isCrouching) {
                _isCrouching = false;
                _crouchT = 0f;
            }
        }
    }

}