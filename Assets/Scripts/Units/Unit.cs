using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Unit : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _retargetInterval = 0.15f;
    [SerializeField] private float _moveTimeoutToResource = 10f;
    [SerializeField] private float _moveTimeoutToBase = 15f;
    [SerializeField] private float _baseArrivalDistance = 0.15f;

    [SerializeField] private float _collectionRange = 2f;
    [SerializeField] private float _collectionTime = 1f;
    [SerializeField] private Vector3 _carryLocalOffset = Vector3.zero;

    private NavMeshAgent _agent;
    private Vector3 _home;
    private bool _initialized;

    private Resource _targetResource;
    private Transform _resourceT;
    private Transform _baseT;

    private Coroutine _runner;
    public bool IsBusy { get; private set; }

    private UnitState _state = UnitState.Idle;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void OnValidate()
    {
        float currentDistance = 0.01f;
        
        if (_baseArrivalDistance < currentDistance) _baseArrivalDistance = currentDistance;
        
        if (_agent != null) _agent.speed = _moveSpeed;
    }

    private void OnDisable() => StopRunner();
    private void OnDestroy() => StopRunner();

    public void Init(Vector3 position)
    {
        float maxDistance = 2f;
        
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();

        transform.position = TrySnapToNavMesh(position, maxDistance, out var snapped) ? snapped : position;

        _agent.speed = _moveSpeed;
        _agent.acceleration = _moveSpeed;
        _agent.angularSpeed = 720f;
        _agent.autoBraking = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.stoppingDistance = _baseArrivalDistance;
        _agent.isStopped = true;

        _home = transform.position;
        _initialized = true;

        ResetUnit();
    }

    public bool GoCollectResource(Resource resource, Base targetBase)
    {
        if (resource == null || targetBase == null || IsBusy) return false;

        _targetResource = resource;
        _resourceT = resource.transform;
        _baseT = targetBase.transform;

        IsBusy = true;
        
        StopRunner();
        
        _runner = StartCoroutine(RunCollectionCycle());
        
        return true;
    }

    private IEnumerator RunCollectionCycle()
    {
        float time = 0f;
        
        _state = UnitState.MoveToResource;

        bool canceled = false;
        
        yield return MoveUntilReached(
            getTargetPos: () => _resourceT ? _resourceT.position : transform.position,
            stopDistance: _collectionRange,
            timeout: _moveTimeoutToResource,
            cancel: () =>
            {
                if (_resourceT == null)
                {
                    canceled = true; 
                    
                    return true; 
                }
                
                return false;
            });

        if (canceled)
        {
            yield return ReturnHome();
            
            yield break;
        }

        _state = UnitState.Collecting;
        
        while (time < _collectionTime)
        {
            if (_resourceT == null)
            {
                yield return ReturnHome();
                
                yield break;
            }

            float extra = 1.25f;
            
            float limitSqr = _collectionRange * _collectionRange * extra * extra;
            
            if ((transform.position - _resourceT.position).sqrMagnitude > limitSqr)
            {
                _state = UnitState.MoveToResource;
                
                yield return MoveUntilReached(
                    getTargetPos: () => _resourceT ? _resourceT.position : transform.position,
                    stopDistance: _collectionRange,
                    timeout: _moveTimeoutToResource,
                    cancel: () => _resourceT == null
                );
                
                _state = UnitState.Collecting;
                
                time = 0f;
                
                continue;
            }

            time += Time.deltaTime;
            yield return null;
        }

        SetCarried(true);

        _state = UnitState.Returning;

        bool baseMissing = false;
        
        yield return MoveUntilReached(
            getTargetPos: () => _baseT ? _baseT.position : _home,
            stopDistance: _baseArrivalDistance,
            timeout: _moveTimeoutToBase,
            cancel: () =>
            {
                if (_baseT == null)
                {
                    baseMissing = true;
                    
                    return true;
                }
                
                return false;
            });

        if (baseMissing && (transform.position - _home).sqrMagnitude > _baseArrivalDistance * _baseArrivalDistance)
        {
            yield return MoveUntilReached(
                getTargetPos: () => _home,
                stopDistance: _baseArrivalDistance,
                timeout: _moveTimeoutToBase
            );
        }

        SetCarried(false);

        if ((transform.position - _home).sqrMagnitude > _baseArrivalDistance * _baseArrivalDistance)
        {
            yield return ReturnHome();
        }        
        else
        {
            ResetUnit();
        }    
    }

    private IEnumerator ReturnHome()
    {
        _state = UnitState.Returning;
        
        yield return MoveUntilReached(
            getTargetPos: () => _home,
            stopDistance: _baseArrivalDistance,
            timeout: _moveTimeoutToBase
        );
        
        ResetUnit();
    }
    
    private IEnumerator MoveUntilReached(Func<Vector3> getTargetPos, float stopDistance, float timeout, Func<bool> cancel = null)
    {
        float time = 0f;
        float retarget = 0f;
        
        EnsureAgentOnNav();
        
        if (_agent == null) yield break;

        _agent.stoppingDistance = stopDistance;
        _agent.isStopped = false;


        while (enabled)
        {
            if (cancel != null && cancel()) break;

            if (retarget <= 0f)
            {
                SafeSetDestination(getTargetPos());
                
                retarget = _retargetInterval;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= Mathf.Max(stopDistance, _agent.radius))
                break;

            time += Time.deltaTime;
            retarget -= Time.deltaTime;

            if (time >= timeout) break;
            
            yield return null;
        }

        _agent.isStopped = true;
    }

    private void SetCarried(bool carry)
    {
        if (_resourceT == null) return;

        if (carry)
        {
            _resourceT.SetParent(transform, worldPositionStays: false);
            _resourceT.localPosition = _carryLocalOffset;

            if (_resourceT.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }
            if (_resourceT.TryGetComponent<Collider>(out var collider))
                collider.isTrigger = true;
        }
        else
        {
            _resourceT.SetParent(null, worldPositionStays: true);

            if (_resourceT.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = false;
                rigidbody.detectCollisions = true;
            }
            if (_resourceT.TryGetComponent<Collider>(out var collider))
                collider.isTrigger = false;
        }
    }

    private void ResetUnit()
    {
        IsBusy = false;
        _state = UnitState.Idle;

        _targetResource = null;
        _resourceT = null;
        _baseT = null;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }
    }

    private void StopRunner()
    {
        if (_runner != null)
        {
            StopCoroutine(_runner);
            _runner = null;
        }
    }

    private void EnsureAgentOnNav()
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        
        if (_agent == null) return;

        if (!_agent.isOnNavMesh)
        {
            if (TrySnapToNavMesh(transform.position, 2f, out var snapped))
                transform.position = snapped;
        }
    }

    private bool TrySnapToNavMesh(Vector3 pos, float maxDistance, out Vector3 snapped)
    {
        if (NavMesh.SamplePosition(pos, out var hit, maxDistance, NavMesh.AllAreas))
        {
            snapped = hit.position;
            
            return true;
        }
        snapped = pos;
        
        return false;
    }

    private void SafeSetDestination(Vector3 worldPos)
    {
        if (_agent == null) return;

        if (TrySnapToNavMesh(worldPos, 2f, out var snapped))
        {
            _agent.SetDestination(snapped);
        }        
        else
        {
            _agent.SetDestination(worldPos);
        }    
    }

    private void OnDrawGizmos()
    {
        if (!_initialized) return;

        switch (_state)
        {
            case UnitState.MoveToResource:
                Gizmos.color = Color.yellow;
                
                if (_resourceT) Gizmos.DrawLine(transform.position, _resourceT.position);
                break;
            
            case UnitState.Returning:
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _baseT ? _baseT.position : _home);
                break;
        }
    }
}
