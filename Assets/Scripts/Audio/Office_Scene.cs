using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static PhoneKeypadUI;

public class Office_Scene : MonoBehaviour
{
    public AudioSource audio;
    public AudioClip clip;
    public PhoneKeypadUI keypad;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start the coroutine when the object starts
        StartCoroutine(ExecuteAfterTime(2.0f));
    }

    IEnumerator ExecuteAfterTime(float delay)
    {
        // Wait for the specified number of seconds
        yield return new WaitForSeconds(delay);

        // Code here will execute after the delay
        audio.clip = clip;
        audio.loop = true;
        audio.Play();
    }

    void Update()
    {
        if (keypad.IsOpen)
        {
            audio.Stop();
        }
    }

}