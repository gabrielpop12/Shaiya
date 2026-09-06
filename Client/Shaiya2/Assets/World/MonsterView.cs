using UnityEngine;

public sealed class MonsterView
    : MonoBehaviour
{
    public long EntityId { get; private set; }

    public void Initialize(
        long entityId)
    {
        EntityId =
            entityId;
    }
}