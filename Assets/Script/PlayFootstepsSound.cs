using UnityEngine;

public class PlayFootstepsSound : MonoBehaviour
{

    public AudioClip[] footstepClips;


    [SerializeField]
    private float footstepVolume = 0.15f;

    public void PlaySound()
    {
        int index = Random.Range(0, footstepClips.Length);  
        AudioClip selectedClip = footstepClips[index];

        if (!SoundManager.Instance.audioSource.isPlaying)
        {
           
           
            SoundManager.Instance.PlayFootstepSound(selectedClip, footstepVolume);
            Debug.Log(selectedClip);
        }
            
    }
}
    
