using UnityEngine;

public class TestInteract : MonoBehaviour, IInteractable {
    public void Interact(InteractSystem system) {
        print(system);
    }
}
