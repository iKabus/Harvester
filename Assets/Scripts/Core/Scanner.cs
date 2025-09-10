using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    [SerializeField] private float _radius = 40f;
    [SerializeField] private float _interval = 3f;

    private Dictionary<Resource, bool> _resources = new();
    private HashSet<Unit> _units = new();

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
        
        foreach (var resource in _resources.Keys)
        {
            if (resource != null)
            {
                resource.TriggeredBaseEnter -= ResourceOnBase;
            }
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

    public IReadOnlyCollection<Unit> GetUnits()
    {
        CleanUnitsList();

        return _units;
    }

    private void ResourceOnBase(Resource resource)
    {
        if (_resources.ContainsKey(resource))
        {
            resource.TriggeredBaseEnter -= ResourceOnBase;
            
            _resources.Remove(resource);
        }
    }

    private IEnumerator Scaning()
    {
        var wait = new WaitForSeconds(_interval);

        while (enabled)
        {
            yield return wait;

            ScanForObjects();
        }
    }
    
    private void ScanForObjects()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);
        
        ProcessCollidersForResources(colliders);
        ProcessCollidersForUnits(colliders);
        
        CleanDictionary();
    }

    private void ProcessCollidersForResources(Collider[] colliders)
    {
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Resource resource))
                AddResource(resource);
        }
    }
    
    private void AddResource(Resource resource)
    {
        if (_resources.ContainsKey(resource))
        {
            return;
        }

        _resources.Add(resource, false);
        
        resource.TriggeredBaseEnter += ResourceOnBase;
    }

    private void ProcessCollidersForUnits(Collider[] colliders)
    {
        _units.Clear();

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Unit unit))
            {
                _units.Add(unit);
            }
        }
    }

    private void CleanDictionary()
    {
        List<Resource> resourcesToRemove = new List<Resource>();

        foreach (var resource in _resources)
        {
            if (resource.Key == null)
            {
                resourcesToRemove.Add(resource.Key);
            }
        }

        foreach (var resource in resourcesToRemove)
        {
            resource.TriggeredBaseEnter -= ResourceOnBase;

            _resources.Remove(resource);
        }
    }

    private void CleanUnitsList()
    {
        _units.RemoveWhere(unit => unit == null);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}