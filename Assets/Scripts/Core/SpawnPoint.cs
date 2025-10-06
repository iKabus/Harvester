using UnityEngine;

public abstract class SpawnPoint : MonoBehaviour
{
    [SerializeField] private bool _isFree = true;
    
    public bool IsFree => _isFree;

    public void SetOccupied() => _isFree = false;
    
    public void SetFree() => _isFree = true;
}
