using UnityEngine;

public class CarryHandler
{
    public void SetCarried(Transform target, bool carry, Transform parent, Vector3 localOffset)
    {
        if (target == null) return;

        if (carry)
        {
            target.SetParent(parent, worldPositionStays: false);
            target.localPosition = localOffset;

            if (target.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            if (target.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }
        }
        else
        {
            target.SetParent(null, worldPositionStays: true);

            if (target.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }

            if (target.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = false;
            }
        }
    }
}