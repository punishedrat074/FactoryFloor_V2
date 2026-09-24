using UnityEngine;

public sealed class LatheController : MonoBehaviour
{
    [Min(0f)] public float rotationSpeed = 360f;
    [SerializeField] private Transform rotatingAssembly;

    private Quaternion restRotation;
    private float angle;

    private void Awake()
    {
        if (rotatingAssembly == null)
            rotatingAssembly = transform.Find("RotatingAssembly");
        if (rotatingAssembly != null)
            restRotation = rotatingAssembly.localRotation;
    }

    private void Update()
    {
        if (rotatingAssembly == null) return;
        angle = Mathf.Repeat(angle + rotationSpeed * Time.deltaTime, 360f);
        rotatingAssembly.localRotation = restRotation * Quaternion.AngleAxis(angle, Vector3.right);
    }
}
