using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections;

public class LogoutManager : MonoBehaviour
{
    [Header("UI")]
    public Button logoutButton;
    public GameObject verifyingPanel;

    [Header("Cognito")]
    // Same values you use elsewhere
    public string cognitoIdpUrl = "https://cognito-idp.us-west-1.amazonaws.com/";
    public string cognitoHostedDomain = "https://us-west-1dsoun0xib.auth.us-west-1.amazoncognito.com";
    public string clientId = "2u6fqc2oh9fdp86smbnbuhcdg8";

    [Header("Scenes")]
    public string loginSceneName = "LoginScene";

    // PlayerPrefs keys (must match the ones used in your login/signup/bootstrap)
    private const string ACCESS_TOKEN_KEY  = "auth_access_token";
    private const string ID_TOKEN_KEY      = "auth_id_token";
    private const string REFRESH_TOKEN_KEY = "auth_refresh_token";

    void Start()
    {
        if (verifyingPanel) verifyingPanel.SetActive(false);
        if (logoutButton) logoutButton.onClick.AddListener(OnLogoutClicked);
    }

    public void OnLogoutClicked()
    {
        StartCoroutine(LogoutRoutine());
    }

    private IEnumerator LogoutRoutine()
    {
        if (verifyingPanel) verifyingPanel.SetActive(true);

        string accessToken  = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, null);
        string refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, null);

        // --- 1) Best-effort: revoke refresh token (so it can’t mint new tokens)
        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Cognito RevokeToken API
            string json = "{\"ClientId\":\"" + Escape(clientId) + "\"," +
                          "\"Token\":\"" + Escape(refreshToken) + "\"," +
                          "\"TokenTypeHint\":\"refresh_token\"}";
            yield return StartCoroutine(PostToCognito(json, "AWSCognitoIdentityProviderService.RevokeToken"));
        }

        // --- 2) Best-effort: global sign-out for this access token (invalidates tokens server-side)
        if (!string.IsNullOrEmpty(accessToken))
        {
            string json = "{\"AccessToken\":\"" + Escape(accessToken) + "\"}";
            yield return StartCoroutine(PostToCognito(json, "AWSCognitoIdentityProviderService.GlobalSignOut"));
        }

        // --- 3) Clear local tokens
        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(ID_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.Save();

        // Optional (Hosted UI session sign-out in browser; uncomment if you use Hosted UI regularly):
        // string postLogout = "com.junzitechsolutions.app://signout";
        // Application.OpenURL($"{TrimSlash(cognitoHostedDomain)}/logout?client_id={UnityWebRequest.EscapeURL(clientId)}&logout_uri={UnityWebRequest.EscapeURL(postLogout)}");

        // --- 4) Go to Login scene
        if (verifyingPanel) verifyingPanel.SetActive(false);
        SceneManager.LoadScene(string.IsNullOrEmpty(loginSceneName) ? "LoginScene" : loginSceneName);
    }

    private IEnumerator PostToCognito(string jsonBody, string xAmzTarget)
    {
        byte[] body = Encoding.UTF8.GetBytes(jsonBody);
        using (var www = new UnityWebRequest(TrimSlash(cognitoIdpUrl), "POST"))
        {
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            www.SetRequestHeader("X-Amz-Target", xAmzTarget);

            yield return www.SendWebRequest();

            // Best-effort only: we log errors but do not block logout
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Logout] {xAmzTarget} network err: {www.error} | Resp: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[Logout] {xAmzTarget} OK: {www.downloadHandler.text}");
            }
        }
    }

    private string TrimSlash(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        return url.EndsWith("/") ? url.Substring(0, url.Length - 1) : url;
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
