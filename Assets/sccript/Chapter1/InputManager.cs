using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class InputManager : MonoBehaviour
{
    string currentLatter;
    Transform selectedBlock=null;
    Transform targetPosition=null;

    [SerializeField] Transform[] targetPos;
    bool allowTap;
    int latterInd =0;


    [Header("Camera Reference")]
    public Camera mainCamera; // Assign your Cinemachine TPS Camera here
    [SerializeField] Chapter1Mannager chap1Manager;
    [SerializeField] SoundsManager soundsManager;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main; // Fallback
    }
    public void StartTask(string  currentLaterN)
    {
        currentLatter = currentLaterN;
        AllowTap();
    }
    public void AllowTap()
    {
        if(selectedBlock==null)
        targetPosition = targetPos[latterInd];

        allowTap = true;
       
    }

    void Update()
    {
        if (allowTap == false)
            return;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPosition = Pointer.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (selectedBlock == null)
                {
                    LetterBlock block = hit.collider.GetComponent<LetterBlock>();
                    if (block != null)
                    {
                        if (block.GetStatus() == false)
                            return;
                       
                        if (block.letter.ToLower() == currentLatter)
                        {
                            soundsManager.CorrectTapped();
                            block.OnSelected();
                            selectedBlock = block.gameObject.GetComponent<Transform>();
                            targetPosition.gameObject.SetActive(true);
                            chap1Manager.TaskDone();
                            allowTap = false;
                        }
                        else
                        {
                            soundsManager.WrongTapped();
                            block.Shake();
                        }
                    }
                }
                else
                {
                    if(hit.collider.gameObject.tag=="target")
                    {
                        soundsManager.CorrectTapped();
                        targetPosition = hit.collider.transform;

                       // selectedBlock.gameObject.GetComponent<LetterBlock>().ResetBlock();
                        selectedBlock.gameObject.GetComponent<LetterBlock>().Disable(targetPosition.gameObject.transform.localPosition);
                        targetPosition.gameObject.SetActive(false);
                        //selectedBlock.gameObject.transform.position = targetPosition.gameObject.transform.position;
                        targetPosition = null;
                        selectedBlock = null;
                        allowTap = false;
                        latterInd++;
                        chap1Manager.TaskDone();
                       
                    }
                }
            }
        }
    }

}

