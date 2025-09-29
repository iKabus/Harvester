using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RtsFreeCamera : MonoBehaviour
{
    [SerializeField] private InputActionReference _move;
    
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _height = -1f;

    private void OnEnable()
    {
        if (_height < 0f) _height = transform.position.y;
        
        _move?.action?.Enable();
    }

    private void OnDisable()
    {
        _move?.action?.Disable();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        Vector2 value = _move?.action != null ? _move.action.ReadValue<Vector2>() : Vector2.zero;
        
        if (value.sqrMagnitude > 1f) value.Normalize();

        Vector3 fwd = transform.forward;
        fwd.y = 0f;  
        fwd.Normalize();
        
        Vector3 right = transform.right;
        right.y = 0f; 
        right.Normalize();

        Vector3 position = transform.position + (fwd * value.y + right * value.x) * _moveSpeed * deltaTime;
        position.y = _height;
        transform.position = position;
    }
}
