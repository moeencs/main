using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Chapter1Mannager : MonoBehaviour
{
    

    [Header("UI References")]
    public TextMeshProUGUI dialogueText;

    int lattersTaskInd = 0;

    public Button nextButton;
    public GameObject DialogueBox;

    [SerializeField] GameObject task1Objects;
    [SerializeField] GameObject task2Objects;

    [SerializeField] AudioSource audioSource;
    
    

    [Header("Dialogue Settings")]
    public Chat[] eachChat;
    public ActorDetail[] eachActor;

    public float typingSpeed = 0.03f;
    public float autoAdvanceDelay = 2f;

    private int currentLineIndex = 0;
     public int taskInd = 0;
    int chatInd = 0;

    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private bool isTyping = false;

    

    [SerializeField] InputManager inputManager;
    [SerializeField] Task1NameBirth taskNameBirth;
    [SerializeField] PhonicsGridManager gridManager;
    [SerializeField] CameraController cameraCcontroler;
    [SerializeField] SoundsManager soundsManager;




    private void Start()
    {
        nextButton.onClick.AddListener(OnNextButtonPressed);
        StartDialogue();
    }

    public void StartDialogue()
    {
        currentLineIndex = 0;
        ShowLine();
    }

    void ShowLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);

        typingCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        yield return new WaitForSeconds(0.1f);
        nextButton.gameObject.SetActive(true);
        string line =  eachChat[chatInd].eachDialogue[currentLineIndex].dialogueLine;
        isTyping = true;

        TextMeshPro currentTextfield = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex].actorInd].dialogueTextField;
        GameObject currentDialogueBox = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex].actorInd].actorDialogBox;
        AudioClip currentDialogueClip = eachChat[chatInd].eachDialogue[currentLineIndex].dialogueSoundClip;
        audioSource.clip = currentDialogueClip;
        audioSource.Play();

        currentTextfield.text = "";
        currentDialogueBox.SetActive(true);

        if(cameraCcontroler.gameObject.activeInHierarchy)
        cameraCcontroler.LookAtTarget(currentDialogueBox.transform);

        foreach (char c in line.ToCharArray())
        {
            currentTextfield.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
    }

    IEnumerator AutoAdvanceAfterDelay()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        NextDialogue();
    }

    void OnNextButtonPressed()
    {
        soundsManager.ButtonClicked();
        if (isTyping)
        {
            // Skip typing and show full line instantly
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);


            TextMeshPro currentTextfield = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex].actorInd].dialogueTextField;


            currentTextfield.text = eachChat[chatInd].eachDialogue[currentLineIndex].dialogueLine;
            isTyping = false;

            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);

            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay());
        }
        else
        {
            NextDialogue();
        }
    }

    public void NextDialogue()
    {
        audioSource.Stop();
        currentLineIndex++;
        nextButton.gameObject.SetActive(false);
        if (currentLineIndex < eachChat[chatInd].eachDialogue.Length)
        {
            ShowLine();
            TextMeshPro currentTextfield = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex - 1].actorInd].dialogueTextField;
            GameObject currentDialogueBox = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex - 1].actorInd].actorDialogBox;


            currentTextfield.text = "";
            currentDialogueBox.SetActive(false);
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        TextMeshPro currentTextfield = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex-1].actorInd].dialogueTextField;
        GameObject currentDialogueBox = eachActor[eachChat[chatInd].eachDialogue[currentLineIndex-1].actorInd].actorDialogBox;

        if (cameraCcontroler.gameObject.activeInHierarchy)
            cameraCcontroler.GoBack();

        currentTextfield.text = "";
        nextButton.gameObject.SetActive(false);
        currentDialogueBox.SetActive(false);
        currentLineIndex = 0;

        PerformTask();
        // You can trigger other actions here
    }
    public void PerformTask()
    {
        if(taskInd==0)
        {
            TaskDone();
            //taskNameBirth.ShowTaskScreen();
        }
      else if (taskInd==1)
        {
            if (lattersTaskInd == 0 || lattersTaskInd == 1)
            {
                inputManager.StartTask("a");
                Debug.Log("test  1");
            }

            else if (lattersTaskInd == 2 || lattersTaskInd == 3)
                inputManager.StartTask("t");

            else if (lattersTaskInd == 4 || lattersTaskInd == 5)
                inputManager.StartTask("k");

            else if (lattersTaskInd == 6 || lattersTaskInd == 7)
                inputManager.StartTask("m");

            else if (lattersTaskInd == 8 || lattersTaskInd == 9)
                inputManager.StartTask("n");

            else if (lattersTaskInd == 10 || lattersTaskInd == 11)
                inputManager.StartTask("l");

            else if (lattersTaskInd == 12 || lattersTaskInd == 13)
                inputManager.StartTask("h");

            else if (lattersTaskInd == 14 || lattersTaskInd == 15)
                inputManager.StartTask("e");

            else if (lattersTaskInd == 16 || lattersTaskInd == 17)
                inputManager.StartTask("i");

           else if (lattersTaskInd == 18)
            {
                FindObjectOfType<VanMovement>().StartVanMovement();
                taskInd++;
            }

        }

        else if(taskInd==2)
        {
            if (lattersTaskInd >= 19)
            {
                Debug.Log("check");
                FindObjectOfType<Chapter1JoinManager>().StartNewLine();
            }
        }
        
    }
    public void TaskDone()
    {
        if(taskInd==0)
        {
            task2Objects.SetActive(true);
            task1Objects.SetActive(false);
            taskInd++;
            chatInd++;
            StartDialogue();
        }
        else if(taskInd==1)
        {
            lattersTaskInd++;
            
            
                StartDialogue();
                chatInd++;
            
        }
        else if (taskInd==2)
        {
            lattersTaskInd++;
            chatInd++;
            gridManager.ResetRotationOfAllBlocks();
            StartDialogue();
        }
    }
    
    public void OnClickGoToHomeButton()
    {
        soundsManager.ButtonClicked();
        SceneManager.LoadSceneAsync(0);
    }

    
}
[System.Serializable]
public class Chat
{
    public Dialogue[] eachDialogue;
}

[System.Serializable]
public class Dialogue
{
    public int actorInd;
    [Header("Dialogue Settings")]
    [TextArea(3, 10)]
    public string dialogueLine;
    public AudioClip dialogueSoundClip;
}
[System.Serializable]
public class ActorDetail
{
    public string actorName;
    public Transform actor;
    public GameObject actorDialogBox;
    [Header("UI References")]
    public TextMeshPro dialogueTextField;
}
