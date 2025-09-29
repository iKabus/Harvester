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

    public event Action ResourceDelivered;

    public int ResourceCount { private set; get; } = 0;

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

    public void NotifyDelivered()
    {
        ResourceCount++;

        ResourceDelivered?.Invoke();
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

        foreach (Unit unit in GetFreeUnitsOrderedByProximity(allUnits).Take(_maxAssignmentsPerTick))
        {
            if (TryAssignUnitToResource(unit) == false)
                break;

            assigned++;
        }
    }

    private IEnumerable<Unit> GetFreeUnitsOrderedByProximity(IReadOnlyCollection<Unit> allUnits)
    {
        Vector3 basePosition = transform.position;

        float maxSqrDist = _unitMaxDistanceFromBase * _unitMaxDistanceFromBase;

        return allUnits
            .Where(unit => IsUnitEligible(unit, basePosition, maxSqrDist))
            .OrderBy(unit =>
            {
                Vector3 offset = unit.transform.position - basePosition;
                return offset.sqrMagnitude;
            });
    }

    private bool IsUnitEligible(Unit unit, Vector3 basePosition, float maxSqrDist)
    {
        if (unit == null || !unit.isActiveAndEnabled || unit.IsBusy)
            return false;

        if (_allowFarUnits) return true;

        Vector3 offset = unit.transform.position - basePosition;

        return offset.sqrMagnitude <= maxSqrDist;
    }

    private bool TryAssignUnitToResource(Unit unit)
    {
        Resource resource = GetNextAvailableResource();

        if (resource == null) return false;

        return unit.GoCollectResource(resource, this);
    }

    private Resource GetNextAvailableResource()
    {
        if (_scanner == null) return null;

        Resource resource = _scanner.GetResource();

        if (resource == null) return null;

        return resource.gameObject.activeInHierarchy ? resource : null;
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