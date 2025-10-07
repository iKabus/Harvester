using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Mover))]
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
    private Mover _mover;
    private CarryHandler _carry;
    private Collector _collector;

    private Vector3 _home;
    private bool _initialized;

    private Transform _resourceT;
    private Transform _baseT;

    private Coroutine _runner;
    private bool _abortCycle;

    public bool IsBusy { get; private set; }

    private UnitState _state = UnitState.Idle;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _mover = GetComponent<Mover>();
        _carry = new CarryHandler();
        _collector = new Collector();
    }

    public void Init(Vector3 position)
    {
        float maxDistance = 2f;

        transform.position = NavMeshUtil.TrySnapToNavMesh(position, maxDistance, out var snapped) ? snapped : position;

        _agent.speed = _moveSpeed;
        _agent.acceleration = _moveSpeed;
        _agent.angularSpeed = 720f;
        _agent.autoBraking = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.stoppingDistance = _baseArrivalDistance;
        _agent.isStopped = true;

        _mover.Configure(_retargetInterval);

        _home = transform.position;
        _initialized = true;

        ResetUnit();
    }

    public bool GoCollectResource(Resource resource, Base targetBase)
    {
        if (resource == null || targetBase == null || IsBusy)
            return false;

        _resourceT = resource.transform;
        _baseT = targetBase.transform;
        IsBusy = true;

        StopRunner();
        
        _runner = StartCoroutine(RunCollectionCycle());
        
        return true;
    }

    private IEnumerator RunCollectionCycle()
    {
        _abortCycle = false;

        yield return MoveToResource();
        
        if (_abortCycle) yield break;

        yield return CollectResource();
        
        if (_abortCycle) yield break;

        yield return ReturnToBase();
        
        if (_abortCycle) yield break;

        FinalizeCollection();
    }

    private IEnumerator MoveToResource()
    {
        bool canceled = false;
        
        _state = UnitState.MoveToResource;

        yield return _mover.MoveUntilReached(
            _collectionRange,
            _moveTimeoutToResource,
            () => _resourceT ? _resourceT.position : transform.position,
            () => _resourceT == null ? (canceled = true) : false
        );

        if (canceled)
        {
            yield return ReturnHome();
            
            _abortCycle = true;
        }
    }

    private IEnumerator CollectResource()
    {
        float buffer = _collectionRange * 1.25f;
        
        if (_resourceT == null)
        {
            yield return ReturnHome();
         
            _abortCycle = true;
            
            yield break;
        }

        _state = UnitState.Collecting;
        
        yield return _collector.CollectRoutine(
            _collectionTime,
            () => _resourceT == null,
            () => _resourceT != null &&
                  (transform.position - _resourceT.position).sqrMagnitude > buffer * buffer,
            () => _mover.MoveUntilReached(
                _collectionRange,
                _moveTimeoutToResource,
                () => _resourceT ? _resourceT.position : transform.position,
                () => _resourceT == null
            )
        );

        if (_resourceT == null)
        {
            yield return ReturnHome();
            
            _abortCycle = true;
            
            yield break;
        }

        _carry.SetCarried(_resourceT, true, transform, _carryLocalOffset);
    }

    private IEnumerator ReturnToBase()
    {
        bool baseMissing = false;
        
        _state = UnitState.Returning;

        yield return _mover.MoveUntilReached(
            _baseArrivalDistance,
            _moveTimeoutToBase,
            () => _baseT ? _baseT.position : _home,
            () => _baseT == null ? (baseMissing = true) : false
        );

        if (baseMissing && (transform.position - _home).sqrMagnitude > _baseArrivalDistance * _baseArrivalDistance)
        {
            yield return _mover.MoveUntilReached(_baseArrivalDistance, _moveTimeoutToBase, () => _home);
        }

        _carry.SetCarried(_resourceT, false, transform, _carryLocalOffset);

        if ((transform.position - _home).sqrMagnitude > _baseArrivalDistance * _baseArrivalDistance)
        {
            yield return ReturnHome();
            _abortCycle = true;
        }
    }

    private IEnumerator ReturnHome()
    {
        _state = UnitState.Returning;
        yield return _mover.MoveUntilReached(_baseArrivalDistance, _moveTimeoutToBase, () => _home);
        ResetUnit();
    }

    private void FinalizeCollection()
    {
        ResetUnit();
    }

    private void ResetUnit()
    {
        IsBusy = false;
        _state = UnitState.Idle;
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

    private void OnDrawGizmos()
    {
        if (!_initialized) return;

        if (_state == UnitState.MoveToResource && _resourceT)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _resourceT.position);
        }
        else if (_state == UnitState.Returning)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _baseT ? _baseT.position : _home);
        }
    }
}
