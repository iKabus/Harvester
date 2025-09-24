using System;
using TMPro;
using UnityEngine;

public class ResourceCounterUI : MonoBehaviour
{
    [SerializeField] private Base _targetBase;
    [SerializeField] private TextMeshProUGUI _resourceCounter;
    [SerializeField] private string labelPrefix =  "";
    [SerializeField] private int _startValue = 0;
    [SerializeField] private bool _billboardToCamera = true;

    private int _count;

    private void Reset()
    {
        if(_targetBase == null)
            _targetBase = GetComponentInParent<Base>();
    }

    private void OnEnable()
    {
        _count  = _startValue;
        
        UpdateText();

        if (_targetBase != null)
        {
            _targetBase.ResourceDelivered += HandleDelivered;
        }
    }

    private void OnDisable()
    {
        _targetBase.ResourceDelivered -= HandleDelivered;
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        
        if(_billboardToCamera == false || _resourceCounter == null) return;

        if (camera != null)
        {
            _resourceCounter.transform.rotation = Quaternion.LookRotation(_resourceCounter.transform.position - camera.transform.position);
        }
    }

    private void HandleDelivered(Resource resource)
    {
        _count++;

        UpdateText();
    }

    private void UpdateText()
    {
        if (_resourceCounter != null)
        {
            _resourceCounter.text = $"{labelPrefix}{_count}";
        }
    }
}
