using UnityEngine;

public class AIAudioSystem : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip[] suspiciousClips;
    [SerializeField] private AudioClip[] combatClips;
    [SerializeField] private AudioClip gunshotClip;

    [Header("Settings")]
    [SerializeField] private float minTimeBetweenVoiceSounds = 4f;

    private AudioSource _source;
    private float _lastVoicePlayTime = -999f;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.spatialBlend = 1f;
        _source.maxDistance = 25f;
        _source.rolloffMode = AudioRolloffMode.Linear;
        _source.playOnAwake = false;
    }

    public void PlaySuspicious()
    {
        if (Time.time - _lastVoicePlayTime < minTimeBetweenVoiceSounds) return;
        PlayRandom(suspiciousClips, 0.7f);
    }

    public void PlayCombat()
    {
        PlayRandom(combatClips, 1f);
    }

    public void PlayGunshot()
    {
        if (gunshotClip == null) return;
        // Slight pitch variation per AI so multiple guards shooting
        // doesn't sound like one repeated sample
        _source.pitch = Random.Range(0.92f, 1.08f);
        _source.PlayOneShot(gunshotClip, 1f);
    }

    // Called by Mixamo animation events — left empty intentionally
    public void PlayFootstep() { }

    private void PlayRandom(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        _source.pitch = Random.Range(0.9f, 1.1f);
        _source.PlayOneShot(clip, volume);
        _lastVoicePlayTime = Time.time;
    }
}