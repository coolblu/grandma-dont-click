using UnityEngine;
using System.Collections;
using static FirstPersonController;

public class Office_Scene : MonoBehaviour
{
    [Header("Scene Audio")]
    public AudioSource audioSource;
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
        if (keypad.IsOpen && audioSource.clip == RingingClip)
        {
            audioSource.Stop();
            StartCoroutine(ScamCall());
        }
    }

    // -----------------------------
    // FOOTSTEP LOGIC
    // -----------------------------
    void HandleFootsteps()
    {
        var p = controller.transform.position;
        float speed = (p - lastPosition).magnitude / Time.deltaTime;
        lastPosition = p;

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

        audioSource.clip = RingingClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    IEnumerator ScamCall()
    {
        yield return null;

        audioSource.clip = ScamClip;
        audioSource.loop = false;
        audioSource.Play();
    }
}
