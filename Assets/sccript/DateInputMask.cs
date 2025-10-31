using System;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class DateInputMask : MonoBehaviour
{
    [Header("Optional UI")]
    [Tooltip("Inline error text (optional).")]
    public TMP_Text errorText;

    [Header("Settings")]
    [Tooltip("Toggle range validation incl. leap years.")]
    public bool validateDate = true;

    private TMP_InputField _field;
    private bool _internalUpdate;

    // Defer caret so TMP can't override us
    private bool _deferCaret;
    private int  _deferCaretMasked;

    // Canonical previous state
    private string _prevDigits = string.Empty;   // e.g., "10312025"
    private string _prevMasked = string.Empty;   // e.g., "10/31/2025"

    private void Awake()
    {
        _field = GetComponent<TMP_InputField>();

        // Solid UX defaults
        _field.lineType    = TMP_InputField.LineType.SingleLine;
        _field.contentType = TMP_InputField.ContentType.IntegerNumber; // mobile numeric keyboard
        if (_field.characterLimit < 10) _field.characterLimit = 10;    // "MM/DD/YYYY"

        // Only digits at keystroke (paste sanitized later)
        _field.onValidateInput += OnValidateChar;

        // Core formatter
        _field.onValueChanged.AddListener(HandleValueChanged);

        // Initialize empty
        ApplyDigitsAndMask("", desiredDigitCaret: 0);
        ShowError(null);
    }

    private void OnDestroy()
    {
        if (_field == null) return;
        _field.onValidateInput       -= OnValidateChar;
        _field.onValueChanged.RemoveListener(HandleValueChanged);
    }

    // --- Keystroke gate: allow digits only; cap theoretical digits at 8 ---
    private char OnValidateChar(string currentText, int charIndex, char addedChar)
    {
        if (addedChar >= '0' && addedChar <= '9')
        {
            if (CountDigits(currentText) >= 8) return '\0';
            return addedChar;
        }
        return '\0';
    }

    // --- Core: run after TMP applies any edit (type/backspace/delete/paste/replace/click) ---
    private void HandleValueChanged(string _)
    {
        if (_internalUpdate) return;

        // 1) Read TMP’s current raw masked text & caret/selection (this reflects the user’s edit)
        string rawMaskedNow = _field.text;
        int caretMaskedNow  = _field.caretPosition;

        // 2) Sanitize to digits (paste like "10/31-2025" → "10312025", capped to 8)
        string newDigits = DigitsOnly(rawMaskedNow, max: 8);

        // 3) Compute the **intended digit-caret** by counting digits before TMP’s current caret
        //    This is the key to avoid “double caret hops”.
        int intendedDigitCaret = CountDigitsUpToMasked(rawMaskedNow, caretMaskedNow);
        intendedDigitCaret = Mathf.Clamp(intendedDigitCaret, 0, newDigits.Length);

        // 4) Apply canonical masked form and remap caret
        ApplyDigitsAndMask(newDigits, intendedDigitCaret);

        // 5) Optional validation feedback
        if (validateDate && newDigits.Length == 8)
        {
            if (!IsValidDigits(newDigits, out string msg)) ShowError(msg);
            else ShowError(null);
        }
        else ShowError(null);
    }

    private void LateUpdate()
    {
        if (!_deferCaret) return;

        if (_field != null && _field.isActiveAndEnabled)
        {
            int pos = Mathf.Clamp(_deferCaretMasked, 0, _field.text.Length);
            _field.caretPosition = pos;
            _field.selectionAnchorPosition = pos;
            _field.selectionFocusPosition = pos;

            if (!_field.isFocused) _field.ActivateInputField();
            _field.ForceLabelUpdate();
        }

        _deferCaret = false;
    }

    // --- Apply canonical mask & schedule caret ---
    private void ApplyDigitsAndMask(string digits, int desiredDigitCaret)
    {
        _internalUpdate = true;

        string masked = BuildMaskedFromDigits(digits);

        // Write without re-entrant events
        _field.SetTextWithoutNotify(masked);
        _field.ForceLabelUpdate();

        // Convert digit-caret -> masked caret in "MM/DD/YYYY"
        int maskedCaret = MapDigitIndexToMaskedCaret(desiredDigitCaret);
        _deferCaretMasked = Mathf.Clamp(maskedCaret, 0, masked.Length);
        _deferCaret = true;

        // Persist canonical state
        _prevDigits = digits;
        _prevMasked = masked;

        _internalUpdate = false;
    }

    // --- Public API ---
    public bool IsComplete => _prevDigits.Length == 8;

    public bool TryGetDate(out DateTime date)
    {
        date = default;
        if (_prevDigits.Length != 8) return false;
        if (!TryParseDigits(_prevDigits, out int mm, out int dd, out int yyyy)) return false;
        try { date = new DateTime(yyyy, mm, dd); return true; }
        catch { return false; }
    }

    public string GetISO() => TryGetDate(out var dt) ? dt.ToString("yyyy-MM-dd") : null;

    // --- Helpers ---
    private static string BuildMaskedFromDigits(string digits)
    {
        var sb = new StringBuilder(10);
        for (int i = 0; i < digits.Length; i++)
        {
            sb.Append(digits[i]);
            if (i == 1 || i == 3) sb.Append('/'); // after 2nd and 4th digit
        }
        return sb.ToString();
    }

    private static string DigitsOnly(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(max);
        for (int i = 0; i < s.Length && sb.Length < max; i++)
        {
            char c = s[i];
            if (c >= '0' && c <= '9') sb.Append(c);
        }
        return sb.ToString();
    }

    private static int CountDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int c = 0;
        for (int i = 0; i < s.Length; i++)
            if (s[i] >= '0' && s[i] <= '9') c++;
        return c;
    }

    // Count how many digits appear in MASKED string BEFORE maskedIndex
    private static int CountDigitsUpToMasked(string masked, int maskedIndex)
    {
        if (string.IsNullOrEmpty(masked) || maskedIndex <= 0) return 0;
        int limit = Mathf.Clamp(maskedIndex, 0, masked.Length);
        int count = 0;
        for (int i = 0; i < limit; i++)
        {
            char ch = masked[i];
            if (ch >= '0' && ch <= '9') count++;
        }
        return count;
    }

    // Map a digit index (0..N) to caret index in "MM/DD/YYYY"
    // 0->0, 1->1, 2->3, 3->4, 4->6, 5->7, 6->8, 7->9, 8->10
    private static int MapDigitIndexToMaskedCaret(int digitIndex)
    {
        if (digitIndex <= 0) return 0;
        if (digitIndex == 1) return 1;
        if (digitIndex == 2) return 3;
        if (digitIndex == 3) return 4;
        if (digitIndex == 4) return 6;
        if (digitIndex == 5) return 7;
        if (digitIndex == 6) return 8;
        if (digitIndex == 7) return 9;
        if (digitIndex >= 8) return 10;
        return 0;
    }

    private static bool TryParseDigits(string digits, out int mm, out int dd, out int yyyy)
    {
        mm = dd = yyyy = 0;
        if (digits.Length != 8) return false;
        mm   = int.Parse(digits.Substring(0, 2));
        dd   = int.Parse(digits.Substring(2, 2));
        yyyy = int.Parse(digits.Substring(4, 4));
        return true;
    }

    private bool IsValidDigits(string digits, out string message)
    {
        message = null;
        if (!TryParseDigits(digits, out int mm, out int dd, out int yyyy))
        { message = "Enter 8 digits."; return false; }

        if (mm < 1 || mm > 12)
        { message = "Month must be 01–12."; return false; }

        int[] daysInMonth = { 31, IsLeap(yyyy) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        int maxDay = daysInMonth[mm - 1];
        if (dd < 1 || dd > maxDay)
        { message = $"Day must be 01–{maxDay}."; return false; }

        if (yyyy < 1900 || yyyy > 2100)
        { message = "Year must be 1900–2100."; return false; }

        return true;
    }

    private static bool IsLeap(int y) =>
        (y % 400 == 0) || (y % 4 == 0 && y % 100 != 0);

    private void ShowError(string msg)
    {
        if (!errorText) return;
        bool show = !string.IsNullOrEmpty(msg);
        if (errorText.gameObject.activeSelf != show)
            errorText.gameObject.SetActive(show);
        errorText.text = msg ?? "";
    }
}
