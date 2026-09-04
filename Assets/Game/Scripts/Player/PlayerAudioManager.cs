using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private AudioSource _footstepSFX;

    [Header("Punch")]
    [SerializeField] private AudioSource _punchSFX;

    [Header("Landing")]
    [SerializeField] private AudioSource _landingSFX;

    [Header("Glide")]
    [SerializeField] private AudioSource _glideSFX;

    public void PlayGlideSFX()
    {
        _glideSFX.Play();
    }

    public void StopGlideSFX()
    {
        _glideSFX.Stop();
    }

    private void PlayFootstepSFX()
    {
        PlayOneShot(_footstepSFX);
    }

    private void PlayPunchSFX()
    {
        PlayOneShot(_punchSFX);
    }

    private void PlayLandingSFX()
    {
        PlayOneShot(_landingSFX);
    }

    private void PlayOneShot(AudioSource source)
    {
        source.volume = Random.Range(0.7f, 1f);
        source.pitch = Random.Range(0.5f, 2.5f);
        source.PlayOneShot(source.clip);
    }
}
