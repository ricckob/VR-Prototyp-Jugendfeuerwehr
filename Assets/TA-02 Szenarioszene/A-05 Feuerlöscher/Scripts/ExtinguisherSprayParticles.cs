using UnityEngine;

// Partikel und Sound vom Sprühstrahl
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class ExtinguisherSprayParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem sprayParticleSystem;
    [SerializeField] private AudioSource sprayAudioSource;
    [Range(0f, 1f)]
    [SerializeField] private float sprayVolume = 1f;

    private bool isPlaying;

    private void Awake()
    {
        if (sprayParticleSystem == null)
            sprayParticleSystem = GetComponent<ParticleSystem>();

        if (sprayAudioSource == null)
            sprayAudioSource = GetComponent<AudioSource>();

        sprayAudioSource.loop = true;
        sprayAudioSource.Stop();
        sprayParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // strength = Hebelstellung 0..1, bestimmt die Lautstärke
    public void Play(float strength)
    {
        if (!isPlaying)
        {
            isPlaying = true;
            sprayParticleSystem.Play(true);
            sprayAudioSource.Play();
        }

        sprayAudioSource.volume = sprayVolume * Mathf.Clamp01(strength);
    }

    public void Stop()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        sprayParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        sprayAudioSource.Stop();
    }
}
