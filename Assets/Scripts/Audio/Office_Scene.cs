using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static PhoneKeypadUI;

public class Office_Scene : MonoBehaviour
{
    public AudioSource audio;
    public AudioClip RingingClip;
    public AudioClip ScamClip;
    public PhoneKeypadUI keypad;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start the coroutine when the object starts
        StartCoroutine(ExecuteAfterTime(2.0f));
    }

    void Update()
    {
        if (keypad.IsOpen && audio.clip == RingingClip)
        {
            audio.Stop();
            StartCoroutine(ScamCall());
        }
    }

    IEnumerator ExecuteAfterTime(float delay)
    {
        // Wait for the specified number of seconds
        yield return new WaitForSeconds(delay);

        // Code here will execute after the delay
        audio.clip = RingingClip;
        audio.loop = true;
        audio.Play();
    }

    IEnumerator ScamCall ()
    {
        yield return null;
        audio.clip = ScamClip;
        audio.loop = false;
        audio.Play();
    }

}