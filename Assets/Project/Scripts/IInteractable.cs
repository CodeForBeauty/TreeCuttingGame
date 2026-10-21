
using UnityEngine;

public interface IInteractable {
    public void Interact(InteractSystem system, RaycastHit hit);
}
