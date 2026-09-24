using System.Collections;
using System;
using UnityEngine;

public sealed class LatheMachiningCycle : MonoBehaviour
{
    public event Action CuttingBegan;
    public event Action CuttingEnded;

    [SerializeField] private Transform cuttingTool;
    [SerializeField] private Transform workpiece;

    [Min(0f)] public float approachDistance = 0.065f;
    [Min(0f)] public float cutDepth = 0.02f;
    [Range(0f, 0.8f)] public float radiusReduction = 0.33f;
    [Min(0.01f)] public float approachTime = 0.7f;
    [Min(0.01f)] public float engageTime = 0.2f;
    [Min(0.01f)] public float cutTime = 5f;
    [Min(0.01f)] public float retractTime = 0.7f;
    [Min(0.01f)] public float resetTime = 0.5f;
    [Min(0f)] public float cyclePause = 0.5f;

    private Vector3 toolRest;
    private Vector3 workpieceRest;

    private IEnumerator Start()
    {
        if (cuttingTool == null || workpiece == null) yield break;
        toolRest = cuttingTool.localPosition;
        workpieceRest = workpiece.localScale;

        while (true)
        {
            Vector3 approach = toolRest + Vector3.forward * approachDistance;
            Vector3 engaged = approach + Vector3.forward * 0.008f;
            Vector3 cutEnd = engaged + Vector3.forward * cutDepth;
            Vector3 machined = new Vector3(
                workpieceRest.x * (1f - radiusReduction),
                workpieceRest.y,
                workpieceRest.z * (1f - radiusReduction));

            yield return MoveTool(toolRest, approach, approachTime);
            CuttingBegan?.Invoke();
            yield return MoveTool(approach, engaged, engageTime);
            yield return Cut(engaged, cutEnd, workpieceRest, machined, cutTime);
            CuttingEnded?.Invoke();
            yield return MoveTool(cutEnd, toolRest, retractTime);
            yield return ResizeWorkpiece(machined, workpieceRest, resetTime);
            if (cyclePause > 0f) yield return new WaitForSeconds(cyclePause);
        }
    }

    private IEnumerator MoveTool(Vector3 from, Vector3 to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            cuttingTool.localPosition = Vector3.Lerp(from, to,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        cuttingTool.localPosition = to;
    }

    private IEnumerator Cut(Vector3 from, Vector3 to, Vector3 original, Vector3 machined,
        float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            cuttingTool.localPosition = Vector3.Lerp(from, to, progress);
            workpiece.localScale = Vector3.Lerp(original, machined, progress);
            yield return null;
        }
        cuttingTool.localPosition = to;
        workpiece.localScale = machined;
    }

    private IEnumerator ResizeWorkpiece(Vector3 from, Vector3 to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            workpiece.localScale = Vector3.Lerp(from, to,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        workpiece.localScale = to;
    }

    private void OnDisable()
    {
        CuttingEnded?.Invoke();
        if (cuttingTool != null) cuttingTool.localPosition = toolRest;
        if (workpiece != null && workpieceRest != Vector3.zero)
            workpiece.localScale = workpieceRest;
    }
}
