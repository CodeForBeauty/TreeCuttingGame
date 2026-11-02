using UnityEngine;

[RequireComponent(typeof(LevelLoader))]
public class LevelEnd : MonoBehaviour {
    [SerializeField] private TreeFall _fall;

    [SerializeField] private CutscenePlayer _cutscene;

    private LevelLoader _loader;

    private void Start() {
        if (_fall == null) {
            Debug.LogError("LevelEnd not setup");
            return;
        }

        if (_cutscene != null) {
            _fall.OnObstacleHit += PlayFail;
            _cutscene.OnFinished += End;
        }

        _fall.OnFell += End;

        _loader = GetComponent<LevelLoader>();
    }

    private void OnDestroy() {
        if (_fall != null) {
            _fall.OnFell -= End;
        }

        if (_cutscene != null) {
            _fall.OnObstacleHit -= PlayFail;
            _cutscene.OnFinished -= End;
        }
    }

    private void End() {
        _loader.Load();
    }

    private void PlayFail() {
        _cutscene.Play();
    }
}
