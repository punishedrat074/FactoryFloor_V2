using UnityEngine;

public sealed class LatheAudioController : MonoBehaviour
{
    [SerializeField] private LatheController spindle;
    [SerializeField] private LatheMachiningCycle machining;
    [SerializeField] private AudioSource motorSource;
    [SerializeField] private AudioSource cuttingSource;
    [SerializeField] private AudioClip motorClip;
    [SerializeField] private AudioClip cuttingClip;

    private bool cutting;

    private void Awake()
    {
        if (motorSource != null) motorSource.clip = motorClip;
        if (cuttingSource != null) cuttingSource.clip = cuttingClip;
        Configure(motorSource, 1.5f, 14f);
        Configure(cuttingSource, 1f, 10f);
        if (motorSource != null) motorSource.loop = true;
        if (cuttingSource != null) cuttingSource.loop = true;
    }

    private static void Configure(AudioSource source, float near, float far)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = near;
        source.maxDistance = far;
        source.dopplerLevel = 0f;
        source.volume = 0f;
    }

    private void OnEnable()
    {
        if (machining != null)
        {
            machining.CuttingBegan += BeginCutting;
            machining.CuttingEnded += EndCutting;
        }
    }

    private void Update()
    {
        if (motorSource != null && motorSource.clip != null)
        {
            bool spinning = spindle != null && spindle.rotationSpeed > 0f;
            if (spinning && !motorSource.isPlaying) motorSource.Play();
            motorSource.volume = Mathf.MoveTowards(motorSource.volume,
                spinning ? 0.45f : 0f, 1.5f * Time.deltaTime);
            if (!spinning && motorSource.volume <= 0f) motorSource.Stop();
        }

        if (cuttingSource != null && cuttingSource.isPlaying)
        {
            cuttingSource.volume = Mathf.MoveTowards(cuttingSource.volume,
                cutting ? 0.85f : 0f, 12f * Time.deltaTime);
            if (!cutting && cuttingSource.volume <= 0f) cuttingSource.Stop();
        }
    }

    private void BeginCutting()
    {
        if (cuttingSource == null || cuttingSource.clip == null) return;
        cutting = true;
        cuttingSource.Stop();
        cuttingSource.time = 0f;
        cuttingSource.volume = 0f;
        cuttingSource.Play();
    }

    private void EndCutting() => cutting = false;

    private void OnDisable()
    {
        if (machining != null)
        {
            machining.CuttingBegan -= BeginCutting;
            machining.CuttingEnded -= EndCutting;
        }
        cutting = false;
        if (motorSource != null) motorSource.Stop();
        if (cuttingSource != null) cuttingSource.Stop();
    }
}
