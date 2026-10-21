using UnityEngine;
using UnityEngine.InputSystem;

public class InteractSystem : MonoBehaviour {
    [Min(0.5f)]
    [SerializeField] private float _interactDistance = 2f;

    public void Interact(InputAction.CallbackContext context) {
        if (context.started && Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _interactDistance)) {
            if (hit.transform.gameObject.TryGetComponent(out IInteractable interactable)) {
                interactable.Interact(this);
            }
        }
    }
}
