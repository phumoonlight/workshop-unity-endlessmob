using UnityEngine;

// Physics layers, in one place.
//
// A layer is a label on a GameObject (top right of the Inspector). A physics
// question like "what is inside this sphere?" can be given a MASK -- a list of
// layers to look at -- and the physics engine then skips everything else
// before our code ever sees it: the ground, the hero, daggers, enemy arrows.
//
// Layers are created in Edit > Project Settings > Tags and Layers. The names
// here must match the names there.
public static class Layers
{
    public const string EnemyName = "Enemy";

    static int enemy = -1;
    static int enemyMask;

    // The layer's NUMBER (0-31). This is what GameObject.layer holds.
    public static int Enemy
    {
        get
        {
            // Looked up on first use, not in a field initializer: Unity APIs
            // are not safe to call that early.
            if (enemy < 0)
            {
                enemy = LayerMask.NameToLayer(EnemyName);
                if (enemy < 0)
                    Debug.LogError($"There is no '{EnemyName}' layer. Add it in Project Settings > Tags and Layers, or no attack will find anything.");
            }
            return enemy;
        }
    }

    // The layer as a MASK, which is what physics queries want. A mask is 32
    // on/off switches in one int, one per layer, so layer 6 is "1 << 6" = 64.
    // Mixing the two up is the classic mistake: passing the number 6 as a mask
    // means "layers 1 and 2" (6 is 110 in binary).
    public static int EnemyMask
    {
        get
        {
            if (enemyMask == 0 && Enemy >= 0)
                enemyMask = 1 << Enemy;
            return enemyMask;
        }
    }
}
