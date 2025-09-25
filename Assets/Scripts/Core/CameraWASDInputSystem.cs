using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraWASDInputSystem : MonoBehaviour
{
    [SerializeField] private InputActionReference _move;
    [SerializeField] private InputActionReference _look;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private bool _clampToBounds = false;
    [SerializeField] private Vector2 _minXZ = new (-100f, -100f);
    [SerializeField] private Vector2 _maxXZ = new (100f, 100f);

    [SerializeField] private bool _requireRightMouseToRotate = true;
    [SerializeField] private float _rotateSpeed = 200f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 80f;
    [SerializeField] private bool _invertY = false;

    private float _yStart;
    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        _yStart = transform.position.y;
        Vector3 angle = transform.rotation.eulerAngles;
        _yaw = angle.y;
        _pitch = NormalizePitch(angle.x);
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
    }

    private void OnEnable()
    {
        Enable(_move);
        Enable(_look);
    }

    private void OnDisable()
    {
        Disable(_move);
        Disable(_look);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        
        bool hold = _requireRightMouseToRotate == false ||
                     (Mouse.current?.rightButton.isPressed ?? false);

        if (hold)
        {
            Vector2 d = ReadVector2(_look);

            if (d.sqrMagnitude > 0f)
            {
                float signY = _invertY ? 1f : -1f;
                
                _yaw += d.x * _rotateSpeed * deltaTime * 0.01f;
                _pitch += d.y * _rotateSpeed * deltaTime * 0.01f * signY;
                _pitch = Mathf.Clamp(_pitch, _maxPitch, _maxPitch);
            }
        }
        
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);

        Vector2 m = ReadVector2(_move);
        
        if(m.sqrMagnitude > 1f) m.Normalize();
        
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        fwd.Normalize();
        
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();
        
        Vector3 delta = (fwd * m.y + right * m.x) * _moveSpeed * deltaTime;
        Vector3 position = transform.position + delta;

        if (_clampToBounds)
        {
            position.x = Mathf.Clamp(position.x, _minXZ.x, _maxXZ.x);
            position.y = Mathf.Clamp(position.y, _minXZ.y, _maxXZ.y);
        }
        
        position.y = _yStart;
        transform.position = position;
    }

    private Vector2 ReadVector2(InputActionReference reference) =>
        (reference != null && reference.action != null) ? reference.action.ReadValue<Vector2>() : Vector2.zero;

    private void Enable(InputActionReference reference)
    {
        if (reference != null && reference.action != null && reference.action.enabled == false)
        {
            reference.action.Enable();
        }
    }

    private void Disable(InputActionReference reference)
    {
        if (reference != null && reference.action != null && reference.action.enabled)
        {
            reference.action.Disable();
        }
    }

    private float NormalizePitch(float x)
    {
        x = Mathf.Repeat(x + 180f, 360f) - 180f;
        
        return x;
    }
}
