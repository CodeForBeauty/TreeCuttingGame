using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour {
    [SerializeField] private string _levelName = "level";

    public void Load() {
        SceneManager.LoadScene(_levelName);
    }
}
