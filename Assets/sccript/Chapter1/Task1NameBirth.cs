using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Task1NameBirth : MonoBehaviour
{
    [SerializeField]
    Chapter1Mannager chapter1Handler;
    [SerializeField] SoundsManager SoundsManager;
    public GameObject TaskScreenMain;
    public GameObject nameBox; 
    public GameObject birthBox;

    public TMP_InputField nameInput;
    public TMP_InputField birtDateInput;
    public TMP_InputField birthYearInput;


    public void ShowTaskScreen()
    {
        TaskScreenMain.SetActive(true);
        nameInput.text = "";
        nameBox.SetActive(true);
        birthBox.SetActive(false);
    }

    public void NameDone()
    {
        SoundsManager.ButtonClicked();
        if(nameInput.text!="")
        {
            nameBox.SetActive(false);
            birthBox.SetActive(true);
        }
    }

    public void  BirthDone()
    {
        if(birtDateInput.text!="" && birthYearInput.text!="")
        {
            birthBox.SetActive(false);
            TaskScreenMain.SetActive(false);
            chapter1Handler.TaskDone();
        }
    }
}
