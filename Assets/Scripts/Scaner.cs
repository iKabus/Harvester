using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scaner : MonoBehaviour
{
    [SerializeField] private float _radius = 40f;
    [SerializeField] private float _interval = 3f;

    private Dictionary<Resource, bool> _resources = new Dictionary<Resource, bool>();

    private Coroutine _scaning;

    private void Start()
    {
        _scaning = StartCoroutine(Scaning());
    }

    private void OnDestroy()
    {
        if (_scaning != null)
        {
            StopCoroutine(_scaning);
        }
    }

    public Resource GetResource()
    {
        CleanDictionary();

        foreach (var (resource, isAssigned) in _resources)
        {
            if (resource != null && isAssigned == false)
            {
                _resources[resource] = true;
                
                return resource;
            }
        }
        
        return null;
    }

    private IEnumerator Scaning()
    {
        var wait = new WaitForSeconds(_interval);
        
        while (enabled)
        {
            yield return wait;

            ScanForResources();
        }
    }

    private void ScanForResources()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Resource resource))
            {
                if (_resources.ContainsKey(resource) == false)
                {
                    _resources.Add(resource, false);
                    Debug.Log($"Обнаружен новый ресурс. Всего ресурсов: {_resources.Count}");
                }
            }
        }
        
        CleanDictionary();
    }

    private void CleanDictionary()
    {
        List<Resource> resourcesToRemove = new List<Resource>();

        foreach (var (resource, isAssigned) in _resources)
        {
            if (resource == null)
            {
                resourcesToRemove.Add(resource);
            }
        }

        foreach (var resource in resourcesToRemove)
        {
            _resources.Remove(resource);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
