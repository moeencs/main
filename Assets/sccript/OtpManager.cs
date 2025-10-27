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

                // TODO: Navigate to login screen after small delay
                yield return new WaitForSeconds(1.0f);
                SceneManager.LoadScene("LoginScene");
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

    #endregion
}
