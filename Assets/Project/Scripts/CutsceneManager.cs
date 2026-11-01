using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneManager : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private GameObject _textBox;

    [Tooltip("Per character")]
    [SerializeField] private float _typeTime;

    public static CutsceneManager Instance { get; private set; }

    private void Awake() {
        Instance = this;
        _textBox.SetActive(false);
    }

    public void ShowNext(string text) {
        _textBox.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateText(text));
    }

    public void Hide() {
        _textBox.SetActive(false);
    }

    private IEnumerator AnimateText(string text) {
        int current = 0;
        var wait = new WaitForSeconds(_typeTime);
        _text.text = "";
        while (current < text.Length) {
            _text.text += text[current];
            yield return wait;
            current++;
        }
    }
}
