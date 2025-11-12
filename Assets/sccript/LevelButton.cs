using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    // Set in inspector or via code
    public int levelIndex = 1;       // 1-based display
    public string sceneNameToLoad;   // optional: scene name for the level

    void Reset()
    {
        label = GetComponentInChildren<TMP_Text>();
        button = GetComponent<Button>();
    }

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        UpdateLabel();
    }

    public void SetLevel(int index, string sceneName = null)
    {
        levelIndex = index;
        sceneNameToLoad = sceneName;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (label) label.text = levelIndex.ToString();
    }

    private void OnClick()
    {
        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            // Fallback: load by build index (example)
            SceneManager.LoadScene(levelIndex);
        }
    }
}
