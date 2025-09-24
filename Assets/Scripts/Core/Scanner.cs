using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    [SerializeField] private float _radius = 40f;
    [SerializeField] private float _interval = 3f;

    private readonly Dictionary<Resource, bool> _resources = new();
    private readonly HashSet<Unit> _units = new();

    private Coroutine _scanning;

    private void Start()
    {
        if (_scanning != null)
        {
            StopCoroutine(_scanning);
        }
        
        _scanning = StartCoroutine(Scanning());
    }

    private void OnDestroy()
    {
        if (_scanning != null)
        {
            StopCoroutine(_scanning);
            _scanning = null;
        }
    }
    
    public Resource GetResource()
    {
        CleanDictionary();

        foreach (var kv in _resources)
        {
            var resource = kv.Key;
            var isAssigned = kv.Value;

            if (resource != null && resource.gameObject.activeInHierarchy && isAssigned == false)
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

    private IEnumerator Scanning()
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

        var presentResources = ProcessCollidersForResources(colliders);
        
        ProcessCollidersForUnits(colliders);

        PruneResourcesNotPresent(presentResources);

        CleanDictionary();
    }
    
    private HashSet<Resource> ProcessCollidersForResources(Collider[] colliders)
    {
        HashSet<Resource> present = new HashSet<Resource>();

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out Resource resource))
            {
                present.Add(resource);

                if (_resources.ContainsKey(resource) == false)
                {
                    _resources.Add(resource, false);
                }
            }
        }

        return present;
    }

    private void ProcessCollidersForUnits(Collider[] colliders)
    {
        _units.Clear();

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out Unit unit))
            {
                _units.Add(unit);
            }
        }
    }
    
    private void PruneResourcesNotPresent(HashSet<Resource> present)
    {
        if (_resources.Count == 0) return;

        List<Resource> toRemove = new List<Resource>();

        foreach (Resource resource in _resources.Keys)
        {
            if (resource == null)
            {
                toRemove.Add(resource);
                continue;
            }

            if (resource.gameObject.activeInHierarchy == false)
            {
                toRemove.Add(resource);
                
                continue;
            }

            if (present.Contains(resource) == false)
            {
                toRemove.Add(resource);
            }
        }

        foreach (Resource resource in toRemove)
        {
            _resources.Remove(resource);
        }
    }
    
    private void CleanDictionary()
    {
        if (_resources.Count == 0) return;

        List<Resource> toRemove = new List<Resource>();

        foreach (var kv in _resources)
        {
            Resource resource = kv.Key;
            
            if (resource == null || resource.gameObject.activeInHierarchy == false)
            {
                toRemove.Add(resource);
            }
        }

        foreach (Resource resource in toRemove)
        {
            _resources.Remove(resource);
        }
    }

    private void CleanUnitsList()
    {
        _units.RemoveWhere(unit => unit == null || !unit.gameObject.activeInHierarchy);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
