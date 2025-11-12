// File: Assets/Scripts/ForgotPasswordManager.cs
// Manages "enter email to receive OTP" -> calls Cognito ForgotPassword
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Send a ForgotPassword request to Cognito (sends OTP to user's email).
/// </summary>
public class ForgotPasswordManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInputField;
    public Button sendCodeButton;
    public GameObject verifyingPanel;
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;

    [Header("Cognito")]
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";
    public string cognitoUrl = "https://cognito-idp.us-west-1.amazonaws.com/";

    [Header("Scenes")]
    public string otpSceneName = "ForgotPasswordOtpScene";

    private Coroutine toastCoroutine;

    void Start()
    {
        if (sendCodeButton != null) sendCodeButton.onClick.AddListener(OnSendCodeClicked);
        if (verifyingPanel != null) verifyingPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);
    }

    public void OnSendCodeClicked()
    {
        string email = emailInputField != null ? emailInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            ShowToast("Please enter a valid email.");
            return;
        }

        if (sendCodeButton != null) sendCodeButton.interactable = false;
        StartCoroutine(SendForgotPasswordRequest(email));
    }

    private IEnumerator SendForgotPasswordRequest(string email)
    {
        if (verifyingPanel != null) verifyingPanel.SetActive(true);

        string jsonPayload = "{\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                             "\"Username\":\"" + EscapeJson(email) + "\"}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(cognitoUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.ForgotPassword");

            yield return www.SendWebRequest();

            if (verifyingPanel != null) verifyingPanel.SetActive(false);
            if (sendCodeButton != null) sendCodeButton.interactable = true;

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogError("[ForgotPassword] Network error: " + www.error);
                ShowToast("Network error. Please check your connection.");
                yield break;
            }

            string resp = www.downloadHandler != null ? www.downloadHandler.text : "";
            Debug.Log("[ForgotPassword] Resp: " + resp + " | Code: " + www.responseCode);

            if (www.responseCode == 200 && resp.Contains("CodeDeliveryDetails"))
            {
                string destination = ExtractNestedJsonValue(resp, "CodeDeliveryDetails", "Destination");
                if (!string.IsNullOrEmpty(destination))
                    OtpSessionData.MaskedEmail = destination;
                else
                    OtpSessionData.MaskedEmail = "your email";

                OtpSessionData.RealEmail = email;

                ShowToast("OTP sent. Check your email.");
                // Navigate to OTP entry scene
                yield return new WaitForSeconds(0.8f);
                SceneManager.LoadScene(otpSceneName);
                yield break;
            }

            // Extract error message
            string errorMsg = ExtractJsonValue(resp, "message")
                              ?? ExtractJsonValue(resp, "Message")
                              ?? ExtractJsonValue(resp, "__type")
                              ?? "Unable to send code. Please try again.";

            Debug.LogError("[ForgotPassword] Failed: " + errorMsg);
            ShowToast(errorMsg);
        }
    }

    #region Helpers (toast + simple json)
    private void ShowToast(string message, float duration = 2.5f)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.Log("[ForgotPassword] " + message);
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

    private string ExtractNestedJsonValue(string json, string parentKey, string childKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(childKey)) return null;
        string parentPattern = $"\"{parentKey}\"";
        int parentIdx = json.IndexOf(parentPattern, StringComparison.OrdinalIgnoreCase);
        if (parentIdx < 0) return null;

        int braceIdx = json.IndexOf('{', parentIdx);
        if (braceIdx < 0) return null;

        int depth = 0;
        int endIdx = -1;
        for (int i = braceIdx; i < json.Length; i++)
        {
            if (json[i] == '{') depth++;
            else if (json[i] == '}') { depth--; if (depth == 0) { endIdx = i; break; } }
        }
        if (endIdx < 0) return null;

        string parentJson = json.Substring(braceIdx, endIdx - braceIdx + 1);
        return ExtractJsonValue(parentJson, childKey);
    }
    #endregion
}
