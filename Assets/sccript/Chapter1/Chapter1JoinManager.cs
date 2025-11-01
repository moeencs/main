using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Chapter1JoinManager : MonoBehaviour
{
    [SerializeField] ActorDetail[] eachActor;  //1 kitten, 2 for puppy , 3 for robbot
    [SerializeField] Transform actorsParent;

    [SerializeField] Transform blocksParent;
    [SerializeField] List<GameObject> currentBlocks = new List<GameObject>();
    [SerializeField] List<GameObject> newList = new List<GameObject>();

    [SerializeField] List<Joining> eachJoining;
    [SerializeField] AudioSource audioSource;

    [SerializeField] TopPosition[] topPosition; //top position depends on the length of word 2, 3 or 4

    [SerializeField] WheelArranger wheelmanager;
    [SerializeField] SoundsManager soundsManager;

    int currentInd = 0;
    [SerializeField] GameObject TempObj;

    public void StartNewLine()
    {
        if (currentInd < eachJoining.Count)
        {
            
            StartCoroutine(StartAnim());
        }
    }

    IEnumerator StartAnim()
    {
        int waitCount = -1;
        int latterCount = -1;
        newList.Clear();
        StartTypingAndAudioClip();

       // yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);
        if (eachJoining[currentInd].joiningNeeded)
        {

            //remove latter from top which are not used  in  this word
            foreach (GameObject eachBlock in currentBlocks)
            {
                string topLatter = eachBlock.GetComponent<LetterBlock>().letter.ToLower();
                if (!eachJoining[currentInd].word.Contains(topLatter))
                {
                    eachBlock.GetComponent<LetterBlock>().BringBackToBottom();
                }
            }
            waitCount++;
            //add blocks to current blocks list
            foreach (char eachLatter in eachJoining[currentInd].word)
            {
                yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);
                waitCount++;

                latterCount++;
                GameObject go = new GameObject();
                go = null;
                string currentLatter = eachLatter.ToString().ToLower();
                if (eachJoining[currentInd].isLastword && latterCount == eachJoining[currentInd].word.Length - 1)
                {
                    Debug.Log(currentLatter + " last");
                    go = Instantiate(newList[newList.Count - 1]);
                    TempObj = go;
                    go.transform.parent = newList[newList.Count - 1].transform.parent;

                    Vector3 newPos = topPosition[eachJoining[currentInd].word.Length - 2].topPos[latterCount];

                    go.gameObject.GetComponent<LetterBlock>().SetValueCustom(newList[newList.Count - 1].GetComponent<LetterBlock>().GetScale(),
                        newList[newList.Count - 1].GetComponent<LetterBlock>().GetRot(),
                        newList[newList.Count - 1].GetComponent<LetterBlock>().GetPos());

                    go.gameObject.GetComponent<LetterBlock>().SetTargetValues(newList[newList.Count - 1].GetComponent<LetterBlock>().GetTScale(),
                        newList[newList.Count - 1].GetComponent<LetterBlock>().GetTRot(),
                        newList[newList.Count - 1].GetComponent<LetterBlock>().GetTPos());

                    go.transform.localPosition = newPos;
                    go.transform.localRotation = newList[newList.Count - 1].transform.localRotation;

                    
                    newList.Add(go);
                   // go.GetComponent<LetterBlock>().BringOnTop(newPos);

                }

                else
                {
                        go = CheckAlreadyThere(currentLatter);
                    if (go != null)
                    {
                        newList.Add(go);
                        Vector3 newPos = topPosition[eachJoining[currentInd].word.Length - 2].topPos[latterCount];
                        go.GetComponent<LetterBlock>().SwapPos(newPos);
                    }
                    else
                    {

                        go = GetFromBottom(currentLatter);
                        newList.Add(go);
                        Vector3 newPos = topPosition[eachJoining[currentInd].word.Length - 2].topPos[latterCount];
                        go.GetComponent<LetterBlock>().BringOnTop(newPos);
                    }
                }
            }


            //fill  new  list  on current blocks  array
            currentBlocks.Clear();
            yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);
            FillNewListInCurrentList();
            waitCount++;
            yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);

            //join now words
            latterCount = -1;
            soundsManager.JoinCube();
            foreach (GameObject blocks in currentBlocks)
            {
                latterCount++;
                Vector3 newPos = topPosition[eachJoining[currentInd].word.Length - 2].joiningPos[latterCount];
                blocks.GetComponent<LetterBlock>().SwapPos(newPos);
            }
            waitCount++;
            yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);

        }
        else
        {
            waitCount++;
            yield return new WaitForSeconds(eachJoining[currentInd].waitTimers[waitCount]);
        }

        eachActor[eachJoining[currentInd].actorInd].actorDialogBox.SetActive(false);
        EndAnim();
    }

    public void PlayAnimationLast()
    {
        actorsParent.GetComponent<Animator>().SetTrigger("run");

        foreach(ActorDetail actor in eachActor)
        {
            actor.actor.gameObject.GetComponent<Animator>().SetTrigger("run");
        }
    }
    public void FillNewListInCurrentList()
    {
       

        foreach(GameObject eachBlock in newList)
        {
            currentBlocks.Add(eachBlock);
        }
        //newList.Clear();

    }
    GameObject CheckAlreadyThere(string latter)
    {
        foreach(GameObject eachLatter in currentBlocks)
        {
            if(eachLatter.GetComponent<LetterBlock>().letter.ToLower()==latter)
            {
                return eachLatter;
            }

        }
        return null;
    }
    GameObject GetFromBottom(string latter)
    {
        foreach (Transform eachLatter in blocksParent)
        {
            if (eachLatter.GetComponent<LetterBlock>().letter.ToLower() == latter)
            {
                return eachLatter.gameObject;
            }

        }
        return null;
    }

    public void StartTypingAndAudioClip()
    {
        if(currentInd!=0)
            eachActor[eachJoining[currentInd-1].actorInd].actorDialogBox.SetActive(false);


        if (eachJoining[currentInd].clip != null)
        {
            AudioClip currentDialogueClip = eachJoining[currentInd].clip;
            audioSource.clip = currentDialogueClip;
            audioSource.Play();
        }

        eachActor[eachJoining[currentInd].actorInd].actorDialogBox.SetActive(true);
        eachActor[eachJoining[currentInd].actorInd].dialogueTextField.text = eachJoining[currentInd].typleLine;
    }
    public void EndAnim()
    {
        currentInd++;
        
        if (currentInd < eachJoining.Count)
        {
            StartNewLine();
        }
        else {
            Debug.Log("game end");
            foreach (Transform eachBlock in blocksParent)
            {
                eachBlock.gameObject.GetComponent<LetterBlock>().BringBackToBottom();
                eachBlock.gameObject.GetComponent<LetterBlock>().enabled = false;
               
            }
            if(TempObj!=null)
                Destroy(TempObj);
            currentBlocks.Clear();
            PlayAnimationLast();
            wheelmanager.CreateWheelAndSpin();
                }
;    }
}
[System.Serializable]
public class Joining
{
    public string word;
    [Header("Dialogue Settings")]
    [TextArea(3, 10)]
    public string typleLine;
    
    public AudioClip clip;
    public int actorInd;
    public bool joiningNeeded;
    public bool isLastword = false;
    
    public float[] waitTimers;
}

[System.Serializable]
public class TopPosition
{
    public Vector3[] topPos;
    public Vector3[] joiningPos;
}
