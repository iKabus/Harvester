using UnityEngine;

public sealed class ResourceReturnHook : MonoBehaviour
{
    private ResourceSpawner _spawner;
    private Resource _resource;

    public void Init(ResourceSpawner spawner, Resource resource)
    {
        _spawner = spawner;
        _resource = resource;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_spawner != null && _resource != null && other.TryGetComponent<Base>(out _))
        {
            _spawner.ReleaseFromBase(_resource);
        }
    }
}