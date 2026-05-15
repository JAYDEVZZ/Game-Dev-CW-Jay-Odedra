using UnityEngine;

public class UpperBodyAim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [Header("Bone")]
    [SerializeField] private HumanBodyBones aimBone = HumanBodyBones.Chest;
    [Header("Tuning")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] [Range(0f, 1f)] private float pitchInfluence = 0.5f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 50f;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;

    private float _currentPitch = 0f;

    private void LateUpdate()
    {
        if (animator == null || thirdPersonCamera == null) return;
        Transform bone = animator.GetBoneTransform(aimBone);
        if (bone == null) return;
        float rawPitch = thirdPersonCamera.CurrentPitchAngle - 20f; 
        float targetPitch = Mathf.Clamp(rawPitch * pitchInfluence, minPitch, maxPitch);

        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * smoothSpeed);


        bone.localRotation *= Quaternion.AngleAxis(_currentPitch, rotationAxis);
    }
}