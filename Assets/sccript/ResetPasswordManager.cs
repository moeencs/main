// File: Assets/Scripts/ResetPasswordManager.cs
// Finalizes password reset using Cognito ConfirmForgotPassword
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Submits new password along with the confirmation code (OTP) to Cognito ConfirmForgotPassword.
/// </summary>
public class ResetPasswordManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField newPasswordField;
    public TMP_InputField confirmPasswordField;
    public Button submitButton;
    public GameObject verifyingPanel;
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;

    [Header("Cognito")]
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";
    public string cognitoUrl = "https://cognito-idp.us-west-1.amazonaws.com/";

    [Header("Scenes")]
    public string loginSceneName = "LoginScene";

    private Coroutine toastCoroutine;

    void Start()
    {
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (verifyingPanel != null) verifyingPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);
    }

    public void OnSubmitClicked()
    {
        string p1 = newPasswordField != null ? newPasswordField.text : "";
        string p2 = confirmPasswordField != null ? confirmPasswordField.text : "";

        if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2))
        {
            ShowToast("Please fill both password fields.");
            return;
        }

        if (p1 != p2)
        {
            ShowToast("Passwords do not match.");
            return;
        }

        // Basic password requirements check (customize per your Cognito policy)
        if (p1.Length < 8)
        {
            ShowToast("Password must be at least 8 characters.");
            return;
        }

        // Get confirmation code + email
        string otp = PlayerPrefs.GetString("forgot_otp", "");
        string email = OtpSessionData.RealEmail;

        if (string.IsNullOrEmpty(otp))
        {
            ShowToast("Missing confirmation code. Please request a new code.");
            return;
        }
        if (string.IsNullOrEmpty(email))
        {
            ShowToast("Missing email. Please restart the forgot password flow.");
            return;
        }

        if (submitButton != null) submitButton.interactable = false;
        StartCoroutine(ConfirmForgotPasswordRequest(email, otp, p1));
    }

    private IEnumerator ConfirmForgotPasswordRequest(string email, string confirmationCode, string newPassword)
    {
        if (verifyingPanel != null) verifyingPanel.SetActive(true);

        string jsonPayload = "{\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                             "\"Username\":\"" + EscapeJson(email) + "\"," +
                             "\"ConfirmationCode\":\"" + EscapeJson(confirmationCode) + "\"," +
                             "\"Password\":\"" + EscapeJson(newPassword) + "\"}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(cognitoUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.ConfirmForgotPassword");

            yield return www.SendWebRequest();

            if (verifyingPanel != null) verifyingPanel.SetActive(false);
            if (submitButton != null) submitButton.interactable = true;

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogError("[ResetPassword] Network error: " + www.error);
                ShowToast("Network error. Please check your connection.");
                yield break;
            }

            string resp = www.downloadHandler != null ? www.downloadHandler.text : "";
            Debug.Log("[ResetPassword] Resp code: " + www.responseCode + " Body: " + resp);

            if (www.responseCode == 200)
            {
                ShowToast("Password reset successful!");
                // Clean up stored temp OTP
                PlayerPrefs.DeleteKey("forgot_otp");

                yield return new WaitForSeconds(1.0f);
                SceneManager.LoadScene(loginSceneName);
                yield break;
            }

            string errorMsg = ExtractJsonValue(resp, "message")
                              ?? ExtractJsonValue(resp, "Message")
                              ?? ExtractJsonValue(resp, "__type")
                              ?? "Password reset failed. Please try again.";

            Debug.LogError("[ResetPassword] Failed: " + errorMsg);
            ShowToast(errorMsg);
        }
    }

    #region Helpers (toast + json)
    private void ShowToast(string message, float duration = 2.5f)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.Log("[ResetPassword] " + message);
            return;
        }

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        toastText.text = message;
        toastPanel.SetActive(true);
        CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = toastPanel.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(0, 1, t / 0.25f); yield return null; }
        yield return new WaitForSeconds(duration);
        t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(1, 0, t / 0.25f); yield return null; }
        toastPanel.SetActive(false);
    }

    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private string ExtractJsonValue(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
        string pattern = $"\"{key}\"";
        int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return null;
        int start = json.IndexOf('"', colon + 1);
        if (start < 0) return null;
        int end = json.IndexOf('"', start + 1);
        if (end < 0) return null;
        return json.Substring(start + 1, end - start - 1);
    }
    #endregion
}
