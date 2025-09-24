using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Base : MonoBehaviour
{
    [SerializeField] private Scanner _scanner;
    [SerializeField] private float _assignInterval = 0.5f;
    [SerializeField] private int _maxAssignmentsPerTick = 3;
    [SerializeField] private bool _allowFarUnits = true;
    [SerializeField] private float _unitMaxDistanceFromBase = 60f;
    
    public event Action<Resource> ResourceDelivered; 

    private Coroutine _assignLoop;

    private void OnEnable()
    {
        if (_assignLoop == null)
            _assignLoop = StartCoroutine(AssignLoop());
    }
    
    private void OnDisable()
    {
        if (_assignLoop != null)
        {
            StopCoroutine(_assignLoop);
            _assignLoop = null;
        }
    }

    public void NotifyDelivered(Resource resource)
    {
        ResourceDelivered?.Invoke(resource);
    }

    private IEnumerator AssignLoop()
    {
        var wait = new WaitForSeconds(_assignInterval);

        while (enabled && _scanner != null)
        {
            TryAssignTasks();
            
            yield return wait;
        }
    }

    private void TryAssignTasks()
    {
        int assigned = 0;
        
        if (_scanner == null) return;

        IReadOnlyCollection<Unit> allUnits = _scanner.GetUnits();
        
        if (allUnits == null || allUnits.Count == 0) return;

        IEnumerable<Unit> freeUnits = allUnits.Where(unit =>
            unit != null &&
            unit.isActiveAndEnabled &&
            !unit.IsBusy &&
            (_allowFarUnits || Vector3.SqrMagnitude(unit.transform.position - transform.position) <= _unitMaxDistanceFromBase)
        );

        freeUnits = freeUnits
            .OrderBy(unit => Vector3.SqrMagnitude(unit.transform.position - transform.position));

        
        foreach (var unit in freeUnits)
        {
            if (assigned >= _maxAssignmentsPerTick)
                break;

            var resource = _scanner.GetResource();
            
            if (resource == null || !resource.gameObject.activeInHierarchy)
                break;

            bool started = unit.GoCollectResource(resource, this);
            
            if (started)
            {
                assigned++;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_allowFarUnits)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _unitMaxDistanceFromBase);
        }
    }
}
