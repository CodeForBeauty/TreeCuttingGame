using UnityEngine;

public class UnlockCursor : MonoBehaviour {
    private void Awake() {
        Cursor.lockState = CursorLockMode.None;
    }
}
