using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Press B to open the build menu, pick a building with 1, 2, 3..., then left-click
// anywhere on the ground to place it. A circle shows where it goes: green if it fits,
// red if it overlaps something or is too close to an enemy camp.
public class BuildManager : MonoBehaviour
{
    [Tooltip("Buildings in the menu, in key order: first = [1], second = [2]...")]
    [SerializeField] BuildingData[] buildings;

    [SerializeField] PlayerWallet wallet;
    [SerializeField] Castle castle;

    [Tooltip("A flat disc 1 meter wide; it gets scaled to the building's footprint.")]
    [SerializeField] Renderer ghost;

    [SerializeField] GameObject menuPanel;
    [SerializeField] TMP_Text menuText;
    [SerializeField] TMP_Text prompt; // placement help at the bottom of the screen
    [SerializeField] TMP_Text hint;   // "[B] Build" when the menu is closed

    [SerializeField] Color validColor = new Color(0.3f, 1f, 0.4f, 0.35f);
    [SerializeField] Color invalidColor = new Color(1f, 0.25f, 0.2f, 0.35f);

    [Header("Rules")]
    [Tooltip("Gap kept between buildings, in meters.")]
    [SerializeField] float spacing = 0.5f;

    [Tooltip("Buildings can't go this close to an enemy camp.")]
    [SerializeField] float minDistanceFromCamps = 12f;

    [Tooltip("Buildings must stay within this distance of the map center on X and Z.")]
    [SerializeField] float mapHalfSize = 95f;

    // Other scripts check this so keys like 1 and 2 don't do two things at once.
    public static bool IsActive { get; private set; }

    int selected = -1; // index into buildings, -1 = nothing chosen yet
    Camera cam;
    MaterialPropertyBlock block;

    void Awake()
    {
        cam = Camera.main;
        block = new MaterialPropertyBlock();
        SetActive(false);
    }

    void OnDestroy() => IsActive = false;

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (Time.timeScale == 0f || GameStats.IsGameOver)
        {
            if (IsActive) SetActive(false);
            hint.enabled = false;
            return;
        }

        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            SetActive(!IsActive);
        else if (IsActive && ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                              (mouse != null && mouse.rightButton.wasPressedThisFrame)))
        {
            // First cancel the chosen building, then close the menu.
            if (selected >= 0) selected = -1;
            else SetActive(false);
        }

        hint.enabled = !IsActive;
        hint.text = "<color=#ffd24a>[B]</color> Build";
        if (!IsActive)
            return;

        // Number keys pick a building.
        if (keyboard != null)
            for (int i = 0; i < buildings.Length && i < 9; i++)
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                    selected = i;

        menuText.text = BuildMenuText();

        if (selected < 0 || mouse == null)
        {
            ghost.enabled = false;
            prompt.text = "Press a number to choose a building\n<size=70%>B or Esc to close</size>";
            return;
        }

        // Where the mouse points on the ground (a flat plane at height 0).
        BuildingData building = buildings[selected];
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            return;
        Vector3 point = ray.GetPoint(distance);
        point.y = 0f;

        float size = building.footprintRadius * 2f;
        ghost.enabled = true;
        ghost.transform.position = point + Vector3.up * 0.03f;
        ghost.transform.localScale = new Vector3(size, 0.01f, size);

        string problem = CheckPlacement(point, building.footprintRadius);
        if (problem == null && wallet.Coins < building.cost)
            problem = "Not enough coins";

        block.SetColor("_BaseColor", problem == null ? validColor : invalidColor);
        ghost.SetPropertyBlock(block);

        prompt.text = problem == null
            ? $"Left-click to build a {building.displayName}  (<color=#ffd24a>{building.cost} coins</color>)\n<size=70%>Right-click to cancel</size>"
            : $"<color=#ff7070>{problem}</color>\n<size=70%>Right-click to cancel</size>";

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (problem == null && !overUI && mouse.leftButton.wasPressedThisFrame && wallet.TrySpend(building.cost))
        {
            // Face the castle, so buildings look like they belong to it.
            Vector3 toCastle = castle.transform.position - point;
            toCastle.y = 0f;
            Quaternion rotation = toCastle.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCastle) : Quaternion.identity;
            Instantiate(building.prefab, point, rotation, transform);
        }
    }

    // Returns null if a building of this size fits here, otherwise the reason it doesn't.
    string CheckPlacement(Vector3 point, float radius)
    {
        if (Mathf.Abs(point.x) > mapHalfSize - radius || Mathf.Abs(point.z) > mapHalfSize - radius)
            return "Too close to the edge of the map";

        // Treat every structure (castle, buildings, outposts) as a circle around its collider.
        foreach (Structure s in Structure.All)
        {
            Bounds b = s.Bounds;
            float otherRadius = Mathf.Max(b.extents.x, b.extents.z);
            if (FlatDistance(point, b.center) < radius + otherRadius + spacing)
                return "Overlaps " + s.DisplayName;
        }

        foreach (EnemyCamp camp in FindObjectsByType<EnemyCamp>(FindObjectsSortMode.None))
            if (FlatDistance(point, camp.transform.position) < minDistanceFromCamps + radius)
                return "Too close to an enemy camp";
        foreach (FortifiedCamp camp in FindObjectsByType<FortifiedCamp>(FindObjectsSortMode.None))
            if (FlatDistance(point, camp.transform.position) < camp.Radius + minDistanceFromCamps + radius)
                return "Too close to a fortified camp";
        foreach (OutpostSite site in FindObjectsByType<OutpostSite>(FindObjectsSortMode.None))
            if (FlatDistance(point, site.transform.position) < site.Radius + radius + spacing)
                return "That spot is saved for an outpost";

        return null;
    }

    string BuildMenuText()
    {
        var text = new StringBuilder();
        text.AppendLine("<b>BUILD</b>   <size=80%>(B to close)</size>");
        text.AppendLine();
        for (int i = 0; i < buildings.Length; i++)
        {
            BuildingData b = buildings[i];
            string color = wallet.Coins >= b.cost ? "#ffffff" : "#808080";
            string marker = i == selected ? "<color=#80ff80>></color> " : "  ";
            text.AppendLine($"{marker}<color={color}><color=#ffd24a>[{i + 1}]</color> {b.displayName}  <color=#ffd24a>{b.cost}c</color></color>");
            text.AppendLine($"<size=75%><color=#a0a0a0>      {b.description}</color></size>");
        }
        return text.ToString();
    }

    void SetActive(bool active)
    {
        IsActive = active;
        selected = -1;
        ghost.enabled = false;
        prompt.enabled = active;
        menuPanel.SetActive(active);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
