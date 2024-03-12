using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

public class HStepSelectorMenu3 : Singleton<HStepSelectorMenu3>
{
    public TextMeshPro value;
    public GameObject stepMenu;
    public PinchSlider slider;
    // Start is called before the first frame update
    private void Start()
    {
        slider.SliderValue = 1f;
        stepMenu.transform.position = HEndEffectorTransform3.Instance.gizmoTransform.position + new Vector3(0.0f, 0.22f, 0.0f);
    }

    // Update is called once per frame
    private void Update()
    {
        value.text = slider.SliderValue.ToString("F2");
    }
}
