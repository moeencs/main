using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour
{
    [SerializeField]
    GameObject MainScreen, EnterNameScreen;


    public GameObject redArrow; // Assign in Inspector
    bool IsCharacterSelectionScreen = true;

    private Dictionary<string, Vector3> characterArrowPositions = new Dictionary<string, Vector3>()
    {
        { "Robot", new Vector3(-0.6f, 3.45f, -5.1f) },
        { "Dog", new Vector3(-4.38f, 3.45f, -5.1f) },
        { "Cat", new Vector3(3.59f, 3.45f, -5.1f) }
    };

    void Start()
    {
        if (redArrow != null)
        {
            redArrow.transform.localPosition = characterArrowPositions[GameManager.SelectedCharacter]; // Default
            redArrow.SetActive(true);
        }

    }

    void Update()
    {
        if (IsCharacterSelectionScreen == false)
            return;
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPosition = Pointer.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("SelectableCharacter"))
                {
                    string selectedName = hit.collider.gameObject.name;
                    SelectCharacter(selectedName);
                }
            }
        }
    }

    void SelectCharacter(string name)
    {
        if (!characterArrowPositions.ContainsKey(name))
        {
            Debug.LogWarning("Character name not recognized: " + name);
            return;
        }

        GameManager.SelectedCharacter = name;
        Debug.Log("Selected Character: " + name);

        if (redArrow != null)
        {
            redArrow.transform.localPosition = characterArrowPositions[name];
        }
    }

    public void OnClickPlayBtn()
    {
        IsCharacterSelectionScreen = false;
        EnterNameScreen.SetActive(true);

    }
    public void onClickCloseEnterNameScreen()
    {
        EnterNameScreen.SetActive(false);
        IsCharacterSelectionScreen = true;
    }
    public void OnclickStartGameBtn()
    {
        SceneManager.LoadScene("PlayGround");
    }
    public void OnClickChapter1()
    {
        SceneManager.LoadSceneAsync("Chapter1");
    }
}
