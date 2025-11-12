using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using System.Text.RegularExpressions;

public class DisplayUserName : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameTMPText;   // Assign this if you use TextMeshPro
    public Text nameText;          // Or assign legacy UGUI Text

    [Header("PlayerPrefs Key")]
    public string idTokenKey = "auth_id_token";

    void Start()
    {
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        string idToken = PlayerPrefs.GetString(idTokenKey, null);
        string json = DecodeJwtPayload(idToken);

        string display =
               ExtractJsonString(json, "custom:name")           // your custom attribute
            ?? ExtractJsonString(json, "name")
            ?? ExtractJsonString(json, "preferred_username")
            ?? ExtractJsonString(json, "given_name")
            ?? ExtractJsonString(json, "email")
            ?? "User";

        if (nameTMPText) nameTMPText.text = display;
        if (nameText)    nameText.text    = display;
    }

    // --- Helpers ---

    // Returns the decoded JWT payload JSON (or null)
    private string DecodeJwtPayload(string jwt)
    {
        try
        {
            if (string.IsNullOrEmpty(jwt)) return null;
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            string payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            // pad base64
            int mod4 = payload.Length % 4;
            if (mod4 > 0) payload += new string('=', 4 - mod4);

            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DisplayUserName] JWT decode failed: {e.Message}");
            return null;
        }
    }

    // Extracts a JSON string value for a simple key, including keys with colon (e.g., "custom:name")
    private string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

        // Matches:  "key" : "value"
        // Allows optional whitespace; stops at the closing quote (no escape handling for brevity)
        string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*)\"";
        var m = Regex.Match(json, pattern);
        if (m.Success && m.Groups.Count > 1)
        {
            return UnescapeJsonString(m.Groups[1].Value);
        }
        return null;
    }

    private string UnescapeJsonString(string s)
    {
        // Minimal unescape for common sequences
        return s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\/", "/")
                .Replace("\\b", "\b").Replace("\\f", "\f").Replace("\\n", "\n")
                .Replace("\\r", "\r").Replace("\\t", "\t");
    }
}
