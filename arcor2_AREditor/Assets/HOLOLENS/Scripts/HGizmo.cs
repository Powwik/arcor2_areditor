using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HGizmo : MonoBehaviour
{
    public enum Axis {
        X,
        Y,
        Z,
        NONE
    }
    [SerializeField] private TMPro.TMP_Text XAxisLabel;
    [SerializeField] private TMPro.TMP_Text YAxisLabel;
    [SerializeField] private TMPro.TMP_Text ZAxisLabel;

    public float XDelta;
    public float YDelta;
    public float ZDelta;

    private string FormatValue(float value) {
        if (Mathf.Abs(value) < 0.000099f)
            return $"0cm";
        if (Mathf.Abs(value) < 0.00999f)
            return $"{value * 1000:0.##}mm";
        if (Mathf.Abs(value) < 0.9999f)
            return $"{value * 100:0.##}cm";
        return $"{value:0.###}m";
    }

    public void SetXDelta(float value) {        
        XAxisLabel.text = $"Δ{FormatValue(value)}";
        XDelta = value;
    }

    public void SetYDelta(float value) {        
        YAxisLabel.text = $"Δ{FormatValue(value)}";
        YDelta = value;
    }

    public void SetZDelta(float value) {        
        ZAxisLabel.text = $"Δ{FormatValue(value)}";
        ZDelta = value;
    }

    public float GetXDelta() {
        return XDelta;
    }

    public float GetYDelta() {
        return YDelta;
    }

    public float GetZDelta() {
        return ZDelta;
    }
}
