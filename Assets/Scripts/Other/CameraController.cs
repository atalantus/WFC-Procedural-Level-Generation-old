using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    private Camera _camera;
    private EventSystem _eventSystem;

    private bool _isRotating;
    private Vector3 _rotatingPos;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _eventSystem = EventSystem.current;
    }

    public void AdjustCamera(int width, int height)
    {
        var x = Mathf.Max(width, height);

        _camera.orthographicSize = 0.4f * x + 0.5f;

        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, 0.5f, 50f);
    }

    private void Update()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            _camera.orthographicSize += -scroll * Time.deltaTime * 75 * (_camera.orthographicSize / 4);

            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, 0.5f, 50f);
        }

        if (Input.GetMouseButtonDown(0) && !_isRotating && _eventSystem.currentSelectedGameObject == null)
        {
            // Start rotation
            _rotatingPos = Input.mousePosition;
            _isRotating = true;
        }
        else if (Input.GetMouseButtonUp(0) && _isRotating)
        {
            // Stop rotation
            _isRotating = false;
        }
        else if (_isRotating)
        {
            // Rotate
            var mousePos = Input.mousePosition;
            var offset = mousePos.x - _rotatingPos.x;

            _rotatingPos = mousePos;

            transform.RotateAround(Vector3.zero, Vector3.up, offset * Time.deltaTime * 5);
        }
    }
}