using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    [Range(0.01f, 1.0f)]
    private float smoothFactor = 0.5f;

    [SerializeField]
    private Vector3 offset;

    void Start()
    {
        // If offset isn't set in Inspector, calculate it from current scene positions
        if (offset == Vector3.zero && target != null)
        {
            offset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate the desired position based on the fixed offset
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly interpolate to the new position
        transform.position = Vector3.Slerp(transform.position, desiredPosition, smoothFactor);
        
        // Ensure the camera stays looking at the player
        transform.LookAt(target);
    }
}