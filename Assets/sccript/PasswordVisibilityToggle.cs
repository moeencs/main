using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PasswordVisibilityToggle : MonoBehaviour
{
    [Header("Wiring")]
    public TMP_InputField input;     // your TMP input
    public Button toggleButton;      // right-side button
    public Image toggleIcon;         // image under the button
    public Sprite eyeOpen;           // visible icon
    public Sprite eyeClosed;         // hidden icon

    [Header("Behavior")]
    public bool startHidden = true;  // default: masked

    private bool _hidden;

    void Reset()
    {
        // Auto-find common children if this is placed on the container
        if (input == null) input = GetComponentInChildren<TMP_InputField>(true);
        if (toggleButton == null) toggleButton = GetComponentInChildren<Button>(true);
        if (toggleIcon == null && toggleButton != null) toggleIcon = toggleButton.GetComponentInChildren<Image>(true);
    }

    void Awake()
    {
        if (input == null || toggleButton == null)
        {
            Debug.LogError("[PasswordVisibilityToggle] Please assign Input and ToggleButton.");
            enabled = false; return;
        }

        _hidden = startHidden;
        ApplyMode(force: true);

        toggleButton.onClick.AddListener(Toggle);
    }

    void OnDestroy()
    {
        if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
    }

    private void Toggle()
    {
        _hidden = !_hidden;
        ApplyMode(force: false);
    }

    private void ApplyMode(bool force)
    {
        // Preserve caret & selection to avoid jumps
        int caret = input.caretPosition;
        int selA = input.selectionAnchorPosition;
        int selF = input.selectionFocusPosition;
        bool wasFocused = input.isFocused;

        var desired = _hidden ? TMP_InputField.ContentType.Password
                              : TMP_InputField.ContentType.Standard;

        if (force || input.contentType != desired)
        {
            input.contentType = desired;

            // Re-apply text without firing value-changed events
            string current = input.text;
            input.SetTextWithoutNotify(current);

            // Update icon
            if (toggleIcon != null)
                toggleIcon.sprite = _hidden ? eyeClosed : eyeOpen;

            // Keep mobile keyboard sane
            input.keyboardType = TouchScreenKeyboardType.Default;

            // Refresh & restore focus/caret
            input.ForceLabelUpdate();
            if (wasFocused) input.ActivateInputField();

            input.selectionAnchorPosition = selA;
            input.selectionFocusPosition  = selF;
            input.caretPosition           = caret;
        }
    }
}
