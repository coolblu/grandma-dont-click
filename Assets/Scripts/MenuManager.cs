using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public AudioSource audio;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void PlayButton()
    {
        audio.Play();
        SceneManager.LoadScene("Main");
    }

    public void CreditButton()
    {
        audio.Play();
        SceneManager.LoadScene("Credits");
    }
}
