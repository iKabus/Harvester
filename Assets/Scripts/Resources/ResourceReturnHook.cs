using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ResourceReturnHook : MonoBehaviour
{
    private ResourceSpawner _spawner;
    private Resource _resource;
    private bool _isReturned = false;

    public void Init(ResourceSpawner spawner, Resource resource)
    {
        _spawner = spawner;
        _resource = resource;
        _isReturned = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isReturned || _spawner == null || _resource == null)
            return;
        
        if (other.TryGetComponent<Base>(out var baseComponent))
        {
            _isReturned = true;
            _spawner.ReleaseFromBase(_resource);
            
            baseComponent.NotifyDelivered(_resource);
        }
    }
}