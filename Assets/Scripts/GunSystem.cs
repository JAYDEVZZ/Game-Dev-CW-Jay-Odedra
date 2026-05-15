using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunSystem : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private float     damage   = 25f;
    [SerializeField] private float     range    = 100f;
    [SerializeField] private float     fireRate = 0.15f;
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private LayerMask ignoreLayer;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Header("Ammo")]
    [SerializeField] private int   magazineSize = 30;
    [SerializeField] private int   totalAmmo    = 100;
    [SerializeField] private float reloadTime   = 1.5f;

    [Header("Suppressor")]
    [SerializeField] private int maxSuppressorCharges = 30;

    [Header("Gunshot Sound")]
    [SerializeField] private float unsuppressedRadius    = 30f;
    [SerializeField] private float unsuppressedDetection = 1f;
    [SerializeField] private float suppressedRadius      = 6f;
    [SerializeField] private float suppressedDetection   = 0.35f;

    private int   _currentMagazine;
    private int   _suppressorCharges;
    private bool  _isReloading;
    private float _nextFireTime;
    private PlayerAudioSystem _audio;

    public int  CurrentMagazine      => _currentMagazine;
    public int  TotalAmmo            => totalAmmo;
    public bool IsReloading          => _isReloading;
    public int  SuppressorCharges    => _suppressorCharges;
    public int  MaxSuppressorCharges => maxSuppressorCharges;
    public bool IsSuppressed         => _suppressorCharges > 0;

    private void Awake()
    {
        _currentMagazine   = magazineSize;
        _suppressorCharges = maxSuppressorCharges;

        _audio = GetComponent<PlayerAudioSystem>()
              ?? GetComponentInParent<PlayerAudioSystem>()
              ?? GetComponentInChildren<PlayerAudioSystem>();
    }

    private void Update()
    {
        if (_isReloading) return;

        if (Mouse.current.leftButton.isPressed && Time.time >= _nextFireTime)
            TryShoot();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            TryReload();
    }

    private void TryShoot()
    {
        if (_currentMagazine <= 0) { TryReload(); return; }

        _currentMagazine--;
        _nextFireTime = Time.time + fireRate;

        bool shotWasSuppressed = _suppressorCharges > 0;
        if (shotWasSuppressed) _suppressorCharges--;

        _audio?.PlayGunshot(shotWasSuppressed);

        Ray     ray       = Camera.main.ScreenPointToRay(
                                new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Vector3 origin    = ray.origin;
        Vector3 direction = ray.direction;

        if (muzzleFlashPrefab != null && gunBarrel != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, gunBarrel.position, gunBarrel.rotation);
            Destroy(flash, 0.1f);
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~ignoreLayer))
        {
            Health target = hit.collider.GetComponentInParent<Health>();
            if (target != null) target.TakeDamage(damage);
        }

        float   shotRadius    = shotWasSuppressed ? suppressedRadius    : unsuppressedRadius;
        float   shotDetection = shotWasSuppressed ? suppressedDetection : unsuppressedDetection;
        Vector3 soundOrigin   = gunBarrel != null ? gunBarrel.position : transform.position;

        SoundManager.EmitSound(soundOrigin, shotRadius, shotDetection);

#if UNITY_EDITOR
        SoundManager.RegisterGizmo(soundOrigin, shotRadius);
#endif
    }

    private void TryReload()
    {
        if (_isReloading)                     return;
        if (_currentMagazine == magazineSize) return;
        if (totalAmmo <= 0)                   return;
        StartCoroutine(Reload());
    }

    

    private IEnumerator Reload()
    {
        _isReloading = true;
        _audio?.PlayReload();
        yield return new WaitForSeconds(reloadTime);

        int needed        = magazineSize - _currentMagazine;
        int taken         = Mathf.Min(needed, totalAmmo);
        _currentMagazine += taken;
        totalAmmo        -= taken;
        _isReloading      = false;
    }

    public void AddAmmo(int amount)  => totalAmmo          += amount;
    public void RefillSuppressor()   => _suppressorCharges  = maxSuppressorCharges;
}