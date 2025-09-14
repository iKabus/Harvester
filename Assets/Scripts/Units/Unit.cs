using System;
using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _collectionRange;
    [SerializeField] private float _collectionTime;
    [SerializeField] private float _baseArrivalDistance = 0.15f;
    [SerializeField] private Vector3 _carryLocalOffset = new Vector3(0f, 0.5f, 0.2f);

    private Resource _targetResource;
    private Transform _resourceTransform;
    
    private Transform _baseTransform;
    
    private Vector3 _startPosition;

    private UnitState _currentState = UnitState.Idle;
    private Coroutine _currentCoroutine;

    public bool IsBusy { get; private set; }

    private void OnDestroy()
    {
        StopCurrentCoroutine();
    }

    public void Init(Vector3 position)
    {
        transform.position = position;
        _startPosition = transform.position;
        ReturnToIdleState();
    }

    public bool GoCollectResource(Resource resource, Base targetBase)
    {
        if (resource == null || targetBase == null || IsBusy)
        {
            return false;
        }

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
        var wait = new WaitForSeconds(_collectionTime);

        ChangeState(UnitState.MoveToResource);
        
        yield return StartCoroutine(MoveToDynamicTarget(
            () => _resourceTransform != null ? _resourceTransform.position : transform.position,
            _collectionRange,
            () => _resourceTransform == null));
        
        if (_resourceTransform == null)
        {
            yield return StartCoroutine(ReturnHomeRoutine());
            yield break;
        }

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
                    () => _resourceTransform == null));
                
                ChangeState(UnitState.Collecting);
                
                timer = 0f;
                
                continue;
            }


            timer += Time.deltaTime;
            
            yield return null;
        }

        PickupResource();
        
        ChangeState(UnitState.ReturningToBase);
        
        yield return StartCoroutine(MoveToDynamicTarget(
            () => _baseTransform != null ? _baseTransform.position : _startPosition,
            _baseArrivalDistance,
            () => _baseTransform == null));
        
        DropResourceAtBase();

        ReturnToIdleState();
    }
    
    private void PickupResource()
    {
        if (_resourceTransform == null)
        {
            return;
        }

        _resourceTransform.SetParent(transform, worldPositionStays: true);
        _resourceTransform.localPosition = _carryLocalOffset;


        if (_resourceTransform.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
        }
    }

    private void DropResourceAtBase()
    {
        if (_resourceTransform == null)
        {
            return;
        }
        
        _resourceTransform.SetParent(null, worldPositionStays: true);

        if (_resourceTransform.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
        }
    }
    
    private IEnumerator ReturnHomeRoutine()
    {
        ChangeState(UnitState.ReturningToBase);
        
        yield return StartCoroutine(MoveToStaticTarget(_startPosition, _baseArrivalDistance));
        
        ReturnToIdleState();
    }
    
    private IEnumerator MoveToDynamicTarget(Func<Vector3> targetPositionFunc, float stopDistance, Func<bool> cancelCondition = null)
    {
        float stopSqr = stopDistance * stopDistance;
        
        while (enabled)
        {
            if (cancelCondition != null && cancelCondition())
            {
                yield break;
            }
            
            Vector3 targetPosition = targetPositionFunc();

            if ((transform.position - targetPosition).sqrMagnitude <= stopSqr)
            {
                yield break;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                _moveSpeed * Time.deltaTime);


            yield return null;
        }
    }
    
    private IEnumerator MoveToStaticTarget(Vector3 targetPosition, float stopDistance)
    {
        float stopSqr = stopDistance * stopDistance;
        
        while ((transform.position - targetPosition).sqrMagnitude > stopSqr)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                _moveSpeed * Time.deltaTime);
            
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
        _currentState = newState;
    }
    
    private void OnDrawGizmos()
    {
        switch (_currentState)
        {
            case UnitState.MoveToResource:
                Gizmos.color = Color.yellow;
                
                if (_resourceTransform != null)
                {
                    Gizmos.DrawLine(transform.position, _resourceTransform.position);
                }

                break;
            
            case UnitState.ReturningToBase:
                Gizmos.color = Color.green;
                
                if (_baseTransform != null)
                {
                    Gizmos.DrawLine(transform.position, _baseTransform.position);
                }
                else
                {
                    Gizmos.DrawLine(transform.position, _startPosition);
                }   
                
                break;
        }
    }
}
