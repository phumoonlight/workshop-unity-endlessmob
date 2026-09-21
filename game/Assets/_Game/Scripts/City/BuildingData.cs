using UnityEngine;

// Describes one kind of building the player can build.
// Create more with: right-click in the Project window > Create > Endless Mob > Building
[CreateAssetMenu(fileName = "Building", menuName = "Endless Mob/Building")]
public class BuildingData : ScriptableObject
{
    public string displayName = "Building";

    [TextArea(2, 3)]
    public string description = "What it does.";

    public int cost = 30;

    [Tooltip("Radius of the circle the building needs on the ground. Used to stop overlaps.")]
    public float footprintRadius = 1.8f;

    [Tooltip("The building to create. Its pivot should be at ground level.")]
    public GameObject prefab;
}
