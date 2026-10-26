using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CustomerFootsteps : MonoBehaviour
{
    public AudioClip[] footsteps;           
    public float stepInterval = 0.5f;       
    public float minWalkSpeed = 0.3f;       
    [Range(0f, 1f)] public float volume = 0.5f;

    AudioSource src;
    Vector3 lastPos;
    float timer;

    void Start()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;   
        lastPos = transform.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 move = transform.position - lastPos;
        move.y = 0f;
        float speed = move.magnitude / dt;
        lastPos = transform.position;

        
        if (speed > minWalkSpeed && speed < 5f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                if (footsteps != null && footsteps.Length > 0)
                    src.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)], volume);
                timer = stepInterval;
            }
        }
        else
        {
            timer = 0f; 
        }
    }
}