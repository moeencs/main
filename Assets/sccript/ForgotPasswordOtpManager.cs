// File: Assets/Scripts/ForgotPasswordOtpManager.cs
// Collects OTP digits for forgot password and navigates to ResetPassword scene
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Collects OTP (digit fields) for forgot-password flow.
/// Stores the concatenated code into PlayerPrefs under key "forgot_otp" and navigates to reset screen.
/// </summary>
public class ForgotPasswordOtpManager : MonoBehaviour
{
    [Header("OTP fields (assign in order)")]
    public TMP_InputField[] otpInputFields;
    public TextMeshProUGUI emailText;
    public Button verifyButton;

    [Header("Scenes")]
    public string resetPasswordSceneName = "ResetPasswordScene";

    void Start()
    {
        if (verifyButton != null) verifyButton.onClick.AddListener(OnVerifyClicked);

        if (emailText != null)
        {
            if (!string.IsNullOrEmpty(OtpSessionData.MaskedEmail))
                emailText.text = $"Enter the code we've sent to {OtpSessionData.MaskedEmail}";
            else
                emailText.text = "Enter the code sent to your email";
        }
    }

    private void OnVerifyClicked()
    {
        if (otpInputFields == null || otpInputFields.Length == 0)
        {
            ShowLocalLog("OTP fields not configured.");
            return;
        }

        string otp = "";
        for (int i = 0; i < otpInputFields.Length; i++)
        {
            if (otpInputFields[i] == null) { ShowLocalLog("OTP input missing."); return; }
            string part = otpInputFields[i].text.Trim();
            if (string.IsNullOrEmpty(part)) { ShowLocalLog("Please enter the full OTP."); return; }
            otp += part;
        }

        // Save OTP temporarily for next scene (reset password)
        PlayerPrefs.SetString("forgot_otp", otp);
        PlayerPrefs.Save();

        // Navigate to reset password scene where we will call ConfirmForgotPassword
        SceneManager.LoadScene(resetPasswordSceneName);
    }

    private void ShowLocalLog(string msg)
    {
        Debug.Log("[ForgotPasswordOtpManager] " + msg);
        // Ideally mirror to UI toast - omitted to keep this minimal; wire your toast if needed
    }
}
