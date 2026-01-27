using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void PlayButton()
    {
        SceneManager.LoadScene("Main");
    }

    public void CreditButton()
    {
        SceneManager.LoadScene("Credits");
    }

    public void MenuButton()
    {
        SceneManager.LoadScene("Menu");
    }
}
