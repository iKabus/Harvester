using System;
using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _collectionRange = 3f;
    [SerializeField] private float _collectionTime = 1f;
    [SerializeField] private float _baseArrivalDistance = 0.15f;
    [SerializeField] private Vector3 _carryLocalOffset = new Vector3(0f, 0.5f, 0.2f);

    private Resource _targetResource;
    private Transform _resourceTransform;
    
    private Transform _baseTransform;
    
    private Vector3 _startPosition;

    private UnitState _currentState = UnitState.Idle;
    private Coroutine _currentCoroutine;

    private bool _initialized;
    
    public bool IsBusy { get; private set; }

    private void OnDestroy() => StopCurrentCoroutine();

    public void Init(Vector3 position)
    {
        transform.position = position;
        _startPosition = position;
        _initialized = true;
        ReturnToIdleState();
    }

    public bool GoCollectResource(Resource resource, Base targetBase)
    {
        if (resource == null || targetBase == null || IsBusy) return false;

        _targetResource = resource;
        _resourceTransform = resource.transform;
        _baseTransform = targetBase.transform;

        IsBusy = true;
        StopCurrentCoroutine();
        _currentCoroutine = StartCoroutine(ResourceCollectionRoutine());
        return true;
    }

    private IEnumerator ResourceCollectionRoutine()
    {
        ChangeState(UnitState.MoveToResource);

        // 1) идти к ресурсу с таймаутом
        yield return StartCoroutine(MoveToDynamicTarget(
            () => _resourceTransform != null ? _resourceTransform.position : transform.position,
            _collectionRange,
            cancelCondition: () => _resourceTransform == null,
            hardTimeoutSec: 10f
        ));

        if (_resourceTransform == null)
        {
            yield return StartCoroutine(ReturnHomeRoutine()); // уже вернёт в idle
            yield break;
        }

        // 2) сбор
        ChangeState(UnitState.Collecting);
        float timer = 0f;
        while (timer < _collectionTime)
        {
            if (_resourceTransform == null)
            {
                yield return StartCoroutine(ReturnHomeRoutine());
                yield break;
            }

            float distance = Vector3.Distance(transform.position, _resourceTransform.position);
            if (distance > _collectionRange * 1.25f)
            {
                ChangeState(UnitState.MoveToResource);
                yield return StartCoroutine(MoveToDynamicTarget(
                    () => _resourceTransform != null ? _resourceTransform.position : transform.position,
                    _collectionRange,
                    cancelCondition: () => _resourceTransform == null,
                    hardTimeoutSec: 10f
                ));
                ChangeState(UnitState.Collecting);
                timer = 0f;
                continue;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        PickupResource();

        // 3) возврат к базе/дому
        ChangeState(UnitState.ReturningToBase);

        // Если база пропадёт — НЕ дропать в поле, а вернуться домой и там дропнуть.
        bool baseMissing = false;
        yield return StartCoroutine(MoveToDynamicTarget(
            () => _baseTransform != null ? _baseTransform.position : _startPosition,
            _baseArrivalDistance,
            cancelCondition: () => {
                if (_baseTransform == null) { baseMissing = true; return true; }
                return false;
            },
            hardTimeoutSec: 15f
        ));

        if (baseMissing)
        {
            // База пропала: дойдём до дома и дропнем там
            yield return StartCoroutine(MoveToStaticTargetSafe(_startPosition, _baseArrivalDistance));
            DropResourceAtBase(); // или DropResourceAtHome()
            ReturnToIdleState();
            yield break;
        }
        else
        {
            DropResourceAtBase();
            yield return StartCoroutine(ReturnHomeRoutine()); // этот метод сам вернёт в idle
            yield break;
        }
    }

    private void PickupResource()
    {
        if (_resourceTransform == null) return;

        // Обычно при переноске удобнее worldPositionStays:false
        _resourceTransform.SetParent(transform, worldPositionStays: false);
        _resourceTransform.localPosition = _carryLocalOffset;

        if (_resourceTransform.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            // На время переноса лучше отключить коллизии
            rb.detectCollisions = false;
        }

        // Если есть коллайдер — можно сделать isTrigger = true
        if (_resourceTransform.TryGetComponent<Collider>(out var col))
            col.isTrigger = true;
    }

    private void DropResourceAtBase()
    {
        if (_resourceTransform == null) return;

        _resourceTransform.SetParent(null, worldPositionStays: true);

        if (_resourceTransform.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.detectCollisions = true; // вернуть обратно
        }

        if (_resourceTransform.TryGetComponent<Collider>(out var col))
            col.isTrigger = false;

        // TODO: здесь уведомить базу о доставке/инкременте ресурсов
        // targetBase.AddResource(_targetResource);
    }

    private IEnumerator ReturnHomeRoutine()
    {
        ChangeState(UnitState.ReturningToBase);
        yield return StartCoroutine(MoveToStaticTargetSafe(_startPosition, _baseArrivalDistance));
        ReturnToIdleState(); // <-- единственная точка перехода в idle
    }

    private IEnumerator MoveToDynamicTarget(Func<Vector3> targetPositionFunc, float stopDistance,
        Func<bool> cancelCondition = null, float hardTimeoutSec = Mathf.Infinity)
    {
        float stopSqr = stopDistance * stopDistance;
        float t = 0f;

        while (true)
        {
            if (!enabled) yield break; // унификация поведения

            if (cancelCondition != null && cancelCondition())
                yield break;

            Vector3 targetPosition = targetPositionFunc();
            if ((transform.position - targetPosition).sqrMagnitude <= stopSqr)
                yield break;

            // Если есть Rigidbody у юнита — используйте MovePosition
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);

            if ((t += Time.deltaTime) >= hardTimeoutSec)
                yield break;

            yield return null;
        }
    }

    private IEnumerator MoveToStaticTargetSafe(Vector3 targetPosition, float stopDistance)
    {
        float stopSqr = stopDistance * stopDistance;
        while (enabled && (transform.position - targetPosition).sqrMagnitude > stopSqr)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void ReturnToIdleState()
    {
        IsBusy = false;
        ChangeState(UnitState.Idle);

        _targetResource = null;
        _resourceTransform = null;
        _baseTransform = null;

        StopCurrentCoroutine();
    }

    private void StopCurrentCoroutine()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
    }

    private void ChangeState(UnitState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        // TODO: OnStateChanged?.Invoke(newState);
    }

    private void OnDrawGizmos()
    {
        if (!_initialized) return;

        switch (_currentState)
        {
            case UnitState.MoveToResource:
                Gizmos.color = Color.yellow;
                if (_resourceTransform != null)
                    Gizmos.DrawLine(transform.position, _resourceTransform.position);
                break;

            case UnitState.ReturningToBase:
                Gizmos.color = Color.green;
                if (_baseTransform != null)
                    Gizmos.DrawLine(transform.position, _baseTransform.position);
                else
                    Gizmos.DrawLine(transform.position, _startPosition);
                break;
        }
    }
}
