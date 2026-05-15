using UnityEngine;

public class DamageVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;

    [Header("Vignette Settings")]
    [SerializeField] private float fadeDuration = 1f;    // how long the fade takes
    [SerializeField] private float peakAlpha = 0.5f;    // max red opacity on hit
    [SerializeField] private float vignetteWidth = 0.18f; // border size as fraction of screen

    private float _alpha = 0f;
    private float _fadeTimer = 0f;

    private void Start()
    {
        if (playerHealth != null)
            playerHealth.onDamageTaken.AddListener(OnDamageTaken);
    }

    private void OnDamageTaken(float normalisedHealth)
    {
        _alpha = peakAlpha;
        _fadeTimer = fadeDuration;
    }

    private void Update()
    {
        if (_fadeTimer <= 0f) return;

        _fadeTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
        _alpha = peakAlpha * (t * t); // t*t gives a nicer ease-out than linear

        if (_fadeTimer <= 0f) _alpha = 0f;
    }

    private void OnGUI()
    {
        if (_alpha <= 0.005f) return;

        float w = Screen.width;
        float h = Screen.height;
        float bx = w * vignetteWidth;
        float by = h * vignetteWidth;

        // three layers to fake an inward gradient
        DrawEdges(_alpha, 0f, 0f, w, h, bx, by);
        DrawEdges(_alpha * 0.5f, bx * 0.4f, by * 0.4f, w, h, bx * 0.6f, by * 0.6f);
        DrawEdges(_alpha * 0.2f, bx * 0.7f, by * 0.7f, w, h, bx * 0.3f, by * 0.3f);
    }
    

    private void DrawEdges(float alpha, float ix, float iy, float w, float h, float bx, float by)
    {
        GUI.color = new Color(1f, 0f, 0f, alpha);

        // Top
        GUI.DrawTexture(new Rect(ix, iy, w - ix * 2f, by), Texture2D.whiteTexture);
        // Bottom
        GUI.DrawTexture(new Rect(ix, h - iy - by, w - ix * 2f, by), Texture2D.whiteTexture);
        // Left
        GUI.DrawTexture(new Rect(ix, iy + by, bx, h - iy * 2f - by * 2f), Texture2D.whiteTexture);
        // Right
        GUI.DrawTexture(new Rect(w - ix - bx, iy + by, bx, h - iy * 2f - by * 2f), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }
}