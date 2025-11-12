// File: DateInputMask.cs
using System;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class DateInputMask : MonoBehaviour
{
    [Header("Optional UI")]
    public TMP_Text errorText;

    [Header("Settings")]
    public bool validateDate = true; // enable/disable final validation (month/day/year rules)

    private TMP_InputField _field;
    private bool _internalUpdate;

    private bool _deferCaret;
    private int  _deferCaretMasked;

    private string _prevDigits = string.Empty;
    private string _prevMasked = string.Empty;

    private void Awake()
    {
        _field = GetComponent<TMP_InputField>();
        _field.lineType    = TMP_InputField.LineType.SingleLine;
        _field.contentType = TMP_InputField.ContentType.IntegerNumber;
        if (_field.characterLimit < 10) _field.characterLimit = 10;

        _field.onValidateInput += OnValidateChar;
        _field.onValueChanged.AddListener(HandleValueChanged);

        ApplyDigitsAndMask("", desiredDigitCaret: 0);
        ShowError(null);
    }

    private void OnDestroy()
    {
        if (_field == null) return;
        _field.onValidateInput       -= OnValidateChar;
        _field.onValueChanged.RemoveListener(HandleValueChanged);
    }

    // Validate each typed character (returns char or '\0' to reject).
    // Also disallow creating a 4-digit year in the future.
    private char OnValidateChar(string currentText, int charIndex, char addedChar)
    {
        // Accept only digits
        if (addedChar < '0' || addedChar > '9') return '\0';

        // How many digits are already present
        string existingDigits = DigitsOnly(currentText, 8);
        int curDigitCount = existingDigits.Length;
        if (curDigitCount >= 8) return '\0'; // maximum digits reached

        // Map masked charIndex to digit insertion index
        int insertDigitIndex = CountDigitsUpToMasked(currentText, charIndex);
        insertDigitIndex = Mathf.Clamp(insertDigitIndex, 0, curDigitCount); // allow append

        // Build candidate new digits with the added char inserted
        var sb = new StringBuilder(9);
        for (int i = 0; i < insertDigitIndex; i++) sb.Append(existingDigits[i]);
        sb.Append(addedChar);
        for (int i = insertDigitIndex; i < existingDigits.Length; i++) sb.Append(existingDigits[i]);
        string newDigits = sb.ToString();

        // If full 8 digits would be produced, prevent future year immediately
        if (newDigits.Length >= 8)
        {
            // parse year safely
            try
            {
                int year = int.Parse(newDigits.Substring(4, 4));
                if (year > DateTime.Now.Year) return '\0'; // block future year keystroke
            }
            catch
            {
                return '\0';
            }
        }

        // Quick partial validation rules:

        // --- Month checks (digits 0 and 1) ---
        if (insertDigitIndex == 0 || insertDigitIndex == 1 || newDigits.Length >= 2)
        {
            if (newDigits.Length >= 1)
            {
                char m0 = newDigits[0];
                if (!(m0 == '0' || m0 == '1')) return '\0';
            }

            if (newDigits.Length >= 2)
            {
                char m0 = newDigits[0];
                char m1 = newDigits[1];
                int month = (m0 - '0') * 10 + (m1 - '0');
                if (month < 1 || month > 12) return '\0';
            }
        }

        // --- Day checks (digits 2 and 3) ---
        if (insertDigitIndex == 2 || insertDigitIndex == 3 || newDigits.Length >= 4)
        {
            if (newDigits.Length >= 3)
            {
                char d0 = newDigits[2];
                if (d0 < '0' || d0 > '3') return '\0';
            }

            if (newDigits.Length >= 4)
            {
                int day = (newDigits[2] - '0') * 10 + (newDigits[3] - '0');
                if (day < 1 || day > 31) return '\0';

                // If month is known (two digits available), use month-specific limit
                if (newDigits.Length >= 2)
                {
                    int month = (newDigits[0] - '0') * 10 + (newDigits[1] - '0');
                    if (month >= 1 && month <= 12)
                    {
                        int maxDay = DaysInMonthFromDigits(month, GetYearIfAvailable(newDigits));
                        if (day > maxDay) return '\0';
                    }
                }
            }
        }

        // Year partial typing: allow digits, final check handled above and in IsValidDigits.

        return addedChar;
    }

    private int GetYearIfAvailable(string digits)
    {
        if (digits.Length >= 8)
        {
            int y = int.Parse(digits.Substring(4, 4));
            return y;
        }
        return -1;
    }

    // Improved value-changed handling that avoids blinking
    private void HandleValueChanged(string _)
    {
        if (_internalUpdate) return;

        string rawMaskedNow = _field.text;
        int caretMaskedNow  = _field.caretPosition;

        string newDigits = DigitsOnly(rawMaskedNow, max: 8);

        int intendedDigitCaret = CountDigitsUpToMasked(rawMaskedNow, caretMaskedNow);
        intendedDigitCaret = Mathf.Clamp(intendedDigitCaret, 0, newDigits.Length);

        ApplyDigitsAndMask(newDigits, intendedDigitCaret);

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

            // Only set caret/selection positions — do NOT call ActivateInputField().
            _field.caretPosition = pos;
            _field.selectionAnchorPosition = pos;
            _field.selectionFocusPosition = pos;
        }

        _deferCaret = false;
    }

    private void ApplyDigitsAndMask(string digits, int desiredDigitCaret)
    {
        _internalUpdate = true;

        string masked = BuildMaskedFromDigits(digits);

        // Avoid rewriting text if nothing changed (prevents TMP caret restart/blink).
        if (masked == _prevMasked)
        {
            int maskedCaretSame = MapDigitIndexToMaskedCaret(desiredDigitCaret);
            _deferCaretMasked = Mathf.Clamp(maskedCaretSame, 0, _field.text.Length);
            _deferCaret = true;

            _prevDigits = digits;
            _internalUpdate = false;
            return;
        }

        _field.SetTextWithoutNotify(masked);
        _field.ForceLabelUpdate();

        int maskedCaret = MapDigitIndexToMaskedCaret(desiredDigitCaret);
        _deferCaretMasked = Mathf.Clamp(maskedCaret, 0, masked.Length);
        _deferCaret = true;

        _prevDigits = digits;
        _prevMasked = masked;

        _internalUpdate = false;
    }

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

    private static string BuildMaskedFromDigits(string digits)
    {
        var sb = new StringBuilder(10);
        for (int i = 0; i < digits.Length; i++)
        {
            sb.Append(digits[i]);
            if (i == 1 || i == 3) sb.Append('/');
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

    // Count digits up to a masked index (useful to find digit insertion index)
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

    // Final validation — ensures month/day/year correctness & month-specific max days
    private bool IsValidDigits(string digits, out string message)
    {
        message = null;
        if (!TryParseDigits(digits, out int mm, out int dd, out int yyyy))
        { message = "Enter 8 digits (MMDDYYYY)."; return false; }

        if (mm < 1 || mm > 12)
        { message = "Month must be 01–12."; return false; }

        int maxDay = DaysInMonthFromDigits(mm, yyyy);
        if (dd < 1 || dd > maxDay)
        { message = $"Day must be 01–{maxDay} for selected month."; return false; }

        // Year must be exactly 4 digits
        if (yyyy < 1000 || yyyy > 9999)
        { message = "Year must be 4 digits."; return false; }

        // Disallow future year
        int currentYear = DateTime.Now.Year;
        if (yyyy > currentYear)
        { message = $"Year cannot be in the future ({currentYear})."; return false; }

        // Optional: restrict to a sensible year range
        if (yyyy < 1900 || yyyy > 2100)
        { message = "Year must be between 1900 and 2100."; return false; }

        return true;
    }

    private static int DaysInMonthFromDigits(int month, int year)
    {
        switch (month)
        {
            case 1: return 31;
            case 2: return IsLeap(year) ? 29 : 28;
            case 3: return 31;
            case 4: return 30;
            case 5: return 31;
            case 6: return 30;
            case 7: return 31;
            case 8: return 31;
            case 9: return 30;
            case 10: return 31;
            case 11: return 30;
            case 12: return 31;
            default: return 31;
        }
    }

    private static bool IsLeap(int y) =>
        y >= 0 && ((y % 400 == 0) || (y % 4 == 0 && y % 100 != 0));

    private void ShowError(string msg)
    {
        if (!errorText) return;
        bool show = !string.IsNullOrEmpty(msg);
        if (errorText.gameObject.activeSelf != show)
            errorText.gameObject.SetActive(show);
        errorText.text = msg ?? "";
    }
}
