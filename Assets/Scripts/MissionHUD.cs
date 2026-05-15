using UnityEngine;

public class MissionHUD : MonoBehaviour
{
    [Header("Scaling")]
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private float hudScale        = 1f;

    private float Scale => (Screen.height / referenceHeight) * hudScale;

    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _alertStyle;

    private void Start()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnIntelCollected += OnIntelUpdate;
    }


    private void OnIntelUpdate() { } 

    private void OnGUI()
    {
        if (MissionManager.Instance == null) return;

        BuildStyles();

        float s      = Scale;
        float panelW = 180f * s;
        float panelH = 70f  * s;
        float x      = Screen.width - panelW - 15f * s;
        float y      = 15f  * s;
        float pad    = 10f  * s;

        int  collected = MissionManager.Instance.CollectedIntel;
        int  total     = MissionManager.Instance.TotalIntel;
        bool allDone   = MissionManager.Instance.AllIntelCollected;

        // Panel background

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(x, y, panelW, 1f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Intel label
        GUI.Label(new Rect(x + pad, y + 8f * s, panelW, 14f * s), "INTEL", _headerStyle);

        // Dots -- one per intel item
        float dotSize = 14f * s;
        float dotGap  = 20f * s;
        float dotY    = y + 26f * s;
        float dotStartX = x + pad;

        for (int i = 0; i < total; i++)
        {
            GUI.color = i < collected
                ? new Color(1f, 0.85f, 0f)        // gold = collected
                : new Color(0.25f, 0.25f, 0.25f); // dark = not yet
            GUI.DrawTexture(new Rect(dotStartX + i * dotGap, dotY, dotSize, dotSize),
                Texture2D.whiteTexture);
        }

        GUI.color = Color.white;

        // ---Status message----

        if (allDone)
        {
            GUI.color = new Color(0f, 1f, 0.4f, Mathf.PingPong(Time.time * 2f, 1f) * 0.5f + 0.5f);
            GUI.Label(new Rect(x + pad, y + 46f * s, panelW, 16f * s),
                "GO TO EXTRACTION", _alertStyle);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUI.Label(new Rect(x + pad, y + 46f * s, panelW, 16f * s),
                $"{collected} / {total} COLLECTED", _headerStyle);
            GUI.color = Color.white;
        }
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
        _alertStyle = new GUIStyle
        {
            fontSize  = Mathf.RoundToInt(10 * s),
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0f, 1f, 0.4f) }
        };
    }
}