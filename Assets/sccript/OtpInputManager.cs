using UnityEngine;
using TMPro; // Required for TextMeshPro
using UnityEngine.EventSystems; // Required for event system checks
using UnityEngine.InputSystem;

/// <summary>
/// Manages a set of OTP input fields to automatically tab to the next field
/// when a character is entered, and tab back on backspace.
/// </summary>
public class OtpInputManager : MonoBehaviour
{
    [Header("OTP Input Fields")]
    [Tooltip("Assign all 6 OTP input fields here in order.")]
    public TMP_InputField[] otpFields;

    void Start()
    {
        // Add listeners to each input field to detect when the text is changed
        for (int i = 0; i < otpFields.Length; i++)
        {
            int index = i; // Store the index for the listener
            otpFields[i].onValueChanged.AddListener((value) => OnOtpValueChanged(index, value));
        }
    }

    void Update()
    {
        // Handle Backspace
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            // Get the currently selected UI element
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject == null) return;

            // Check if the selected object is one of our OTP fields
            for (int i = 0; i < otpFields.Length; i++)
            {
                if (otpFields[i].gameObject == selectedObject)
                {
                    // If the field is already empty and it's not the first field, move to the previous one
                    if (string.IsNullOrEmpty(otpFields[i].text) && i > 0)
                    {
                        // Select and activate the previous field
                        otpFields[i - 1].Select();
                        otpFields[i - 1].ActivateInputField();
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called when any of the OTP input fields' text is changed.
    /// </summary>
    /// <param name="fieldIndex">The index of the field that changed.</param>
    /// <param name="value">The new value in the field.</param>
    private void OnOtpValueChanged(int fieldIndex, string value)
    {
        // If a character was entered (not deleted)
        if (value.Length == 1)
        {
            // If this isn't the last field, move to the next one
            if (fieldIndex < otpFields.Length - 1)
            {
                // Select and activate the next field
                otpFields[fieldIndex + 1].Select();
                otpFields[fieldIndex + 1].ActivateInputField();
            }
        }
    }

    /// <summary>
    /// A helper function to get the complete 6-digit code as a string.
    /// You can call this from your main "Verify" button's script.
    /// </summary>
    public string GetFullOtpCode()
    {
        string otp = "";
        foreach (TMP_InputField field in otpFields)
        {
            otp += field.text;
        }
        return otp;
    }
}


