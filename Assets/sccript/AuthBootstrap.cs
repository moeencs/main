using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AuthBootstrap : MonoBehaviour
{
    [Header("Cognito / OAuth")]
    [Tooltip("Cognito hosted domain (used for other flows).")]
    public string cognitoHostedDomain = "https://us-west-1dsoun0xib.auth.us-west-1.amazoncognito.com";

    [Tooltip("Cognito App Client ID (must match the one used in SignUpManager/LogInManager)")]
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";

    [Tooltip("Cognito IDP endpoint used for InitiateAuth (REFRESH_TOKEN_AUTH)")]
    public string cognitoIdpUrl = "https://cognito-idp.us-west-1.amazonaws.com/";

    [Header("Scenes")]
    [Tooltip("Scene to load when user is authenticated")]
    public string mainSceneName = "Chapter1";

    [Tooltip("Scene to load when user must authenticate (login/signup)")]
    public string loginSceneName = "LoginScene";

    [Header("Timing & behavior")]
    [Tooltip("Seconds to wait on splash before automatic navigation (useful to show splash). Set to 0 to navigate immediately.")]
    public float splashDelaySeconds = 1.0f;

    [Tooltip("If true, this component will automatically start on Start(). If false, call StartBootstrap() manually from your splash script.")]
    public bool autoStart = false;

    [Tooltip("Time (seconds) to wait for token endpoint responses before treating as network error.")]
    public int requestTimeoutSeconds = 15;

    // PlayerPrefs keys (must match SignUpManager / LogInManager)
    private const string ACCESS_TOKEN_KEY = "auth_access_token";
    private const string ID_TOKEN_KEY = "auth_id_token";
    private const string REFRESH_TOKEN_KEY = "auth_refresh_token";

    private bool isRunning = false;

    void Start()
    {
        if (autoStart)
            StartBootstrap();
    }

    /// <summary>
    /// Public entry point so your existing splash script can call this when it finishes animations.
    /// </summary>
    public void StartBootstrap()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(BootstrapRoutine());
    }

    private IEnumerator BootstrapRoutine()
    {
        Debug.Log("[AuthBootstrap] Starting bootstrap sequence...");

        // Optional small delay to show splash
        if (splashDelaySeconds > 0f)
            yield return new WaitForSeconds(splashDelaySeconds);

        string idToken = PlayerPrefs.GetString(ID_TOKEN_KEY, null);
        string refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, null);

        if (!string.IsNullOrEmpty(idToken))
        {
            long exp = GetJwtExpiry(idToken);
            if (exp > 0)
            {
                DateTimeOffset expDt = DateTimeOffset.FromUnixTimeSeconds(exp);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                TimeSpan remaining = expDt - now;
                Debug.Log($"[AuthBootstrap] ID token expiry: {expDt:u}, remaining seconds: {remaining.TotalSeconds:F0}");

                // If token still has > 60s remaining, accept it as valid
                if (remaining.TotalSeconds > 60)
                {
                    Debug.Log("[AuthBootstrap] ID token valid. Navigating to main scene.");
                    NavigateToMain();
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[AuthBootstrap] Could not parse exp from ID token. Will attempt refresh if refresh token exists.");
            }

            // If we reach here, id_token exists but is expired (or exp unknown). Try refresh if available.
            if (!string.IsNullOrEmpty(refreshToken))
            {
                Debug.Log("[AuthBootstrap] Attempting refresh token exchange via InitiateAuth...");
                yield return StartCoroutine(RefreshTokenCoroutine(refreshToken));
                yield break; // RefreshTokenCoroutine handles navigation on success/failure
            }
            else
            {
                Debug.Log("[AuthBootstrap] No refresh token available. Navigating to login.");
                ClearAuthTokens();
                NavigateToLogin();
                yield break;
            }
        }
        else
        {
            Debug.Log("[AuthBootstrap] No id_token found. Navigating to login.");
            NavigateToLogin();
            yield break;
        }
    }

    
    private IEnumerator RefreshTokenCoroutine(string refreshToken)
    {
        string endpoint = TrimTrailingSlash(cognitoIdpUrl);

        // Build JSON body
        string jsonPayload = "{" +
            "\"ClientId\":\"" + EscapeJson(clientId) + "\"," +
            "\"AuthFlow\":\"REFRESH_TOKEN_AUTH\"," +
            "\"AuthParameters\":{" +
                "\"REFRESH_TOKEN\":\"" + EscapeJson(refreshToken) + "\"" +
            "}" +
        "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(endpoint, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");
            www.timeout = requestTimeoutSeconds;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AuthBootstrap] Refresh token network error: {www.error} | Resp: {www.downloadHandler.text}");
                // On failure, clear tokens and send user to login
                ClearAuthTokens();
                NavigateToLogin();
                yield break;
            }

            string resp = www.downloadHandler.text;
            Debug.Log($"[AuthBootstrap] InitiateAuth response: {resp}");

            string newAccessToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "AccessToken");
            string newIdToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "IdToken");
            string newRefreshToken = ExtractNestedJsonValue(resp, "AuthenticationResult", "RefreshToken");

            if (!string.IsNullOrEmpty(newAccessToken) || !string.IsNullOrEmpty(newIdToken))
            {
                if (!string.IsNullOrEmpty(newAccessToken)) PlayerPrefs.SetString(ACCESS_TOKEN_KEY, newAccessToken);
                if (!string.IsNullOrEmpty(newIdToken)) PlayerPrefs.SetString(ID_TOKEN_KEY, newIdToken);
                if (!string.IsNullOrEmpty(newRefreshToken)) PlayerPrefs.SetString(REFRESH_TOKEN_KEY, newRefreshToken);
                PlayerPrefs.Save();

                Debug.Log("[AuthBootstrap] Tokens refreshed (InitiateAuth) and saved. Navigating to main scene.");
                NavigateToMain();
                yield break;
            }
            else
            {
                string errorMsg = ExtractJsonValue(resp, "message") ?? ExtractJsonValue(resp, "__type") ?? "Refresh failed";
                Debug.LogError($"[AuthBootstrap] InitiateAuth refresh failed: {errorMsg}");
                ClearAuthTokens();
                NavigateToLogin();
                yield break;
            }
        }
    }

    #region Navigation helpers
    private void NavigateToMain()
    {
        if (!string.IsNullOrEmpty(mainSceneName))
        {
            Debug.Log($"[AuthBootstrap] Loading main scene: {mainSceneName}");
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning("[AuthBootstrap] mainSceneName empty - no navigation performed.");
        }
    }

    private void NavigateToLogin()
    {
        if (!string.IsNullOrEmpty(loginSceneName))
        {
            Debug.Log($"[AuthBootstrap] Loading login scene: {loginSceneName}");
            SceneManager.LoadScene(loginSceneName);
        }
        else
        {
            Debug.LogWarning("[AuthBootstrap] loginSceneName empty - no navigation performed.");
        }
    }
    #endregion

    #region Utility / JSON helpers (small, resilient)
    private void ClearAuthTokens()
    {
        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(ID_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.Save();
    }

    // Tries to parse exp (unix seconds) from id_token (JWT) payload. Returns 0 on failure.
    private long GetJwtExpiry(string jwt)
    {
        try
        {
            if (string.IsNullOrEmpty(jwt)) return 0;
            string[] parts = jwt.Split('.');
            if (parts.Length < 2) return 0;
            string payload = parts[1];
            string json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            // quick extract of "exp": number
            int idx = json.IndexOf("\"exp\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return 0;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return 0;
            int start = colon + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.')) end++;
            string numStr = json.Substring(start, end - start);
            if (long.TryParse(numStr.Split('.')[0], out long exp))
                return exp;
            return 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AuthBootstrap] Failed to parse JWT exp: {ex}");
            return 0;
        }
    }

    // base64url decode
    private static byte[] Base64UrlDecode(string input)
    {
        string s = input;
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 0: break;
            default: s += ""; break;
        }
        return Convert.FromBase64String(s);
    }

    // Very small JSON extractor: returns top-level string value for key, or null.
    private string ExtractJsonValue(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
        string pattern = $"\"{key}\"";
        int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return null;
        int quoteStart = json.IndexOf('"', colon + 1);
        if (quoteStart < 0)
        {
            // maybe it's unquoted (numbers)
            int valStart = colon + 1;
            while (valStart < json.Length && char.IsWhiteSpace(json[valStart])) valStart++;
            int valEnd = valStart;
            while (valEnd < json.Length && json[valEnd] != ',' && json[valEnd] != '}' && !char.IsWhiteSpace(json[valEnd])) valEnd++;
            return json.Substring(valStart, valEnd - valStart).Trim();
        }
        int quoteEnd = json.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0) return null;
        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }

    // Extract nested value: looks for parentKey, then childKey inside it (works for simple JSON)
    private string ExtractNestedJsonValue(string json, string parentKey, string childKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(childKey)) return null;
        string parentPattern = $"\"{parentKey}\"";
        int parentIdx = json.IndexOf(parentPattern, StringComparison.OrdinalIgnoreCase);
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

    private string TrimTrailingSlash(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.EndsWith("/")) return url.Substring(0, url.Length - 1);
        return url;
    }
    #endregion
}
