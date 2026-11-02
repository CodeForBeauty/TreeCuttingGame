using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TreeFallCheck : MonoBehaviour {
    [SerializeField] private TreeFall _fall;
    [SerializeField] private string _floorTag = "Floor";
    [SerializeField] private string _obstacleTag = "Obstacle";

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag(_floorTag)) {
            _fall.GroundHit();
        }
        if (other.gameObject.CompareTag(_obstacleTag)) {
            _fall.ObstacleHit();
        }
    }
}
