using UnityEngine;
using UnityEngine.SceneManagement; // Required for changing scenes

public class SceneNavigation : MonoBehaviour
{
    // This function will load your Sign-Up scene.
    // Ensure the scene name "AuthScene" exactly matches your scene file's name.
    public void LoadSignUpScene()
    {
        SceneManager.LoadScene("AuthScene");
    }

    // This function will load your Login scene.
    // Ensure the scene name "LoginScene" exactly matches your scene file's name.
    public void LoadLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    // You can add more functions here later for other scenes, like a Main Menu.
    /*
    public void LoadMainMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
    */
}
