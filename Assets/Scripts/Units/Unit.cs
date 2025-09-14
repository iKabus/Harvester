using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _collectionRange;
    [SerializeField] private float _collectionTime;

    private Resource _targetResource;
    private Vector3 _startPosition;
    private Vector3 _basePosition;

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
    }

    public void GoCollectResource(Resource resource, Base targetBase)
    {
        _targetResource = resource;
        _basePosition = targetBase.transform.position;
        IsBusy = true;

        StopCurrentCoroutine();
        _currentCoroutine = StartCoroutine(ResourceCollectionRoutine());
    }

    private IEnumerator ResourceCollectionRoutine()
    {
        var wait = new WaitForSeconds(_collectionTime);

        ChangeState(UnitState.MoveToResource);
        yield return StartCoroutine(MoveToPositionRoutine(_targetResource.transform.position, _collectionRange));

        if (_targetResource == null)
        {
            CancelTask();
            yield break;
        }

        ChangeState(UnitState.Collecting);
        yield return wait;

        if (_targetResource == null)
        {
            CancelTask();
            yield break;
        }

        ChangeState(UnitState.ReturningToBase);
        yield return StartCoroutine(MoveToPositionRoutine(_basePosition, 0.1f));

        DeliverResource();
    }

    private IEnumerator MoveToPositionRoutine(Vector3 targetPosition, float stopDistance)
    {
        while ((transform.position - targetPosition).magnitude > stopDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                _moveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    private void DeliverResource()
    {
        if (_targetResource != null)
        {
            Destroy(_targetResource.gameObject);
        }

        ReturnToIdleState();
    }

    private void ReturnToIdleState()
    {
        IsBusy = false;
        ChangeState(UnitState.Idle);
        _targetResource = null;

        StopCurrentCoroutine();
    }

    private void CancelTask()
    {
        StopCurrentCoroutine();
        ChangeState(UnitState.ReturningToBase);

        _currentCoroutine = StartCoroutine(CancelAndReturnRoutine());
    }

    private IEnumerator CancelAndReturnRoutine()
    {
        yield return StartCoroutine(MoveToPositionRoutine(_startPosition, 0.1f));
        ReturnToIdleState();
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
                if (_targetResource != null)
                    Gizmos.DrawLine(transform.position, _targetResource.transform.position);
                break;
            case UnitState.ReturningToBase:
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _basePosition);
                break;
        }
    }
}
