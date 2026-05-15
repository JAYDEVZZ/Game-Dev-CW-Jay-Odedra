using System.Collections.Generic;
using UnityEngine;

public class EnemyWorldHUD : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float barWidth = 80f;
    [SerializeField] private float barHeight = 9f;
    [SerializeField] private float worldHeightOffset = 2.5f;
    [SerializeField] private float iconOffset = 30f; // increase to push ? ! higher

    [Header("Occlusion")]
    [SerializeField] private LayerMask occlusionMask; // set this to your Environment layer in the Inspector

    private AITarget[] _allAI;
    private GUIStyle _labelStyle;

    // Cached per-frame so we're not raycasting inside OnGUI
    private readonly Dictionary<AITarget, bool> _occluded = new();

    private void Start()
    {
        _allAI = FindObjectsByType<AITarget>(FindObjectsSortMode.None);

        _labelStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;

        foreach (AITarget ai in _allAI)
        {
            if (ai == null) continue;

            // Raycast from camera toward enemy chest height
            Vector3 enemyPos = ai.transform.position + Vector3.up * (worldHeightOffset * 0.6f);
            Vector3 dir = enemyPos - camPos;
            float dist = dir.magnitude;

            bool blocked = occlusionMask != 0 &&
                           Physics.Raycast(camPos, dir.normalized, dist - 0.1f, occlusionMask);

            _occluded[ai] = blocked;
        }
    }

    private void OnGUI()
    {
        if (Camera.main == null) return;

        foreach (AITarget ai in _allAI)
        {
            if (ai == null) continue;

            // Skip if behind a wall
            if (_occluded.TryGetValue(ai, out bool blocked) && blocked) continue;

            Health health = ai.GetComponent<Health>();
            if (health != null && health.IsDead) continue;

            bool showDetection = ai.DetectionMeter > 0.01f || ai.IsInvestigatingLure;
            bool showHealth = ai.CurrentState == AITarget.AIState.Combat
                || (health != null && health.NormalisedHealth < 0.99f);

            if (!showDetection && !showHealth) continue;

            // world → screen
            Vector3 worldPos = ai.transform.position + Vector3.up * worldHeightOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) continue;

            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // scale with distance
            float dist = Vector3.Distance(Camera.main.transform.position, ai.transform.position);
            float scale = Mathf.Clamp(10f / Mathf.Max(dist, 1f), 0.5f, 1.4f);
            float w = barWidth * scale;
            float h = barHeight * scale;
            float gap = 4f * scale;

            float drawY = guiY;

            if (showDetection)
            {
                drawY -= h + gap;

                string icon = ai.CurrentState == AITarget.AIState.Combat ? "!" : "?";
                Color iconColor = ai.CurrentState == AITarget.AIState.Combat ? Color.red
                    : ai.IsInvestigatingLure ? Color.green // green = following a lure
                    : Color.yellow;
                float iconSize = 18f * scale;

                // Shadow
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.Label(new Rect(guiX - 8f + 1f, drawY - iconOffset * scale + 1f, 16f, iconSize),
                    icon, _labelStyle);

                GUI.color = iconColor;
                GUI.Label(new Rect(guiX - 8f, drawY - iconOffset * scale, 16f, iconSize),
                    icon, _labelStyle);
                GUI.color = Color.white;

                Color fillCol = ai.DetectionMeter >= 1f ? Color.red : Color.yellow;
                DrawBar(guiX - w / 2f, drawY, w, h, ai.DetectionMeter, fillCol);
            }

            if (showHealth && health != null)
            {
                drawY -= h + gap;
                DrawBar(guiX - w / 2f, drawY, w, h,
                    health.NormalisedHealth, HealthColor(health.NormalisedHealth));
            }
        }
    }
    

    private void DrawBar(float x, float y, float w, float h, float value, Color fill)
    {
        // Outline
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);

        // Background
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Fill
        GUI.color = fill;
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(value), h), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private Color HealthColor(float v) =>
        v > 0.6f ? Color.green : v > 0.3f ? Color.yellow : Color.red;
}