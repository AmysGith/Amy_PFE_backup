using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CoralBleachingController : MonoBehaviour
{
    [Header("Références")]
    public POIPopulator populator;
    public ParticleSystem mucusParticles;

    [Header("Zones")]
    public float triggerRadius = 25f;
    public float bleachStartRadius = 15f;

    [Header("Timing")]
    public float bleachInDuration = 8f;
    public float bleachOutDuration = 12f;

    [Header("Particules")]
    [Range(0f, 1f)]
    public float particleThreshold = 0.4f;

    // ── Etat ─────────────────────────────────────────────────────────────────
    private float bleachProgress;
    private bool playerInside;
    private Transform playerTransform;
    private Coroutine bleachCoroutine;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = triggerRadius;
        col.center = new Vector3(25f, 0f, 25f);

        if (populator == null)
            populator = GetComponent<POIPopulator>();
    }


    private void OnEnable()
    {
        bleachProgress = 0f;
        playerInside = false;
        UpdateBleach(0f);
        if (mucusParticles != null) mucusParticles.Stop();
    }

    private void OnDisable()
    {
        if (bleachCoroutine != null) StopCoroutine(bleachCoroutine);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Trigger
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collider détecté: " + other.name + " tag: " + other.tag + " root: " + other.transform.root.name);

        Transform root = other.transform.root;
        if (!root.CompareTag("Player")) return;

        playerTransform = root;
        playerInside = true;

        if (bleachCoroutine != null) StopCoroutine(bleachCoroutine);
        bleachCoroutine = StartCoroutine(BleachIn());
    }


    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;

        if (bleachCoroutine != null) StopCoroutine(bleachCoroutine);
        bleachCoroutine = StartCoroutine(BleachOut());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator BleachIn()
    {
        while (bleachProgress < 1f)
        {
            float proximity = 1f;
            if (playerTransform != null)
            {
                Vector3 chunkCenter = transform.position + new Vector3(25f, 0f, 25f);
                float dist = Vector3.Distance(playerTransform.position, chunkCenter);
                proximity = Mathf.Max(0.1f, 1f - Mathf.Clamp01(dist / bleachStartRadius));
            }

            bleachProgress = Mathf.MoveTowards(
                bleachProgress, 1f,
                (1f / bleachInDuration) * proximity * Time.deltaTime);

            UpdateBleach(bleachProgress);
            yield return null;
        }
    }

    private IEnumerator BleachOut()
    {
        while (bleachProgress > 0f)
        {
            bleachProgress = Mathf.MoveTowards(
                bleachProgress, 0f,
                (1f / bleachOutDuration) * Time.deltaTime);

            UpdateBleach(bleachProgress);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mise à jour shader + particules
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateBleach(float progress)
    {
        if (populator != null)
            populator.SetBleachProgress(progress);

        if (mucusParticles == null) return;

        if (progress >= particleThreshold && !mucusParticles.isPlaying)
            mucusParticles.Play();
        else if (progress < particleThreshold && mucusParticles.isPlaying)
            mucusParticles.Stop();

        if (mucusParticles.isPlaying)
        {
            var emission = mucusParticles.emission;
            emission.rateOverTime = Mathf.Lerp(
                0f, 80f,
                (progress - particleThreshold) / (1f - particleThreshold));
        }
    }
}