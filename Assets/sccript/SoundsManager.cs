using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundsManager : MonoBehaviour
{
    [SerializeField] AudioSource bgSounds;
    [SerializeField] AudioClip buttonTapClip;
    [SerializeField] AudioClip wrongTapClip;
    [SerializeField] AudioClip correctTapClip;
    [SerializeField] AudioClip joinCubeClip;


    // Start is called before the first frame update
    public void ButtonClicked()
    {
        bgSounds.PlayOneShot(buttonTapClip);
    }

    public void WrongTapped()
    {
        bgSounds.PlayOneShot(wrongTapClip);
    }

    public void CorrectTapped()
    {
        bgSounds.PlayOneShot(correctTapClip);
    }
    public void JoinCube()
    {
        bgSounds.PlayOneShot(joinCubeClip);
    }

}
