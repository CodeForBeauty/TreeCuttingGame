using UnityEngine;

[RequireComponent(typeof(LevelLoader))]
public class LevelEnd : MonoBehaviour {
    [SerializeField] private TreeFall _fall;

    private LevelLoader _loader;

    private void Start() {
        if (_fall == null) {
            Debug.LogError("LevelEnd not setup");
            return;
        }

        _fall.OnFell += End;

        _loader = GetComponent<LevelLoader>();
    }

    private void OnDestroy() {
        if (_fall != null) {
            _fall.OnFell -= End;
        }
    }

    private void End() {
        _loader.Load();
    }
}
