using UnityEngine;

public class PlayJumpSounds : MonoBehaviour
{
    public AudioClip[] jumpClips;

    [SerializeField]
    private float footstepVolume = 0.15f;

    public void PlayTakeOffSound()
    {
       
        AudioClip selectedClip = jumpClips[0];

        if (!SoundManager.Instance.audioSource.isPlaying)
        {
          
          
            SoundManager.Instance.PlayFootstepSound(selectedClip, footstepVolume);
            Debug.LogWarning(selectedClip + "Is Playing");
        }

    }

    public void PlayLandSound()
    {

        AudioClip selectedClip = jumpClips[1];

        if (!SoundManager.Instance.audioSource.isPlaying)
        {
           
            SoundManager.Instance.PlayFootstepSound(selectedClip, footstepVolume);
            Debug.LogWarning(selectedClip + "Is Playing");
        }

    }
}
