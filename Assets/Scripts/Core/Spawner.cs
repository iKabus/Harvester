using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Spawner<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] protected T _prefab;
    [SerializeField] protected SpawnPoint[] _spawnPoints;
    [SerializeField] protected float _repeatRate = 2f;

    [SerializeField] protected int _poolCapacity = 3;
    [SerializeField] protected int _poolMaxSize = 3;
    [SerializeField] protected bool _collectionCheck = true;

    private Coroutine _coroutine;
    protected ObjectPool<T> _pool;

    public int ActiveCount => _pool != null ? _pool.CountActive : 0;
    public int InactiveCount => _pool != null ? _pool.CountInactive : 0;

    protected virtual void Awake()
    {
        _pool = new ObjectPool<T>(
            createFunc: () => Instantiate(_prefab),
            actionOnGet: item =>
            {
                if (item == null)
                {
                    return;
                }

                item.gameObject.SetActive(true);
                SetupItemEvents(item);
            },
            actionOnRelease: item =>
            {
                if (item == null)
                {
                    return;
                }

                CleanupItemEvents(item);
                item.gameObject.SetActive(false);

                if (item.transform.parent != null)
                {
                    item.transform.SetParent(null, true);
                }
            },
            actionOnDestroy: item =>
            {
                if (item == null)
                {
                    return;
                }

                CleanupItemEvents(item);
                Destroy(item.gameObject);
            },
            collectionCheck: _collectionCheck,
            defaultCapacity: _poolCapacity,
            maxSize: _poolMaxSize
        );
    }

    protected virtual void OnEnable()
    {
        if (_coroutine == null)
        {
            _coroutine = StartCoroutine(SpawnCooldown());
        }
    }

    protected virtual void Start()
    {
        if (_coroutine == null)
        {
            _coroutine = StartCoroutine(SpawnCooldown());
        }
    }

    protected virtual void OnDisable()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _pool?.Dispose();
        _pool = null;
    }

    protected IEnumerator SpawnCooldown()
    {
        var wait = new WaitForSeconds(_repeatRate);

        while (isActiveAndEnabled)
        {
            TrySpawn();

            yield return wait;
        }
    }

    protected virtual bool TrySpawn()
    {
        var spawnPosition = TryGetPosition();

        if (!spawnPosition.HasValue)
            return false;

        var item = _pool.Get();

        if (item == null)
            return false;


        InitializeItem(item, spawnPosition.Value);
        
        return true;
    }

    protected Vector3? TryGetPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return null;

        var freePoints = _spawnPoints
            .Where(p => p != null
                        && p.isActiveAndEnabled
                        && p.gameObject.activeInHierarchy
                        && p.IsFree)
            .ToArray();

        if (freePoints.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, freePoints.Length);

        return freePoints[randomIndex].transform.position;
    }

    protected void ReleaseToPool(T item)
    {
        if (item == null) return;
        if (_pool == null) return;

        if (!item.gameObject.activeInHierarchy)
        {
            return;
        }

        _pool.Release(item);
    }

    protected abstract void InitializeItem(T item, Vector3 position);

    protected abstract void SetupItemEvents(T item);

    protected abstract void CleanupItemEvents(T item);
}