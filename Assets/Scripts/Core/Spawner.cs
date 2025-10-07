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
    protected Dictionary<T, SpawnPoint> _owners = new();

    public int ActiveCount => _pool != null ? _pool.CountActive : 0;

    protected virtual void Awake()
    {
        _owners = new Dictionary<T, SpawnPoint>();
        
        InitializePool();
    }
    
    protected virtual void OnEnable()
    {
        StartSpawning();
    }
    
    protected virtual void OnDisable()
    {
        StopSpawning();
    }

    protected virtual void OnDestroy()
    {
        StopSpawning();
        
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
        SpawnPoint point = TryGetFreeSpawnPoint();

        if (point == null)
            return false;

        var item = _pool.Get();

        if (item == null)
            return false;

        point.SetOccupied();
        
        _owners[item] = point;

        InitializeItem(item, point.transform.position);
        
        return true;
    }
    
    private void InitializePool()
    {
        _pool = new ObjectPool<T>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: _collectionCheck,
            defaultCapacity: _poolCapacity,
            maxSize: _poolMaxSize
        );
    }
    
    private T CreateItem() => Instantiate(_prefab);

    private void OnGet(T item)
    {
        if (item == null) return;

        item.gameObject.SetActive(true);
        SetupItemEvents(item);
    }

    private void OnRelease(T item)
    {
        if (item == null) return;

        if (_owners.TryGetValue(item, out var sp))
        {
            sp.SetFree();
            _owners.Remove(item);
        }

        CleanupItemEvents(item);
        item.gameObject.SetActive(false);
        item.transform.SetParent(null, true);
    }

    private void OnDestroyItem(T item)
    {
        CleanupItemEvents(item);
        Destroy(item?.gameObject);
    }

    private void StartSpawning()
    {
        if (_coroutine == null)
            _coroutine = StartCoroutine(SpawnCooldown());
    }

    private void StopSpawning()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private SpawnPoint TryGetFreeSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return null;

        var freePoints = _spawnPoints
            .Where(p => p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy && p.IsFree)
            .ToArray();

        if (freePoints.Length == 0)
            return null;

        return freePoints[Random.Range(0, freePoints.Length)];
    }

    protected abstract void InitializeItem(T item, Vector3 position);

    protected abstract void SetupItemEvents(T item);

    protected abstract void CleanupItemEvents(T item);
}