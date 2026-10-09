using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Colours only the menu item that is under the mouse, so at most one is ever highlighted.
public class MenuHighlight : MonoBehaviour
{
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.82f, 0.25f);

    private Text[] items;

    void Awake()
    {
        items = GetComponentsInChildren<Text>(true);
    }

    void Update()
    {
        bool found = false;
        foreach (Text item in items)
        {
            bool hovered = !found && TryGetMousePosition(out Vector2 mouse)
                && RectTransformUtility.RectangleContainsScreenPoint(item.rectTransform, mouse, null);
            item.color = hovered ? highlightColor : normalColor;
            found |= hovered;
        }
    }

    private bool TryGetMousePosition(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        return Mouse.current != null;
#else
        position = Input.mousePosition;
        return Input.mousePresent;
#endif
    }
}
