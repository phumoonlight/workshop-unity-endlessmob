using UnityEngine;

// Throws pickups on the ground around a point. Coins and gems are magnet
// pickups, so once scattered they fly to the hero by themselves.
public static class Loot
{
    public static void Scatter(MagnetPickup prefab, int count, Vector3 center, float radius)
    {
        if (prefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 position = center + new Vector3(offset.x, 0f, offset.y);
            position.y = prefab.transform.position.y; // each pickup's own resting height
            PickupPool.Spawn(prefab, position);
        }
    }
}
