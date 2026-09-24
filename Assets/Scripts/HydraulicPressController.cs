using UnityEngine;

public class HydraulicPressController : MonoBehaviour
{
    [Header("Parts")]
    public Transform ram;              // the "Ram" empty
    public Renderer statusLight;       // stack light
    public Transform workpiece;        // NEW: drag "Workpiece" here

    [Header("Motion (metres, seconds)")]
    public float downDistance = 0.5f;  // distance until the plate just TOUCHES the workpiece
    [Range(0f, 0.6f)]
    public float squashPercent = 0.3f; // NEW: how much shorter the workpiece gets (0.3 = 30%)
    public float pressSpeed = 0.2f;
    public float returnSpeed = 0.5f;
    public float holdTime = 1.5f;
    public float idleTime = 2.5f;
    [Min(0.01f)]
    public float recoveryTime = 0.8f;

    [Header("Status light colours")]
    public Color idleColor = Color.green;
    public Color pressingColor = Color.red;

    enum State { Idle, Pressing, Holding, Returning }

    State state = State.Idle;
    float timer;
    Vector3 topPos, bottomPos;

    // workpiece bookkeeping
    Renderer wpRenderer;
    Vector3 wpStartScale, wpStartPos;
    float wpHeightWorld, wpHeightLocal, maxSquash;
    float recoveryElapsed, recoveryStartSquash;
    bool recovering;

    void Start()
    {
        topPos = ram.localPosition;

        if (workpiece != null)
        {
            wpRenderer = workpiece.GetComponentInChildren<Renderer>();
            if (wpRenderer != null)
            {
                wpStartScale = workpiece.localScale;
                wpStartPos = workpiece.position;
                wpHeightWorld = wpRenderer.bounds.size.y;
                wpHeightLocal = wpHeightWorld / transform.lossyScale.y;
            }
        }

        // the plate now travels past the touch point by (squashPercent x workpiece height)
        bottomPos = topPos + Vector3.down * (downDistance + wpHeightLocal * squashPercent);
        timer = idleTime;
        SetLight(idleColor);
    }

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                UpdateRecovery();
                timer -= Time.deltaTime;
                if (timer <= 0f && !recovering)
                {
                    state = State.Pressing;
                    SetLight(pressingColor);
                }
                break;

            case State.Pressing:
                ram.localPosition = Vector3.MoveTowards(ram.localPosition, bottomPos, pressSpeed * Time.deltaTime);
                UpdateWorkpiece();
                if (ram.localPosition == bottomPos) { state = State.Holding; timer = holdTime; }
                break;

            case State.Holding:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Returning;
                    recoveryStartSquash = maxSquash;
                    recoveryElapsed = 0f;
                    recovering = recoveryStartSquash > 0f;
                }
                break;

            case State.Returning:
                ram.localPosition = Vector3.MoveTowards(ram.localPosition, topPos, returnSpeed * Time.deltaTime);
                UpdateRecovery();
                if (ram.localPosition == topPos) { state = State.Idle; timer = idleTime; SetLight(idleColor); }
                break;
        }
    }

    void UpdateWorkpiece()
    {
        if (wpRenderer == null) return;
        float travelled = topPos.y - ram.localPosition.y;
        float penetration = Mathf.Max(0f, travelled - downDistance);
        float squash = Mathf.Clamp01(penetration / wpHeightLocal);
        maxSquash = Mathf.Max(maxSquash, squash);
        ApplySquash(maxSquash);
    }

    void UpdateRecovery()
    {
        if (!recovering) return;

        recoveryElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(recoveryElapsed / Mathf.Max(recoveryTime, 0.01f));
        maxSquash = Mathf.Lerp(recoveryStartSquash, 0f, Mathf.SmoothStep(0f, 1f, progress));
        ApplySquash(maxSquash);
        if (progress >= 1f) recovering = false;
    }

    void ApplySquash(float s)
    {
        if (wpRenderer == null) return;
        float h = 1f - s;
        float w = 1f / Mathf.Sqrt(Mathf.Max(h, 0.01f));   // bulges sideways as it flattens
        workpiece.localScale = new Vector3(wpStartScale.x * w, wpStartScale.y * h, wpStartScale.z * w);
        float newHeight = wpHeightWorld * h;
        // keep the bottom face on the table (cube pivots are in the centre)
        workpiece.position = new Vector3(wpStartPos.x, wpStartPos.y - (wpHeightWorld - newHeight) * 0.5f, wpStartPos.z);
    }

    void SetLight(Color c)
    {
        if (statusLight == null) return;
        Material m = statusLight.material;
        m.SetColor("_BaseColor", c);
        m.SetColor("_EmissiveColor", c * 3f);
    }
}
