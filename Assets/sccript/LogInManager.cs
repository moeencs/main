using UnityEngine;
using UnityEngine.UI;       // Buttons, Images
using TMPro;                // TextMeshPro
using System.Collections;   // Coroutines
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;

/// <summary>
/// LogInManager - handles login UI, calls Cognito InitiateAuth, stores tokens on success.
/// </summary>
public class LogInManager : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;

    [Header("Buttons & Links")]
    public Button logInButton;
    public Button forgotPasswordButton;
    public Button googleSignInButton;
    public Button appleSignInButton;
    public Button facebookSignInButton;

    [Header("Loader & Toast UI")]
    public GameObject verifyingPanel;    // overlay while verifying (assign in inspector)
    public GameObject toastPanel;        // toast panel
    public TextMeshProUGUI toastText;    // toast text

    [Header("Cognito Settings (set in Inspector)")]
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";
    public string cognitoUrl = "https://cognito-idp.us-west-1.amazonaws.com/";

    // PlayerPrefs keys used to store tokens
    private const string ACCESS_TOKEN_KEY = "auth_access_token";
    private const string ID_TOKEN_KEY = "auth_id_token";
    private const string REFRESH_TOKEN_KEY = "auth_refresh_token";

    void Start()
    {
        // Wire existing button listeners
        if (logInButton != null) logInButton.onClick.AddListener(OnLogInClicked);
        if (forgotPasswordButton != null) forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
        if (googleSignInButton != null) googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        if (appleSignInButton != null) appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
        if (facebookSignInButton != null) facebookSignInButton.onClick.AddListener(OnFacebookSignInClicked);

        if (verifyingPanel != null) verifyingPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);
    }

    public void OnLogInClicked()
    {
        Debug.Log("Log In button clicked. Starting validation...");

        // 1. Get Text from Fields
        string email = emailInputField != null ? emailInputField.text.Trim() : "";
        string password = passwordInputField != null ? passwordInputField.text : "";

        // 2. Basic Validation
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowToast("Please enter both email and password.");
            Debug.LogError("LOGIN FAILED: Email or Password field is empty.");
            return;
        }

        if (!email.Contains("@"))
        {
            ShowToast("Please enter a valid email address.");
            Debug.LogError("LOGIN FAILED: Email address is not valid (missing '@').");
            return;
        }

        // 3. Validation Success
        Debug.Log("Validation Successful! Preparing to send to backend...");

        // Disable button while request in progress
        if (logInButton != null) logInButton.interactable = false;

        // 4. Call Backend
        StartCoroutine(SendLogInRequest(email, password));
    }

    private IEnumerator SendLogInRequest(string email, string password)
    {
        // Show loader
        if (verifyingPanel != null) verifyingPanel.SetActive(true);

        
        string jsonPayload = "{\"AuthFlow\":\"USER_PASSWORD_AUTH\"," +
                             "\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                             "\"AuthParameters\":{" +
                                "\"USERNAME\":\"" + EscapeJson(email) + "\"," +
                                "\"PASSWORD\":\"" + EscapeJson(password) + "\"" +
                             "}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(cognitoUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

            // send
            yield return www.SendWebRequest();

            // hide loader and re-enable button
            if (verifyingPanel != null) verifyingPanel.SetActive(false);
            if (logInButton != null) logInButton.interactable = true;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Login network error: " + www.error + " | Resp: " + www.downloadHandler.text);
                ShowToast("Network error. Please check your connection.");
                yield break;
            }

            string resp = www.downloadHandler.text;
            Debug.Log("Login response: " + resp);

            // Success (HTTP 200) => extract tokens
            // Parse AuthenticationResult.AccessToken, IdToken, RefreshToken
            string accessToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "AccessToken");
            string idToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "IdToken");
            string refreshToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "RefreshToken");

            if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(idToken))
            {
                // Store tokens (PlayerPrefs for now)
                if (!string.IsNullOrEmpty(accessToken))
                {
                    PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
                }
                if (!string.IsNullOrEmpty(idToken))
                {
                    PlayerPrefs.SetString(ID_TOKEN_KEY, idToken);
                }
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);
                }
                PlayerPrefs.Save();

                Debug.Log("Login successful. Tokens stored in PlayerPrefs.");

                ShowToast("Login successful!");

                // TODO: Navigate to Main UI (enable panel or load scene)
                // Example placeholder: call a navigation manager here or load a scene: SceneManager.LoadScene("MainScene");
                SceneManager.LoadScene("Chapter1");
                Debug.Log("TODO: Navigate to Main UI here.");
                yield break;
            }

            // If we reach here, treat as error. Extract error message if present.
            string errorMsg = ExtractJsonValue(resp, "message");
            if (string.IsNullOrEmpty(errorMsg))
            {
                // Some Cognito errors are under "__type"
                errorMsg = ExtractJsonValue(resp, "__type");
            }

            if (string.IsNullOrEmpty(errorMsg))
                errorMsg = "Login failed. Please try again.";

            Debug.LogError("Login failed: " + errorMsg);
            ShowToast(errorMsg);
        }
    }

    // Social sign-in placeholders (unchanged behavior)
    public void OnForgotPasswordClicked()
    {
        Debug.Log("Forgot Password link clicked. Opening password reset screen/flow...");
        // implement navigation to Forgot Password panel
    }

    public void OnGoogleSignInClicked()
    {
        Debug.Log("Google Sign-In button clicked. Calling backend/SDK...");
        StartCoroutine(SendSocialSignInRequest("google"));
    }

    public void OnAppleSignInClicked()
    {
        Debug.Log("Apple Sign-In button clicked. Calling backend/SDK...");
        StartCoroutine(SendSocialSignInRequest("apple"));
    }

    public void OnFacebookSignInClicked()
    {
        Debug.Log("Facebook Sign-In button clicked. Calling backend/SDK...");
        StartCoroutine(SendSocialSignInRequest("facebook"));
    }

    private IEnumerator SendSocialSignInRequest(string provider)
    {

        // TODO: Implement social sign-in request
        Debug.Log($"Simulating '{provider}' sign-in request...");
        yield return new WaitForSeconds(1.0f);
        Debug.Log($"'{provider}' SIGN-IN SUCCESSFUL! (Simulated)");
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

    // Extracts simple string value for top-level key like "token":"value"
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
        string value = json.Substring(start + 1, end - start - 1);
        return value;
    }

    // Extract nested value: looks for parentKey, then childKey inside it (works for simple JSON)
    private string ExtractNestedJsonValue(string json, string parentKey, string childKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(childKey)) return null;
        string parentPattern = $"\"{parentKey}\"";
        int parentIdx = json.IndexOf(parentPattern, System.StringComparison.OrdinalIgnoreCase);
        if (parentIdx < 0) return null;

        // Find the opening brace for the parent object
        int braceIdx = json.IndexOf('{', parentIdx);
        if (braceIdx < 0) return null;

        // Find the closing brace of the parent object by scanning forward and balancing braces
        int depth = 0;
        int endIdx = -1;
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

        // Extract substring for the parent object
        string parentJson = json.Substring(braceIdx, endIdx - braceIdx + 1);

        // Now search for childKey inside parentJson
        return ExtractJsonValue(parentJson, childKey);
    }

    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    #endregion
}
