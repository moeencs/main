using System.Collections;
using System.Text;
using Newtonsoft.Json;
using sccript.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace sccript.Services
{
    public class ProgressService
    {
        private const string SAVE_PROGRESS_URL = "https://w7hwcmezek.execute-api.us-west-1.amazonaws.com/default/progress-saveProgress";
        
        public static IEnumerator SaveProgress(GameProgress progress)
        {   
            using UnityWebRequest request = new UnityWebRequest(SAVE_PROGRESS_URL, "POST");
            string json = JsonConvert.SerializeObject(progress);

            var bodyRaw = Encoding.UTF8.GetBytes(json);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            string idToken = PlayerPrefs.GetString("auth_id_token");
            request.SetRequestHeader("Authorization", $"Bearer {idToken}");
            request.SetRequestHeader("Content-Type", "application/x-amz-json-1.1");
            request.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

            // send
            yield return request.SendWebRequest();

            string resp = request.downloadHandler.text;
            Debug.Log("Response: " + resp);
        }
    }
}