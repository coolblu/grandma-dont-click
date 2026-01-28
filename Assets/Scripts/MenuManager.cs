using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void PlayButton()
    {
        audioSource.Play();
        SceneManager.LoadScene("Main");
    }

    public void CreditButton()
    {
        audioSource.Play();
        SceneManager.LoadScene("Credits");
    }

    public void MenuButton()
    {
        SceneManager.LoadScene("Menu");
    }
}
