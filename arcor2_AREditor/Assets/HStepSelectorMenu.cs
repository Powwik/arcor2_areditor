using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit.UI;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HStepSelectorMenu : Singleton<HStepSelectorMenu>
{
    public GameObject stepSelectorMenu;
    public float step;
    public float unit;
    private string unitsString;
    private string realValue;
    public TextMeshPro axisText;
    public TextMeshPro stepText;
    public Interactable step1Button;
    public Interactable step2Button;
    public Interactable step5Button;
    public Interactable positiveButton;
    public Interactable negativeButton;
    public Interactable mmButton;
    public Interactable cmButton;
    public Interactable dmButton;

    // Start is called before the first frame update
    void Start() {
        step = 1f;
        unit = 0.01f;
        unitsString = "cm";
        realValue = ConvertToRealValue(unit * step);
        step1Button.OnClick.AddListener(() => setStep(1f));
        step2Button.OnClick.AddListener(() => setStep(2f));
        step5Button.OnClick.AddListener(() => setStep(5f));

        positiveButton.OnClick.AddListener(() => MoveObject(step));
        negativeButton.OnClick.AddListener(() => MoveObject(-step));

        mmButton.OnClick.AddListener(() => SetUnit("mm"));
        cmButton.OnClick.AddListener(() => SetUnit("cm"));
        dmButton.OnClick.AddListener(() => SetUnit("dm"));
    }

    // Update is called once per frame
    void Update()
    {
        axisText.text = "Selected axis: " + HEndEffectorTransform.Instance.selectedAxis;
        stepText.text = "Current step: " + realValue;
    }

    private void MoveObject(float s) {

        switch (HEndEffectorTransform.Instance.selectedAxis) {
            case Gizmo.Axis.X:
                HEndEffectorTransform.Instance.gizmoTransform.position += new Vector3(0.0f, 0.0f, s * unit);
                break;
            case Gizmo.Axis.Y:
                HEndEffectorTransform.Instance.gizmoTransform.position += new Vector3(s * unit, 0.0f, 0.0f);
                break;
            case Gizmo.Axis.Z:
                HEndEffectorTransform.Instance.gizmoTransform.position += new Vector3(0.0f, s * unit, 0.0f);
                break;
            default:
                return;
        }
        Vector3 vec = HEndEffectorTransform.Instance.gizmoTransform.position + new Vector3(0.0f, 0.25f, 0.0f);
        HEndEffectorTransform.Instance.confirmationWindow.transform.position = vec;
        HEndEffectorTransform.Instance.confirmationWindow.SetActive(true);

        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(HEndEffectorTransform.Instance.tmpModel.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);
        HEndEffectorTransform.Instance.SetIKToModel(HEndEffectorTransform.Instance.defaultOrientation, position);
    }


    public void setStep(float number) {
        step = number;

        realValue = ConvertToRealValue(unit * step);
    }

    public void SetUnit(string units) {
        switch(units) {
            case ("mm"):
                unit = 0.001f;
                unitsString = "mm";
                break;
            case ("cm"):
                unit = 0.01f;
                unitsString = "cm";
                break;
            case ("dm"):
                unit = 0.1f;
                unitsString = "dm";
                break;
            default : break;
        }
        realValue = ConvertToRealValue(unit * step);
    }

    private string ConvertToRealValue(float value) {
        string numberString = value.ToString();
        return numberString[numberString.Length - 1] + unitsString;
    }
}
