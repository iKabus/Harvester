using UnityEngine;

public class ResourceSpawner : Spawner<Resource>
{
    protected override void InitializeItem(Resource resource, Vector3 position)
    {
        ResetTransformAndPhysics(resource);

        resource.Init(position);

        EnableCollider(resource, true);

        AttachReturnHook(resource);
    }

    protected override void SetupItemEvents(Resource resource) { }
    protected override void CleanupItemEvents(Resource resource) { }
    
    public void ReleaseFromBase(Resource resource)
    {
        if (resource == null) return;

        if (!resource.gameObject.activeInHierarchy)
            return;

        EnableCollider(resource, false);

        ResetTransformAndPhysics(resource);

        _pool.Release(resource);
    }
    
    private void AttachReturnHook(Resource resource)
    {
        if (!resource.TryGetComponent<ResourceReturnHook>(out var hook))
        {
            hook = resource.gameObject.AddComponent<ResourceReturnHook>();
        }

        hook.Init(this, resource);
    }

    private void ResetTransformAndPhysics(Resource resource)
    {
        var resourceTransform = resource.transform;

        if (resourceTransform.parent != null)
        {
            resourceTransform.SetParent(null, true);
        }
        if (resource.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void EnableCollider(Resource resource, bool enable)
    {
        if (resource.TryGetComponent<Collider>(out var component))
        {
            component.enabled = enable;
        }
    }
}