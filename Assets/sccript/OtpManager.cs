using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;

public class OtpManager : MonoBehaviour
{
    [Header("OTP UI References")]
    // now supports multiple input fields (e.g., 6 separate digit fields)
    public TMP_InputField[] otpInputFields;
    public TextMeshProUGUI emailText;
    public Button verifyButton;

    [Header("Loader UI")]
    public GameObject verifyingPanel;

    [Header("Toast Message UI")]
    public GameObject toastPanel;
    public TextMeshProUGUI toastText;

    // Hardcoded for now; will later be fetched from environment
    private string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";
    private string cognitoUrl = "https://cognito-idp.us-west-1.amazonaws.com/";

    void Start()
    {
        // Set masked email text from signup step
        if (emailText != null)
        {
            if (!string.IsNullOrEmpty(OtpSessionData.MaskedEmail))
                emailText.text = $"Enter the code we've sent to {OtpSessionData.MaskedEmail}";
            else
                emailText.text = "Enter the verification code you received.";
        }

        if (verifyButton != null)
            verifyButton.onClick.AddListener(OnVerifyClicked);

        if (verifyingPanel != null)
            verifyingPanel.SetActive(false);
    }

    private void OnVerifyClicked()
    {
        // Build OTP by concatenating fields
        string otp = "";
        if (otpInputFields == null || otpInputFields.Length == 0)
        {
            Debug.LogError("OTP input fields not assigned in Inspector.");
            ShowToast("OTP fields not configured.");
            return;
        }

        for (int i = 0; i < otpInputFields.Length; i++)
        {
            if (otpInputFields[i] == null)
            {
                Debug.LogError($"OTP input field at index {i} is null.");
                ShowToast("OTP fields are not correctly assigned.");
                return;
            }

            string part = otpInputFields[i].text.Trim();
            if (string.IsNullOrEmpty(part))
            {
                ShowToast("Please enter the full OTP.");
                return;
            }
            otp += part;
        }

        // Get the real email that was stored after signup
        string actualEmail = OtpSessionData.RealEmail;
        if (string.IsNullOrEmpty(actualEmail))
        {
            Debug.LogError("Real email not found in OtpSessionData. Signup must set OtpSessionData.RealEmail.");
            ShowToast("Internal error: missing email. Please try signup again.");
            return;
        }

        StartCoroutine(VerifyOtpRequest(actualEmail, otp));
    }

    private IEnumerator VerifyOtpRequest(string email, string otp)
    {
        if (verifyingPanel != null)
            verifyingPanel.SetActive(true);

        // Build JSON body
        string jsonPayload = "{\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                             "\"Username\":\"" + EscapeJson(email) + "\"," +
                             "\"ConfirmationCode\":\"" + EscapeJson(otp) + "\"}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(cognitoUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.ConfirmSignUp");

            yield return www.SendWebRequest();

            if (verifyingPanel != null)
                verifyingPanel.SetActive(false);

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("OTP verify network error: " + www.error + " | Resp: " + www.downloadHandler.text);
                ShowToast("Network error. Please check your connection.");
                yield break;
            }

            string resp = www.downloadHandler.text;
            Debug.Log("OTP verify response: " + resp);

            if (www.responseCode == 200)
            {
                Debug.Log("OTP verification successful!");
                ShowToast("Verification successful!");
                yield return new WaitForSeconds(1.0f);

                // Auto-login using the password we cached at signup
                string pw = OtpSessionData.Password;
                if (string.IsNullOrEmpty(pw))
                {
                    // App may have been restarted; fall back gracefully
                    ShowToast("Verified! Please log in to continue.");
                    SceneManager.LoadScene("LoginScene");
                    yield break;
                }

                 yield return StartCoroutine(LoginAfterConfirm(email, pw));

                // Clear the cached password whether login succeeds or fails (safety)
                 OtpSessionData.Password = null;
                yield break;
            }
            else
            {
                string errorMsg = ExtractJsonValue(resp, "message");
                if (string.IsNullOrEmpty(errorMsg))
                    errorMsg = "Verification failed. Please try again.";
                Debug.LogError("OTP verification failed: " + errorMsg);
                ShowToast(errorMsg);
            }
        }
    }

    #region Toast Helper

    private Coroutine toastCoroutine;

    private void ShowToast(string message, float duration = 2.5f)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.LogWarning("Toast UI not assigned. Message: " + message);
            return;
        }

        if (toastCoroutine != null)
            StopCoroutine(toastCoroutine);

        toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        toastText.text = message;
        toastPanel.SetActive(true);

        CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = toastPanel.AddComponent<CanvasGroup>();

        // Fade in
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / 0.3f);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // Fade out
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / 0.3f);
            yield return null;
        }

        toastPanel.SetActive(false);
    }

    private IEnumerator LoginAfterConfirm(string email, string password)
    {
        if (verifyingPanel != null) verifyingPanel.SetActive(true);
    
        string loginJson = "{\"AuthFlow\":\"USER_PASSWORD_AUTH\"," +
                           "\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                           "\"AuthParameters\":{" +
                              "\"USERNAME\":\"" + EscapeJson(email) + "\"," +
                              "\"PASSWORD\":\"" + EscapeJson(password) + "\"" +
                           "}}";
        byte[] body = Encoding.UTF8.GetBytes(loginJson);
    
        using (var www = new UnityWebRequest(cognitoUrl, "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");
    
            yield return www.SendWebRequest();
    
            if (verifyingPanel != null) verifyingPanel.SetActive(false);
    
            string resp = www.downloadHandler != null ? www.downloadHandler.text : "";
            long status = www.responseCode;
    
            // Parse tokens like you do in LogInManager
            string accessToken  = ExtractNestedJsonValue(resp, "AuthenticationResult", "AccessToken");
            string idToken      = ExtractNestedJsonValue(resp, "AuthenticationResult", "IdToken");
            string refreshToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "RefreshToken");
    
            if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(idToken))
            {
                if (!string.IsNullOrEmpty(accessToken))  PlayerPrefs.SetString("auth_access_token", accessToken);
                if (!string.IsNullOrEmpty(idToken))      PlayerPrefs.SetString("auth_id_token", idToken);
                if (!string.IsNullOrEmpty(refreshToken)) PlayerPrefs.SetString("auth_refresh_token", refreshToken);
                PlayerPrefs.Save();
    
                Debug.Log("[OTP] Auto-login successful. Navigating to dashboard.");
                SceneManager.LoadScene("LevelsDashboard");
                yield break;
            }
    
            // Friendly error handling (mirrors your style)
            string error =
                  ExtractJsonValue(resp, "message")
               ?? ExtractJsonValue(resp, "Message")
               ?? ExtractJsonValue(resp, "error_description")
               ?? ExtractJsonValue(resp, "error")
               ?? ExtractJsonValue(resp, "__type")
               ?? $"Login failed after confirmation (HTTP {status}). Please try again.";
            Debug.LogError("[OTP] Post-confirm login failed: " + error + " | Resp: " + resp);
            ShowToast(error);
        }
    }


    #endregion

    #region JSON Helpers

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private string ExtractJsonValue(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
        string pattern = $"\"{key}\"";
        int idx = json.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase);
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
        int parentIdx = json.IndexOf(parentPattern, System.StringComparison.OrdinalIgnoreCase);
        if (parentIdx < 0) return null;
    
        int braceIdx = json.IndexOf('{', parentIdx);
        if (braceIdx < 0) return null;
    
        int depth = 0, endIdx = -1;
        for (int i = braceIdx; i < json.Length; i++)
        {
            if (json[i] == '{') depth++;
            else if (json[i] == '}')
            {
                depth--;
                if (depth == 0) { endIdx = i; break; }
            }
        }
        if (endIdx < 0) return null;
    
        string parentJson = json.Substring(braceIdx, endIdx - braceIdx + 1);
        return ExtractJsonValue(parentJson, childKey);
    }

    #endregion
}
