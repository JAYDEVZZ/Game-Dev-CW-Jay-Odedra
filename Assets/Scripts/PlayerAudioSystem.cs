using UnityEngine;

public class PlayerAudioSystem : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;         // 3-4 variations
    [SerializeField] private float       walkVolume      = 0.5f;
    [SerializeField] private float       runVolume       = 0.8f;
    [SerializeField] private Vector2     walkPitchRange  = new Vector2(0.88f, 1.08f);
    [SerializeField] private Vector2     runPitchRange   = new Vector2(1.0f,  1.15f);

    [Header("Weapon")]
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private AudioClip suppressedClip;
    [SerializeField] private AudioClip reloadClip;

    [Header("Lure")]
    [SerializeField] private AudioClip lureThrowClip;
    [SerializeField] private AudioClip lureLandClip;

    // separate sources so sounds never cut each other off
    private AudioSource _footstepSource;
    private AudioSource _weaponSource;
    private AudioSource _lureSource;

    private void Awake()
    {
        _footstepSource = MakeSource(1f);
        _weaponSource   = MakeSource(1f);
        _lureSource     = MakeSource(0.8f);
    }


    private AudioSource MakeSource(float volume)
    {
        AudioSource s    = gameObject.AddComponent<AudioSource>();
        s.playOnAwake    = false;
        s.loop           = false;
        s.spatialBlend   = 0f; 
        s.volume         = volume;
        return s;
    }

    //  Footsteps 

    public void PlayFootstep(bool isRunning)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        // Picks a random clip for footsetep variety

        AudioClip clip     = footstepClips[Random.Range(0, footstepClips.Length)];
        Vector2   pitchRng = isRunning ? runPitchRange : walkPitchRange;

        _footstepSource.pitch  = Random.Range(pitchRng.x, pitchRng.y);
        _footstepSource.volume = isRunning ? runVolume : walkVolume;
        _footstepSource.PlayOneShot(clip);
    }

    // weapons

    public void PlayGunshot(bool suppressed)
    {
        AudioClip clip = suppressed ? suppressedClip : gunshotClip;
        if (clip == null) return;
        _weaponSource.pitch = Random.Range(0.95f, 1.05f);
        _weaponSource.PlayOneShot(clip, suppressed ? 0.55f : 1f);
    }

    public void PlayReload()
    {
        if (reloadClip == null) return;
        _weaponSource.PlayOneShot(reloadClip, 0.8f);
    }

    // - Lures

    public void PlayLureThrow()
    {
        if (lureThrowClip == null) return;
        _lureSource.PlayOneShot(lureThrowClip);
    }

    public void PlayLureLand()
    {
        if (lureLandClip == null) return;
        _lureSource.PlayOneShot(lureLandClip, 0.6f);
    }
}