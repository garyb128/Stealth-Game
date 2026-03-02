using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerExposure : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera captureCameraTop;
    [SerializeField] Camera captureCameraBottom;
    [SerializeField] RenderTexture captureRTTop;
    [SerializeField] RenderTexture captureRTBottom;

    [Header("Settings")]
    [SerializeField] float updateInterval = 0.1f;
    [SerializeField] float smoothSpeed = 5f;

    // 0 = fully in shadow, 1 = fully lit
    // NPCPerception reads this to scale detection rate
    public float Exposure { get; private set; }
    [SerializeField] float targetExposure;

    // Store each camera's luminance separately and combine
    float luminanceTop;
    float luminanceBottom;

    Coroutine sampleCoroutine;// Reference to the coroutine

    void Awake()
    {
        if (captureCameraTop == null || captureCameraBottom == null)
        {
            Debug.LogError("[PlayerExposure] Missing camera references!", this);
            return;
        }

        // Cameras stay disabled - we trigger renders manually
        captureCameraTop.enabled = false;
        captureCameraBottom.enabled = false;
    }


    void OnEnable()
    {
        // Start the sample coroutine
        sampleCoroutine = null;
        sampleCoroutine = StartCoroutine(SampleRoutine());
    }

    void OnDisable()
    {
        //Stop the sample coroutine
        StopCoroutine(sampleCoroutine);
        sampleCoroutine = null;
    }

    void Update()
    {
        // Smoothly lerp toward target rather than snapping
        Exposure = Mathf.Lerp(Exposure, targetExposure, smoothSpeed * Time.deltaTime);
    }

    IEnumerator SampleRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(updateInterval);

        while (true)
        {
            yield return wait;

            // Manually trigger a render of just the octohedron mesh
            // Camera is disabled so this is the only time it renders
            captureCameraTop.Render();
            captureCameraBottom.Render();

            // Request async readback for both — callbacks fire when GPU is ready
            AsyncGPUReadback.Request(captureRTTop, 0, TextureFormat.RGBA32, OnReadbackTop);
            AsyncGPUReadback.Request(captureRTBottom, 0, TextureFormat.RGBA32, OnReadbackBottom);
        }
    }

    void OnReadbackTop(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogWarning("[PlayerExposure] GPU readback error (Top).");
            return;
        }

        luminanceTop = CalculateLuminance(request);

        // Combine both cameras every time either one updates
        targetExposure = Mathf.Clamp01((luminanceTop + luminanceBottom) * 0.5f);
    }

    void OnReadbackBottom(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogWarning("[PlayerExposure] GPU readback error (Bottom).");
            return;
        }

        luminanceBottom = CalculateLuminance(request);

        targetExposure = Mathf.Clamp01((luminanceTop + luminanceBottom) * 0.5f);
    }

    private float CalculateLuminance(AsyncGPUReadbackRequest request)
    {
        var pixels = request.GetData<Color32>();
        float total = 0f;
        int pixelCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 p = pixels[i];

            // Skip pure black pixels — these are background, not the mesh
            if (p.r == 0 && p.g == 0 && p.b == 0) continue;

            total += (p.r * 0.2126f + p.g * 0.7152f + p.b * 0.0722f) / 255f;
            pixelCount++;
        }

        // If no mesh pixels were found, return 0
        return pixelCount > 0 ? total / pixelCount : 0f;
    }
}
