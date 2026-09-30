using UnityEngine;

// Only operates on the explicitly assigned conveyor parts.
public sealed class ConveyorController : MonoBehaviour
{
    [Min(0f)] public float beltSpeed = 0.25f;
    public bool running = true;
    [SerializeField] private Transform[] beltSeams;
    [SerializeField] private Transform[] workpieces;
    [SerializeField] private float[] workpieceCenters;
    [SerializeField] private Transform[] rollers;
    [SerializeField] private float[] rollerRadii;
    [SerializeField] private AudioClip motorClip;
    [SerializeField] private AudioClip rollerClip;
    [Range(0f, 1f)] public float motorVolume = 0.22f;
    [Range(0f, 1f)] public float rollerVolume = 0.12f;

    private const float BeltLength = 7.20f;
    // Travel past the nose, drop into the floor tray, pause, then reload.
    private const float LoadingZ = -3.30f;
    private const float DischargeZ = 3.96f;
    private const float FallTravel = .12f;
    private const float FloorPauseTravel = .30f;
    private const float WorkpieceTravel = DischargeZ - LoadingZ + FallTravel + FloorPauseTravel;
    private Vector3[] seamRest, pieceRest;
    private Quaternion[] rollerRest;
    private float[] rollerAngles;
    private float beltPhase, piecePhase;
    private AudioSource motorSource, rollerSource;
    private bool ready;

    private void Awake()
    {
        if (beltSeams == null || workpieces == null || rollers == null ||
            workpieceCenters == null || rollerRadii == null ||
            workpieces.Length != workpieceCenters.Length || rollers.Length != rollerRadii.Length)
        {
            Debug.LogError("Conveyor part references are incomplete.", this);
            enabled = false;
            return;
        }
        seamRest = CapturePositions(beltSeams);
        pieceRest = CapturePositions(workpieces);
        rollerRest = new Quaternion[rollers.Length];
        rollerAngles = new float[rollers.Length];
        for (int i = 0; i < rollers.Length; i++)
            if (rollers[i]) rollerRest[i] = rollers[i].localRotation;

        motorSource = CreateSound("ConveyorMotorAudio", new Vector3(.77f, .8f, 3.16f), motorClip, 1.3f, 16f);
        rollerSource = CreateSound("ConveyorRollerAudio", new Vector3(0f, .84f, 0f), rollerClip, 1f, 12f);
        ready = true;
    }

    private static Vector3[] CapturePositions(Transform[] parts)
    {
        var positions = new Vector3[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (parts[i]) positions[i] = parts[i].localPosition;
        return positions;
    }

    private AudioSource CreateSound(string label, Vector3 position, AudioClip clip, float near, float far)
    {
        var emitter = new GameObject(label);
        emitter.transform.SetParent(transform, false);
        emitter.transform.localPosition = position;
        var source = emitter.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = near;
        source.maxDistance = far;
        source.dopplerLevel = 0f;
        source.spread = 0f;
        source.priority = 180;
        source.volume = 0f;
        return source;
    }

    private void Update()
    {
        if (!ready) return;
        float speed = running ? Mathf.Max(0f, beltSpeed) : 0f;
        float distance = speed * Time.deltaTime;
        beltPhase = Mathf.Repeat(beltPhase + distance, BeltLength);
        piecePhase = Mathf.Repeat(piecePhase + distance, WorkpieceTravel);

        for (int i = 0; i < beltSeams.Length; i++)
        {
            if (!beltSeams[i]) continue;
            Vector3 position = seamRest[i];
            // Seams travel on the top surface and re-enter at the rear roller.
            position.z = Mathf.Repeat(position.z + BeltLength * .5f + beltPhase, BeltLength) - BeltLength * .5f;
            beltSeams[i].localPosition = position;
        }
        for (int i = 0; i < workpieces.Length; i++)
        {
            if (!workpieces[i]) continue;
            float phase = Mathf.Repeat(workpieceCenters[i] - LoadingZ + piecePhase, WorkpieceTravel);
            float onBelt = DischargeZ - LoadingZ;
            float fall = Mathf.Clamp01((phase - onBelt) / FallTravel);
            float center = LoadingZ + Mathf.Min(phase, onBelt + FallTravel);
            // The entire blank clears the end roller before gravity lowers it.
            // The tray surface is .08 m high; belt top is .98 m.
            float drop = .90f * fall * fall;
            workpieces[i].localPosition = pieceRest[i] +
                new Vector3(0f, -drop, center - workpieceCenters[i]);
        }
        for (int i = 0; i < rollers.Length; i++)
        {
            if (!rollers[i] || Mathf.Abs(rollerRadii[i]) < .001f) continue;
            // Cylinder local Y maps to conveyor -X. Negative radii mark return idlers.
            rollerAngles[i] = Mathf.Repeat(rollerAngles[i] - distance / rollerRadii[i] * Mathf.Rad2Deg, 360f);
            rollers[i].localRotation = rollerRest[i] * Quaternion.AngleAxis(rollerAngles[i], Vector3.up);
        }

        float ratio = Mathf.Clamp(speed / .25f, .35f, 2f);
        UpdateSound(motorSource, speed > 0f ? motorVolume : 0f, .75f * ratio);
        UpdateSound(rollerSource, speed > 0f ? rollerVolume : 0f, ratio);
    }

    private static void UpdateSound(AudioSource source, float volume, float pitch)
    {
        if (!source || !source.clip) return;
        source.pitch = pitch;
        if (volume > 0f && !source.isPlaying) source.Play();
        source.volume = Mathf.MoveTowards(source.volume, volume, Time.deltaTime * 1.5f);
        if (volume <= 0f && source.volume <= 0f) source.Stop();
    }

    private void OnDisable()
    {
        if (motorSource) motorSource.Stop();
        if (rollerSource) rollerSource.Stop();
        if (!ready) return;
        for (int i = 0; i < beltSeams.Length; i++)
            if (beltSeams[i]) beltSeams[i].localPosition = seamRest[i];
        for (int i = 0; i < workpieces.Length; i++)
            if (workpieces[i]) workpieces[i].localPosition = pieceRest[i];
        for (int i = 0; i < rollers.Length; i++)
        {
            if (rollers[i]) rollers[i].localRotation = rollerRest[i];
            rollerAngles[i] = 0f;
        }
        beltPhase = piecePhase = 0f;
        if (motorSource) motorSource.volume = 0f;
        if (rollerSource) rollerSource.volume = 0f;
    }
}
