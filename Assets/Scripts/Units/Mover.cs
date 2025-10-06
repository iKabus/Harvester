using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Mover : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float _retargetInterval = 0.15f;
    private float _maxDistance = 2f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void Configure(float retargetInterval)
    {
        _retargetInterval = Mathf.Max(0.01f, retargetInterval);
    }

    public IEnumerator MoveUntilReached(
        float stopDistance,
        float timeout,
        Func<Vector3> getTargetPos,
        Func<bool> onCancelCheck = null)
    {
        float time = 0f;
        float retarget = 0f;

        EnsureAgentOnNav();

        if (_agent == null) yield break;

        _agent.stoppingDistance = stopDistance;
        _agent.isStopped = false;

        while (enabled)
        {
            if (onCancelCheck != null && onCancelCheck()) break;

            if (retarget <= 0f)
            {
                SafeSetDestination(getTargetPos());
                retarget = _retargetInterval;
            }

            if (_agent.pathPending == false && _agent.remainingDistance <= Mathf.Max(stopDistance, _agent.radius))
                break;

            time += Time.deltaTime;
            retarget -= Time.deltaTime;

            if (time >= timeout) break;

            yield return null;
        }

        _agent.isStopped = true;
    }

    private void EnsureAgentOnNav()
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();

        if (_agent == null) return;

        if (_agent.isOnNavMesh == false)
        {
            if (NavMeshUtil.TrySnapToNavMesh(transform.position, _maxDistance, out var snapped))
                transform.position = snapped;
        }
    }

    private void SafeSetDestination(Vector3 worldPos)
    {
        if (_agent == null) return;

        if (NavMeshUtil.TrySnapToNavMesh(worldPos, _maxDistance, out var snapped))
        {
            _agent.SetDestination(snapped);
        }
        else
        {
            _agent.SetDestination(worldPos);
        }
    }
}