using UnityEngine;

public class GameTimeScale : MonoBehaviour {
    [SerializeField] private float _scale = 1f;

    private void Update() {
        Time.timeScale = _scale;
    }
}
