using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [SerializeField] PlayerExposure playerExposure;

    VisualElement lightMeterFill;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        lightMeterFill = root.Q<VisualElement>("light-meter-fill");
    }

    void Update()
    {
        if (playerExposure == null || lightMeterFill == null) return;

        // Convert exposure 0-1 to a percentage height
        float exposure = playerExposure.Exposure;
        lightMeterFill.style.height = Length.Percent(exposure * 100f);

        //Change color based on exposure level
        // Dark = yellow, medium = orange, fully lit = red
        Color meterColor;
        if (exposure < 0.3f)
            meterColor = new Color(0.2f, 0.8f, 0.2f); // green — safe
        else if (exposure < 0.6f)
            meterColor = new Color(1f, 0.85f, 0.2f);   // yellow — caution
        else
            meterColor = new Color(1f, 0.3f, 0.1f);    // red — danger

        lightMeterFill.style.backgroundColor = new StyleColor(meterColor);
    }
}
