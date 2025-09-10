using UnityEngine;

public abstract class SpawnPoint : MonoBehaviour
{
    public bool IsFree { get; protected set; } = true;
    
    protected virtual void HandleTriggerEnter(Collider other)
    {
        IsFree = false;
    }
    
    protected virtual void HandleTriggerExit(Collider other)
    {
        IsFree = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (ShouldProcessCollision(other))
        {
            HandleTriggerEnter(other);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (ShouldProcessCollision(other))
        {
            HandleTriggerExit(other);
        }
    }
    
    protected abstract bool ShouldProcessCollision(Collider other);
}
