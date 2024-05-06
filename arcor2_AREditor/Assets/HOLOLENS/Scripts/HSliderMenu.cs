using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

/*********************************************************************
 * \file HSLiderMenu.cs
 * \script handles slider object
 * 
 * \author Daniel Zmrzlý
 *********************************************************************/
public class HSliderMenu : Singleton<HSliderMenu>
{
    public TextMeshPro Value;
    public GameObject SliderMenu;
    public PinchSlider Slider;
    public Vector3 SliderMenuVectorUp = new Vector3(0.0f, 0.27f, 0.0f);

    // Start is called before the first frame update
    private void Start()
    {
        Slider.SliderValue = 1f;
        SliderMenu.transform.localPosition = HEndEffectorTransform.Instance.gizmoTransform.localPosition + SliderMenuVectorUp;
    }

    // Update is called once per frame
    private void Update()
    {
        Value.text = Slider.SliderValue.ToString("F2");
    }
}
