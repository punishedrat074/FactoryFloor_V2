using System.Collections;
using UnityEngine;

// Drives the existing sibling joint groups without changing the authored scene pose.
public sealed class RoboticArmPickAndPlace : MonoBehaviour
{
    [Min(0.2f)] public float travelTime = 2.4f;
    [Min(0.2f)] public float approachTime = 1.6f;
    [Min(0.1f)] public float gripTime = 0.65f;
    [Min(0f)] public float cyclePause = 2f;

    Transform turntable, lower, upper, wrist, gripper, left, right, workpiece;
    Transform pickup, pickupApproach, drop, dropApproach, transfer;
    Transform workpieceParent;
    Vector3 workpiecePosition, workpieceScale;
    Quaternion workpieceRotation;
    Transform[] joints;
    Vector3[] homePositions;
    Quaternion[] homeRotations;
    Collider[] workpieceColliders;
    bool[] colliderStates;
    Vector3 shoulder, lowerLink, upperLink, gripperOffset, leftOffset, rightOffset;
    Vector3 toolPosition, homeToolPosition;
    Quaternion toolRotation, homeToolRotation;
    float lowerLength, upperLength, openGap, closedGap, jawGap;
    const float ToolCenterOffset = 0.685f;
    bool initialized;

    void Start()
    {
        turntable = transform.Find("RotatingBase");
        lower = transform.Find("LowerArm");
        upper = transform.Find("UpperArm");
        wrist = transform.Find("Wrist");
        gripper = transform.Find("Gripper");
        left = transform.Find("LeftGripperFinger");
        right = transform.Find("RightGripperFinger");
        workpiece = transform.Find("PickupStation/MetalWorkpiece");
        pickup = transform.Find("PickupStation/PickupPosition");
        pickupApproach = transform.Find("PickupStation/PickupApproach");
        drop = transform.Find("DropStation/DropPosition");
        dropApproach = transform.Find("DropStation/DropApproach");
        transfer = transform.Find("TransferClearance");
        if (!turntable || !lower || !upper || !wrist || !gripper || !left || !right ||
            !workpiece || !pickup || !pickupApproach || !drop || !dropApproach || !transfer)
        {
            Debug.LogError("Robotic arm: a required part or target is missing.", this);
            enabled = false;
            return;
        }

        joints = new[] { turntable, lower, upper, wrist, gripper, left, right };
        homePositions = new Vector3[joints.Length];
        homeRotations = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            homePositions[i] = joints[i].localPosition;
            homeRotations[i] = joints[i].localRotation;
        }
        shoulder = lower.localPosition;
        lowerLink = upper.localPosition - shoulder;
        upperLink = wrist.localPosition - upper.localPosition;
        lowerLength = lowerLink.magnitude;
        upperLength = upperLink.magnitude;
        gripperOffset = gripper.localPosition - wrist.localPosition;
        leftOffset = left.localPosition - wrist.localPosition;
        rightOffset = right.localPosition - wrist.localPosition;
        openGap = Mathf.Abs(leftOffset.x);
        // Finger pads project 73.5 mm inward from each finger group's origin.
        closedGap = workpiece.localScale.x * 0.5f + 0.0735f;
        jawGap = openGap;
        homeToolRotation = wrist.localRotation;
        homeToolPosition = wrist.localPosition + homeToolRotation * Vector3.forward * ToolCenterOffset;
        toolPosition = homeToolPosition;
        toolRotation = homeToolRotation;
        workpieceParent = workpiece.parent;
        workpiecePosition = workpiece.localPosition;
        workpieceRotation = workpiece.localRotation;
        workpieceScale = workpiece.localScale;
        workpieceColliders = workpiece.GetComponentsInChildren<Collider>();
        colliderStates = new bool[workpieceColliders.Length];
        for (int i = 0; i < colliderStates.Length; i++) colliderStates[i] = workpieceColliders[i].enabled;
        initialized = true;
        StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            yield return MoveTo(pickupApproach, travelTime);
            yield return MoveJaws(openGap);
            yield return MoveTo(pickup, approachTime);
            yield return MoveJaws(closedGap);
            if (Vector3.Distance(workpiece.position, transform.TransformPoint(toolPosition)) > 0.025f)
            {
                Debug.LogError("Robotic arm: workpiece is outside the pickup fixture.", this);
                yield break;
            }
            workpiece.SetParent(gripper, true);
            foreach (Collider c in workpieceColliders) c.enabled = false;
            yield return new WaitForSeconds(0.2f);
            yield return MoveTo(pickupApproach, approachTime);
            yield return MoveTo(transfer, travelTime * 0.5f);
            yield return MoveTo(dropApproach, travelTime * 0.5f);
            yield return MoveTo(drop, approachTime);
            // Keep the placed part stationary while the fingers open.
            workpiece.SetParent(workpieceParent, true);
            RestoreColliders();
            yield return MoveJaws(openGap);
            yield return new WaitForSeconds(0.3f);
            yield return MoveTo(dropApproach, approachTime);
            yield return MoveTo(transfer, travelTime * 0.5f);
            yield return MoveTo(pickupApproach, travelTime * 0.5f);
            yield return Move(homeToolPosition, homeToolRotation, travelTime);
            yield return new WaitForSeconds(cyclePause);
            ResetWorkpiece();
        }
    }

    IEnumerator MoveTo(Transform target, float seconds)
    {
        return Move(transform.InverseTransformPoint(target.position),
            Quaternion.Inverse(transform.rotation) * target.rotation, seconds);
    }

    IEnumerator Move(Vector3 destination, Quaternion rotation, float seconds)
    {
        Vector3 start = toolPosition;
        Quaternion startRotation = toolRotation;
        float duration = Mathf.Max(0.1f, seconds);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Ease(elapsed / duration);
            SetPose(Vector3.Lerp(start, destination, t), Quaternion.Slerp(startRotation, rotation, t));
            yield return null;
        }
        SetPose(destination, rotation);
    }

    IEnumerator MoveJaws(float destination)
    {
        float start = jawGap;
        float duration = Mathf.Max(0.1f, gripTime);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            jawGap = Mathf.Lerp(start, destination, Ease(elapsed / duration));
            SetPose(toolPosition, toolRotation);
            yield return null;
        }
        jawGap = destination;
        SetPose(toolPosition, toolRotation);
    }

    void SetPose(Vector3 tcp, Quaternion rotation)
    {
        Vector3 wristPosition = tcp - rotation * Vector3.forward * ToolCenterOffset;
        Vector3 delta = wristPosition - shoulder;
        float radius = new Vector2(delta.x, delta.z).magnitude;
        float distance = Mathf.Sqrt(radius * radius + delta.y * delta.y);
        float safeDistance = Mathf.Clamp(distance, Mathf.Abs(lowerLength - upperLength) + 0.001f,
            lowerLength + upperLength - 0.001f);
        float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        Quaternion swivel = Quaternion.Euler(0, yaw, 0);
        float angle = Mathf.Atan2(delta.y, radius) + Mathf.Acos(Mathf.Clamp(
            (lowerLength * lowerLength + safeDistance * safeDistance - upperLength * upperLength) /
            (2f * lowerLength * safeDistance), -1f, 1f));
        Vector3 elbowInPlane = new Vector3(0, Mathf.Sin(angle) * lowerLength, Mathf.Cos(angle) * lowerLength);
        Vector3 elbow = shoulder + swivel * elbowInPlane;
        Vector3 forearmInPlane = Quaternion.Inverse(swivel) * (wristPosition - elbow);
        turntable.localRotation = swivel * homeRotations[0];
        lower.localPosition = shoulder;
        lower.localRotation = swivel * Quaternion.FromToRotation(lowerLink, elbowInPlane) * homeRotations[1];
        upper.localPosition = elbow;
        upper.localRotation = swivel * Quaternion.FromToRotation(upperLink, forearmInPlane) * homeRotations[2];
        wrist.localPosition = wristPosition;
        wrist.localRotation = rotation;
        gripper.localPosition = wristPosition + rotation * gripperOffset;
        gripper.localRotation = rotation;
        Vector3 l = leftOffset, r = rightOffset;
        l.x = -jawGap; r.x = jawGap;
        left.localPosition = wristPosition + rotation * l;
        right.localPosition = wristPosition + rotation * r;
        left.localRotation = right.localRotation = rotation;
        toolPosition = tcp;
        toolRotation = rotation;
    }

    static float Ease(float t) => t * t * t * (t * (6f * t - 15f) + 10f);

    void RestoreColliders()
    {
        for (int i = 0; i < workpieceColliders.Length; i++)
            if (workpieceColliders[i]) workpieceColliders[i].enabled = colliderStates[i];
    }

    void ResetWorkpiece()
    {
        if (!workpiece || !workpieceParent) return;
        workpiece.SetParent(workpieceParent, false);
        workpiece.localPosition = workpiecePosition;
        workpiece.localRotation = workpieceRotation;
        workpiece.localScale = workpieceScale;
        RestoreColliders();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (!initialized) return;
        ResetWorkpiece();
        for (int i = 0; i < joints.Length; i++)
            if (joints[i]) joints[i].SetLocalPositionAndRotation(homePositions[i], homeRotations[i]);
    }
}
