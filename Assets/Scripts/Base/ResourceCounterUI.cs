using System;
using TMPro;
using UnityEngine;

public class ResourceCounterUI : MonoBehaviour
{
    [SerializeField] private Base _targetBase;
    [SerializeField] private TextMeshProUGUI _resourceCounter;
    [SerializeField] private string labelPrefix =  "";
    [SerializeField] private bool _billboardToCamera = true;
    
    private void OnEnable()
    {
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

    private void HandleDelivered()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        if (_resourceCounter != null)
        {
            _resourceCounter.text = $"{labelPrefix}{_targetBase.ResourceCount}";
        }
    }
}
