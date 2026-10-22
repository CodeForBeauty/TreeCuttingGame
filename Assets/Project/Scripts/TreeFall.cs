using UnityEngine;

public class TreeFall : MonoBehaviour {
    [Range(1f, 360f)]
    [SerializeField] private float _fallSpeed = 25f;

    // local
    private Vector3 _hitPoint = Vector3.zero;
    private bool _isFalling = false;

    private Vector3 _fallAxis = Vector3.zero;

    private void Update() {
        if (!_isFalling) {
            return;
        }

        transform.RotateAround(transform.TransformPoint(_hitPoint), _fallAxis, _fallSpeed * Time.deltaTime);
    }

    public void StartFall(Vector3 fallDirection, Vector3 hitPoint) {
        _isFalling = true;

        _hitPoint = hitPoint;

        _fallAxis = Vector3.Cross(Vector3.up, fallDirection);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(_hitPoint), 0.2f);
    }
}
