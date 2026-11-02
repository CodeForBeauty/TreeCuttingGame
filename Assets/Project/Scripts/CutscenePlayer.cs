using UnityEngine;

public class CutscenePlayer : MonoBehaviour {
    public event System.Action OnFinished = delegate{ };

    [SerializeField] private bool _playOnStart = false;

    [SerializeField] private string[] _texts;

    private int _currentText = 0;

    private void Start() {
        if (_playOnStart) {
            Play();
        }
    }

    public void Play() {
        _currentText = -1;
        CutsceneManager.Instance.SetCurrent(this);
        ShowNext();
    }

    public void ShowNext() {
        _currentText++;
        if (_currentText >= _texts.Length) {
            OnFinished();
            CutsceneManager.Instance.Hide();
            return;
        }
        CutsceneManager.Instance.ShowNext(_texts[_currentText]);
    }
}
