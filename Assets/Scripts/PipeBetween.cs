using UnityEngine;

[ExecuteAlways]
public class PipeBetween : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float thickness = 0.06f;   // diameter in metres

    void LateUpdate()
    {
        if (pointA == null || pointB == null) return;
        Vector3 a = pointA.position, b = pointB.position;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.0001f) return;
        transform.position = (a + b) * 0.5f;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        transform.localScale = new Vector3(thickness, len * 0.5f, thickness);
    }
}