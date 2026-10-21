using FP;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private FPMovement _playerController;
    [SerializeField] private GameObject _pauseMenu;

    private bool _isPaused = false;

    private void Start() {
        if (_pauseMenu == null || _playerController == null) {
            Debug.LogError("Game Manager variables not set up fully");
            return;
        }

        UpdatePaused();
    }

    public void TogglePause() {
        _isPaused = !_isPaused;

        UpdatePaused();
    }

    private void UpdatePaused() {
        if (_pauseMenu == null || _playerController == null) {
            return;
        }

        _playerController.isControllable = !_isPaused;

        _pauseMenu.SetActive(_isPaused);
    }
}
