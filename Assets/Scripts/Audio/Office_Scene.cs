using UnityEngine;
using System.Collections;
using static FirstPersonController;

public class Office_Scene : MonoBehaviour
{
    [Header("Scene Audio")]
    public AudioSource audio;
    public AudioClip RingingClip;
    public AudioClip ScamClip;
    public PhoneKeypadUI keypad;

    [Header("Footsteps")]
    public FirstPersonController controller;
    public AudioSource footsteps;
    public float minSpeed = 0.1f;

    private Vector3 lastPosition;

    void Start()
    {
        StartCoroutine(ExecuteAfterTime(2.0f));
        lastPosition = transform.position;
    }

    void Update()
    {
        HandlePhoneAudio();
        HandleFootsteps();
    }

    // -----------------------------
    // PHONE AUDIO LOGIC
    // -----------------------------
    void HandlePhoneAudio()
    {
        if (keypad.IsOpen && audio.clip == RingingClip)
        {
            audio.Stop();
            StartCoroutine(ScamCall());
        }
    }

    // -----------------------------
    // FOOTSTEP LOGIC
    // -----------------------------
    void HandleFootsteps()
    {
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (speed > minSpeed && controller.IsGrounded)
        {
            if (!footsteps.isPlaying)
                footsteps.Play();
        }
        else
        {
            if (footsteps.isPlaying)
                footsteps.Stop();
        }
    }

    // -----------------------------
    // COROUTINES
    // -----------------------------
    IEnumerator ExecuteAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);

        audio.clip = RingingClip;
        audio.loop = true;
        audio.Play();
    }

    IEnumerator ScamCall()
    {
        yield return null;

        audio.clip = ScamClip;
        audio.loop = false;
        audio.Play();
    }
}
