using UnityEngine;
using UnityEngine.InputSystem;

public class InteractSystem : MonoBehaviour {
    [SerializeField] private float _interactInterval = 0.2f;
    [Min(0.5f)]
    [SerializeField] private float _interactDistance = 2f;

    private float _interactTimeout = 0f;

    private void Update() {
        _interactTimeout = Mathf.Max(0f, _interactTimeout - Time.deltaTime);
    }

    public void Interact(InputAction.CallbackContext context) {
        if (_interactTimeout > 0f) {
            return;
        }
        if (context.started && Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _interactDistance)) {
            if (hit.transform.gameObject.TryGetComponent(out IInteractable interactable)) {
                interactable.Interact(this, hit);
                _interactTimeout = _interactInterval;
            }
        }
    }
}
