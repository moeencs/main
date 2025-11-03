using UnityEngine;
using UnityEngine.UI; // Required for Buttons
using TMPro;          // Required for TextMeshPro elements
using System.Collections; // Coroutines
using System.Text;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Manages Sign Up UI + quick Google Sign-In using Cognito Hosted UI.
/// </summary>
public class SignUpManager : MonoBehaviour
{
    [Header("UI Element References")]
    public TMP_InputField nameInputField;
    public TMP_InputField emailInputField;
    public TMP_InputField dateInputField;
    public TMP_InputField passwordInputField;

    [Header("Button References")]
    public Button signUpButton;
    public Button googleSignInButton;
    public Button appleSignInButton;
    public Button facebookSignInButton;

    [Header("OTP UI (assign when OTP UI exists)")]
    public GameObject panelOtp;                 
    public TextMeshProUGUI otpDestinationText;
    public GameObject panelSignup;

    [Header("Cognito Settings")]
    public string cognitoUrl = "https://cognito-idp.us-west-1.amazonaws.com/"; 


    [Header("Cognito / Hosted UI Settings (set these in Inspector)")]
    // Domain for Cognito hosted UI (e.g. https://your-domain.auth.us-west-1.amazoncognito.com)
    // Do NOT include trailing paths; token endpoint will be `${cognitoHostedDomain}/oauth2/token`
    public string cognitoHostedDomain = "https://us-west-1dsoun0xib.auth.us-west-1.amazoncognito.com";
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";
    // Redirect URI that Cognito will redirect to
    public string redirectUri = "com.junzitechsolutions.app://oauth";
    // Scopes to request
    public string googleScope = "openid+email+profile";

    [Header("Optional UI: loader & toast (assign if you have them)")]
    public GameObject verifyingPanel;   // small overlay while exchanging code for token
    public GameObject toastPanel;       // optional toast panel
    public TextMeshProUGUI toastText;   // text element inside toast panel

    // Other fields
    private DateInputMask _dateMask;

    // Storage keys for tokens
    private const string ACCESS_TOKEN_KEY = "auth_access_token";
    private const string ID_TOKEN_KEY = "auth_id_token";
    private const string REFRESH_TOKEN_KEY = "auth_refresh_token";

    // Internal state
    private string pendingCode = null;
    private Coroutine toastCoroutine = null;

    void Start()
    {
        // Existing signup button wiring
        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpClicked);

        if (googleSignInButton != null)
            googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);

        if (verifyingPanel != null) verifyingPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);
        
        // Cache the date mask on the date input (if present)
        if (dateInputField != null)
        _dateMask = dateInputField.GetComponent<DateInputMask>();

        if (verifyingPanel != null) verifyingPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);

        // If the app was opened via a deep link (Application.absoluteURL) before Start, handle it
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            Debug.Log("[SignUpManager] App opened with URL: " + Application.absoluteURL);
            HandleDeepLink(Application.absoluteURL);
        }
    }

    void OnEnable()
    {
        // Subscribe to deep link activation (when the app is already running and a deep link opens it)
        Application.deepLinkActivated += HandleDeepLink;
    }

    void OnDisable()
    {
        Application.deepLinkActivated -= HandleDeepLink;
    }

    /// <summary>
    /// Handle incoming deep link URLs.
    /// Expected format: com.example.app://oauth?code=AUTH_CODE or com.example.app://oauth/?code=AUTH_CODE
    /// </summary>
    /// <param name="urlOrLink">incoming URL</param>
    private void HandleDeepLink(string urlOrLink)
    {
        try
        {
            if (string.IsNullOrEmpty(urlOrLink)) return;
            Debug.Log("[SignUpManager] Deep link received: " + urlOrLink);

            Uri uri = new Uri(urlOrLink);
            string query = uri.Query; // contains ?code=xxx or ?code=xxx&state=...
            if (string.IsNullOrEmpty(query))
            {
                Debug.Log("[SignUpManager] Deep link had no query parameters.");
                return;
            }

            // Simple parse for "code" param
            string code = null;
            string[] parts = query.TrimStart('?').Split('&');
            foreach (var p in parts)
            {
                var kv = p.Split('=');
                if (kv.Length == 2 && kv[0] == "code")
                {
                    code = Uri.UnescapeDataString(kv[1]);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(code))
            {
                Debug.Log("[SignUpManager] Authorization code received via deep link.");
                // Start exchange coroutine
                StartCoroutine(ExchangeCodeForTokenCoroutine(code));
            }
            else
            {
                Debug.LogWarning("[SignUpManager] No code parameter found in deep link.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[SignUpManager] Error parsing deep link: " + ex);
        }
    }

    /// <summary>
    /// Called when the Google Sign-In button is clicked.
    /// It opens the Cognito Hosted UI authorize URL in the system browser.
    /// The redirectUri must be registered in your Cognito App Client (Callback URLs).
    /// </summary>
    public void OnGoogleSignInClicked()
    {
        string authorizeUrl = $"{TrimTrailingSlash(cognitoHostedDomain)}/oauth2/authorize" +
                              $"?identity_provider=Google" +
                              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                              $"&response_type=code" +
                              $"&client_id={Uri.EscapeDataString(clientId)}" +
                              $"&scope=openid+email+profile";

        Debug.Log("[SignUpManager] Opening Hosted UI: " + authorizeUrl);

        // Open system browser — user logs in and when finished Cognito will redirect to the redirectUri
        Application.OpenURL(authorizeUrl);

        // The app will be re-opened via deep link; HandleDeepLink will receive the code.
        ShowToast("Opening Google sign-in...", 2f);
    }

    // Helper to trim trailing slash if accidentally added
    private string TrimTrailingSlash(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.EndsWith("/")) return url.Substring(0, url.Length - 1);
        return url;
    }

    /// <summary>
    /// Exchanges an authorization code for tokens at Cognito token endpoint.
    /// </summary>
    private IEnumerator ExchangeCodeForTokenCoroutine(string code)
    {
        if (verifyingPanel != null) verifyingPanel.SetActive(true);

        // Token endpoint is /oauth2/token under the hosted domain
        string tokenEndpoint = $"{TrimTrailingSlash(cognitoHostedDomain)}/oauth2/token";

        string form = $"grant_type=authorization_code&client_id={Uri.EscapeDataString(clientId)}&code={Uri.EscapeDataString(code)}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(form);

        using (UnityWebRequest www = new UnityWebRequest(tokenEndpoint, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            // Note: no X-Amz-Target for token endpoint
            yield return www.SendWebRequest();

            if (verifyingPanel != null) verifyingPanel.SetActive(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SignUpManager] Token exchange network error: " + www.error + " | Resp: " + www.downloadHandler.text);
                ShowToast("Network error during sign-in. Please try again.");
                yield break;
            }

            string resp = www.downloadHandler.text;
            Debug.Log("[SignUpManager] Token endpoint response: " + resp);

            // Parse access_token, id_token, refresh_token
            string accessToken = ExtractJsonValue(resp, "access_token");
            string idToken = ExtractJsonValue(resp, "id_token");
            string refreshToken = ExtractJsonValue(resp, "refresh_token");

            if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(idToken))
            {
                // Store tokens in PlayerPrefs (simple approach)
                if (!string.IsNullOrEmpty(accessToken)) PlayerPrefs.SetString(ACCESS_TOKEN_KEY, accessToken);
                if (!string.IsNullOrEmpty(idToken)) PlayerPrefs.SetString(ID_TOKEN_KEY, idToken);
                if (!string.IsNullOrEmpty(refreshToken)) PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);
                PlayerPrefs.Save();

                Debug.Log("[SignUpManager] Google sign-in tokens stored.");

                ShowToast("Sign-in successful!", 2f);

                SceneManager.LoadScene("Chapter1");
                Debug.Log("[SignUpManager] TODO: Navigate to Main UI here.");

                yield break;
            }
            else
            {
                // Attempt to extract error message if present
                string errorMsg = ExtractJsonValue(resp, "error_description");
                if (string.IsNullOrEmpty(errorMsg))
                    errorMsg = ExtractJsonValue(resp, "error");
                if (string.IsNullOrEmpty(errorMsg))
                    errorMsg = "Sign-in failed. Please try again.";

                Debug.LogError("[SignUpManager] Token exchange failed: " + errorMsg);
                ShowToast(errorMsg, 3f);
                yield break;
            }
        }
    }

    #region Existing Signup flow (unchanged)

public void OnSignUpClicked()
{
    Debug.Log("Sign Up button clicked. Starting validation...");

    string name = nameInputField.text;
    string email = emailInputField.text;
    string password = passwordInputField.text;

    // Use the mask to read/validate date; require valid MM/DD/YYYY
    string date; // we will pass ISO (yyyy-MM-dd) to backend
    if (_dateMask != null)
    {
        if (!_dateMask.TryGetDate(out var dob))
        {
            ShowToast("Please enter a valid date (MM/DD/YYYY).", 3f);
            return;
        }
        date = dob.ToString("yyyy-MM-dd"); // backend-friendly string
    }
    else
    {
        // Fallback (shouldn't happen if mask is attached)
        var raw = dateInputField.text;
        ShowToast("Date field not set up. Please try again.", 3f);
        Debug.LogWarning($"[SignUp] Date mask missing. Raw date was: {raw}");
        return;
    }

    // --- Basic Validation ---
    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ShowToast("Please fill out all the fields.", 3f);
        return;
    }

    if (!email.Contains("@")) // keep your existing email check
    {
        ShowToast("Please enter a valid email address.", 3f);
        return;
    }

    Debug.Log("Validation Successful! Preparing to send to backend...");

    // Pass ISO date to your request
    StartCoroutine(SendSignUpRequest(name, email, date, password));
}


private IEnumerator SendSignUpRequest(string name, string email, string date, string password)
{
    // The Client ID from your Cognito User Pool
    string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8"; 

    // Build the JSON payload exactly as Cognito expects
    string jsonPayload = "{\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
                         "\"Username\":\"" + EscapeJson(email) + "\"," +
                         "\"Password\":\"" + EscapeJson(password) + "\"," +
                         "\"UserAttributes\":[" +
                            "{\"Name\":\"custom:dob\",\"Value\":\"" + EscapeJson(date) + "\"}," +
                            "{\"Name\":\"custom:name\",\"Value\":\"" + EscapeJson(name) + "\"}" +
                         "]}";

    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

    using (UnityWebRequest www = new UnityWebRequest(cognitoUrl, "POST"))
    {
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        // Required Cognito headers
        www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
        www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.SignUp");

        yield return www.SendWebRequest();

        // 1. Handle ONLY true network-level errors first (like no internet)
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"Signup connection error: {www.error}");
            ShowToast("Network error. Please check your connection and try again.", 3f);
            yield break;
        }

        string resp = www.downloadHandler.text;
        Debug.Log("Cognito signup response: " + resp);

        // 2. Handle the successful sign-up case (which requires OTP verification)
        if (resp.Contains("CodeDeliveryDetails"))
        {
            string maskedDestination = ExtractNestedJsonValue(resp, "CodeDeliveryDetails", "Destination");
            OtpSessionData.MaskedEmail = maskedDestination;
            OtpSessionData.RealEmail = email; 

            Debug.Log("Sign up successful. Proceeding to OTP verification.");
            SceneManager.LoadScene("OTPScene");
            yield break; 
        }

        // --- 3. UPDATED: Handle ALL unsuccessful sign-up errors from Cognito ---
        
        // This is the final message we will show the user.
        string userMessage;

        // Try to get the specific "message" and "__type" from Cognito's JSON response.
        string cognitoMessage = ExtractJsonValue(resp, "message");
        string errorType = ExtractJsonValue(resp, "__type");

        // Use a switch on the error type to create a clean, default message for known errors.
        switch (errorType)
        {
            case "UsernameExistsException":
                userMessage = "An account with this email already exists.";
                break;
                
            case "InvalidPasswordException":
                // The message from Cognito is often more specific here, e.g., "Password must have uppercase..."
                // So we'll prioritize that if it exists, otherwise show a generic message.
                userMessage = !string.IsNullOrEmpty(cognitoMessage) 
                    ? cognitoMessage 
                    : "Password does not meet requirements (e.g., length, uppercase, numbers, symbols).";
                break;
                
            case "InvalidParameterException":
                // This error can happen for many reasons, so we check for Cognito's specific message.
                userMessage = !string.IsNullOrEmpty(cognitoMessage) 
                    ? cognitoMessage 
                    : "One or more fields are invalid. Please check your email and date format.";
                break;
                
            case "CodeDeliveryFailureException":
                userMessage = "Could not send verification code. Please double-check your email address.";
                break;
                
            case "TooManyRequestsException":
                userMessage = "You've made too many requests. Please wait a moment and try again.";
                break;
                
            default:
                // If the error type is unknown, use the message from Cognito if we have it.
                // Otherwise, use a generic fallback.
                if (!string.IsNullOrEmpty(cognitoMessage)) {
                    userMessage = cognitoMessage;
                } else {
                    userMessage = "An unexpected sign-up error occurred. Please try again.";
                }
                break;
        }
        
        // Finally, log the technical error and display the user-friendly message in the toast panel.
        Debug.LogError($"Signup failed with Cognito error: {errorType} | Message: {cognitoMessage}");
        ShowToast(userMessage, 4f); // Show for a longer duration
    }
}
    #endregion

    #region Toast helper (simple)
    private IEnumerator ToastRoutine(string message, float duration)
    {
        if (toastText != null) toastText.text = message;
        if (toastPanel != null) toastPanel.SetActive(true);

        CanvasGroup cg = null;
        if (toastPanel != null)
        {
            cg = toastPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = toastPanel.AddComponent<CanvasGroup>();
        }

        // fade in
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t / 0.2f);
            yield return null;
        }

        if (toastPanel != null) toastPanel.SetActive(false);
    }

    public void ShowToast(string message, float duration = 2.5f)
    {
        if (toastPanel == null || toastText == null)
        {
            Debug.Log("[SignUpManager] Toast UI not assigned. Message: " + message);
            return;
        }

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
    }

    //private Coroutine toastCoroutine = null;
    #endregion

    #region JSON helpers (simple)
    // Very small helpers for token JSON parsing (works for simple top-level string values)
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

    // reuse small escape helper if needed by other code
    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

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
    #endregion
}
