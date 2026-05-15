using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health            playerHealth;
    [SerializeField] private GunSystem         gunSystem;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private LureSystem        lureSystem;
    [Header("Crosshair")]
    [SerializeField] private float crosshairSize  = 5f;
    [SerializeField] private float crosshairGap   = 6f;
    [SerializeField] private float crosshairThick = 2f;
    [Header("Scaling")]
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private float hudScale        = 1f;
    private float Scale => (Screen.height / referenceHeight) * hudScale;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _ammoStyle;
    private GUIStyle _warnStyle;


    private void OnGUI()
    {
        BuildStyles();
        DrawPlayerPanel();
        DrawCrosshair();
    }

    private void BuildStyles()
    {
        float s = Scale;
        _headerStyle = new GUIStyle
        {
            fontSize  = Mathf.RoundToInt(10 * s),
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        _labelStyle = new GUIStyle
        {
            fontSize  = Mathf.RoundToInt(13 * s),
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
        _ammoStyle = new GUIStyle
        {
            fontSize  = Mathf.RoundToInt(20 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperRight,
            normal    = { textColor = Color.white }
        };
        _warnStyle = new GUIStyle
        {
            fontSize  = Mathf.RoundToInt(10 * s),
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.35f, 0.35f) }
        };
    }

    private void DrawPlayerPanel()
    {
        float s      = Scale;
        float panelW = 240f * s;
        float panelH = 120f * s;
        float halfW  = panelW / 2f;
        float halfH  = panelH / 2f;
        float x      = 15f  * s;
        float y      = Screen.height - panelH - 15f * s;
        float pad    = 9f   * s;

        // Background
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(x + halfW, y, 1f * s, panelH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + halfH, panelW, 1f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float qW = halfW - pad * 2f;  

        // Health
        float tlX = x + pad;
        float tlY = y + pad;

        GUI.Label(new Rect(tlX, tlY, qW, 14f * s), "HEALTH", _headerStyle);

        if (playerHealth != null)
        {
            float hp = playerHealth.NormalisedHealth;
            DrawBar(tlX, tlY + 16f * s, qW, 9f * s, hp, HealthColor(hp));
            GUI.Label(new Rect(tlX, tlY + 29f * s, qW, 18f * s),
                $"{hp * 100f:0} / 100", _labelStyle);
        }

        //  Ammo 
        float trX = x + halfW + pad;
        float trY = y + pad;

        GUI.Label(new Rect(trX, trY, qW, 14f * s), "AMMO", _headerStyle);

        if (gunSystem != null)
        {
            if (gunSystem.IsReloading)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(trX, trY + 14f * s, qW, 20f * s), "RELOADING", _labelStyle);
                GUI.color = Color.white;
            }
            else
            {
                GUI.Label(new Rect(trX, trY + 10f * s, qW, 26f * s),
                    $"{gunSystem.CurrentMagazine}", _ammoStyle);

                GUIStyle reserveStyle = new GUIStyle(_headerStyle)
                {
                    alignment = TextAnchor.UpperRight,
                    normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                GUI.Label(new Rect(trX, trY + 36f * s, qW, 14f * s),
                    $"/ {gunSystem.TotalAmmo}", reserveStyle);
            }

            if (!gunSystem.IsReloading && gunSystem.CurrentMagazine <= 5)
            {
                GUI.color = new Color(1f, 0.3f, 0.3f, Mathf.PingPong(Time.time * 2f, 1f));
                GUI.Label(new Rect(trX, trY + 50f * s, qW, 14f * s), "LOW AMMO", _warnStyle);
                GUI.color = Color.white;
            }
        }

        //Supressor 
        float blX = x + pad;
        float blY = y + halfH + pad;

        GUI.Label(new Rect(blX, blY, qW, 14f * s), "SUPPRESSOR", _headerStyle);

        if (gunSystem != null)
        {
            int   charges = gunSystem.SuppressorCharges;
            int   maxC    = gunSystem.MaxSuppressorCharges;
            float ratio   = (float)charges / maxC;

            if (charges <= 0)
            {
                GUI.color = new Color(1f, 0.3f, 0.3f, Mathf.PingPong(Time.time * 2f, 1f));
                GUI.Label(new Rect(blX, blY + 16f * s, qW, 18f * s), "DEPLETED", _labelStyle);
                GUI.color = Color.white;
            }
            else
            {
                Color barCol = charges > 10
                    ? new Color(0f, 0.85f, 1f)
                    : new Color(1f, 0.55f, 0f);

                DrawBar(blX, blY + 16f * s, qW, 9f * s, ratio, barCol);
                GUI.color = new Color(0.65f, 0.65f, 0.65f);
                GUI.Label(new Rect(blX, blY + 29f * s, qW, 14f * s),
                    $"{charges} / {maxC}", _headerStyle);
                GUI.color = Color.white;
            }
        }

        //  Lure
        float brX = x + halfW + pad;
        float brY = y + halfH + pad;

        GUI.Label(new Rect(brX, brY, qW, 14f * s), "LURES", _headerStyle);

        if (lureSystem != null)
        {
            int   lures    = lureSystem.CurrentLures;
            int   maxLures = lureSystem.MaxLures;
            float dotSize  = 12f * s;
            float dotGap   = 16f * s;
            for (int i = 0; i < maxLures; i++)
            {

                GUI.color = i < lures
                    ? new Color(0.2f, 1f, 0.4f)
                    : new Color(0.22f, 0.22f, 0.22f);
                GUI.DrawTexture(
                    new Rect(brX + i * dotGap, brY + 16f * s, dotSize, dotSize),
                    Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUI.Label(new Rect(brX, brY + 30f * s, qW, 14f * s),
                lures <= 0 ? "NONE LEFT" : $"{lures} / {maxLures}", _headerStyle);
            GUI.color = Color.white;


            if (lures <= 0)
            {
                GUI.color = new Color(1f, 0.3f, 0.3f, Mathf.PingPong(Time.time * 2f, 1f));
                GUI.Label(new Rect(brX, brY + 30f * s, qW, 14f * s), "NONE LEFT", _headerStyle);
                GUI.color = Color.white;
            }
        }
    }

    private void DrawCrosshair()
    {
        if (thirdPersonCamera == null || !thirdPersonCamera.IsAiming) return;

        float s  = Scale;
        float cx = Screen.width  / 2f;
        float cy = Screen.height / 2f;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        DrawCrosshairLines(cx + 1, cy + 1, crosshairSize * s, crosshairGap * s, crosshairThick * s);
        GUI.color = new Color(1f, 1f, 1f, 0.92f);
        DrawCrosshairLines(cx, cy, crosshairSize * s, crosshairGap * s, crosshairThick * s);
        GUI.color = Color.white;
    }

    private void DrawCrosshairLines(float cx, float cy, float s, float g, float t)
    {
        GUI.DrawTexture(new Rect(cx - t / 2f, cy - g - s, t, s), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - t / 2f, cy + g,     t, s), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - g - s,  cy - t / 2f, s, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + g,      cy - t / 2f, s, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1.5f * Scale, cy - 1.5f * Scale,
            3f * Scale, 3f * Scale), Texture2D.whiteTexture);
    }


    private void DrawBar(float x, float y, float w, float h, float value, Color fill)
    {
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(value), h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }


    private Color HealthColor(float v) =>
        v > 0.6f ? Color.green : v > 0.3f ? Color.yellow : Color.red;
}