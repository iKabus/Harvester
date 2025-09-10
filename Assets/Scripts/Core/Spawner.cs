using System.Collections;
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

    private Coroutine _coroutine;
    protected ObjectPool<T> _pool;

    protected virtual void Awake()
    {
        _pool = new ObjectPool<T>(
            createFunc: () => Instantiate(_prefab),
            actionOnGet: item => item.gameObject.SetActive(true),
            actionOnRelease: item => item.gameObject.SetActive(false),
            actionOnDestroy: item => Destroy(item.gameObject),
            collectionCheck: true,
            defaultCapacity: _poolCapacity,
            maxSize: _poolMaxSize);
    }

    protected virtual void Start()
    {
        _coroutine = StartCoroutine(SpawnCooldown());
    }

    protected virtual void OnDestroy()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
    }

    protected IEnumerator SpawnCooldown()
    {
        var wait = new WaitForSeconds(_repeatRate);

        while (enabled)
        {
            TrySpawn();
            
            yield return wait;
        }
    }

    protected virtual void TrySpawn()
    {
        Vector3? spawnPosition = TryGetPosition();

        if (spawnPosition.HasValue)
        {
            T item = _pool.Get();
            InitializeItem(item, spawnPosition.Value);
        }
    }

    protected Vector3? TryGetPosition()
    {
        var freePoints = _spawnPoints.Where(point => point.IsFree).ToArray();
    
        if (freePoints.Length == 0)
        {
            return null;
        }
    
        int randomIndex = Random.Range(0, freePoints.Length);
        return freePoints[randomIndex].transform.position;
    }

    protected abstract void InitializeItem(T item, Vector3 position);
    protected abstract void SetupItemEvents(T item);
    protected abstract void CleanupItemEvents(T item);
}