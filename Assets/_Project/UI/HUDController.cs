using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    PlayerExposure playerExposure;
    PlayerController playerController;

    VisualElement lightMeterFill;
    VisualElement noiseMeterFill;
    VisualElement crosshair;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        lightMeterFill = root.Q<VisualElement>("light-meter-fill");
        noiseMeterFill = root.Q<VisualElement>("noise-meter-fill");
        crosshair = root.Q<VisualElement>("crosshair");

        // Find player and get the Exposure
        var player = GameObject.FindWithTag("Player");
        playerExposure = player.GetComponentInChildren<PlayerExposure>();
        playerController = player.GetComponentInChildren<PlayerController>();
    }

    void Update()
    {
        // Light meter
        if (playerExposure != null && lightMeterFill != null)
        {
            float exposure = playerExposure.Exposure;
            lightMeterFill.style.height = Length.Percent(exposure * 100f);

            Color meterColor;
            if (exposure < 0.3f)
                meterColor = new Color(0.2f, 0.8f, 0.2f);
            else if (exposure < 0.6f)
                meterColor = new Color(1f, 0.85f, 0.2f);
            else
                meterColor = new Color(1f, 0.3f, 0.1f);

            lightMeterFill.style.backgroundColor = new StyleColor(meterColor);
        }

        // Noise meter
        if (noiseMeterFill != null && NoiseSystem.Instance != null)
        {
            float loudness = NoiseSystem.Instance.CurrentNoiseLoudness;
            noiseMeterFill.style.height = Length.Percent(loudness * 100f);
        }

        // Show/hide crosshair
        if(crosshair != null && playerController != null)
        {
            crosshair.style.display = playerController.isAiming ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
