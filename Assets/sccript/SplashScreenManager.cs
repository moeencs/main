using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

/// <summary>
/// Manages the splash screen, automatically loading the
/// next scene after a set delay.
/// </summary>
public class SplashScreenManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("The time in seconds to wait before loading the next scene.")]
    public float delayDuration = 3.0f;

    [Tooltip("The name of the scene to load after the delay. Make sure this is in your Build Settings!")]
    public string sceneToLoad = "ParentDashboard";


    // This function is called as soon as the object is active
    void Start()
    {
        // Start the coroutine that handles the timed delay
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    /// <summary>
    /// A Coroutine that waits for a specific duration
    /// and then loads the main authentication scene.
    /// </summary>
    private IEnumerator LoadNextSceneAfterDelay()
    {
        // --- 1. Wait for the specified duration ---
        // This will pause the function here without freezing the game
        yield return new WaitForSeconds(delayDuration);

        // --- 2. Load the next scene ---
        // This line will execute after the delay
        Debug.Log($"Splash screen delay finished. Loading scene: {sceneToLoad}");

        if (!PlayerPrefs.HasKey("UserInfo"))
        {
            sceneToLoad = "AuthScene";
        }
        
        SceneManager.LoadScene(sceneToLoad);
    }
}
