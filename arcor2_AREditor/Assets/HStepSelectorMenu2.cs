using System.Collections;
using System.Collections.Generic;
using Base;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class HStepSelectorMenu2 : Singleton<HStepSelectorMenu2> {

    public GameObject UnitsMenu;
    public GameObject ButtonsMenu;
    public Interactable positiveButton;
    public Interactable negativeButton;
    public Interactable mmButton;
    public Interactable cmButton;
    public float unit;

    // Start is called before the first frame update
    void Start()
    {
        unit = 0.01f;

        positiveButton.OnClick.AddListener(() => MoveObject(unit));
        negativeButton.OnClick.AddListener(() => MoveObject(-unit));

        mmButton.OnClick.AddListener(() => SetUnit("mm"));
        cmButton.OnClick.AddListener(() => SetUnit("cm"));

        UnitsMenu.SetActive(false);
        ButtonsMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void MoveObject(float s)
    {
        switch (HEndEffectorTransform2.Instance.selectedAxis) {
            case Gizmo.Axis.X:
                HEndEffectorTransform2.Instance.gizmoTransform.position += new Vector3(0.0f, 0.0f, s);
                break;
            case Gizmo.Axis.Y:
                HEndEffectorTransform2.Instance.gizmoTransform.position += new Vector3(s, 0.0f, 0.0f);
                break;
            case Gizmo.Axis.Z:
                HEndEffectorTransform2.Instance.gizmoTransform.position += new Vector3(0.0f, s, 0.0f);
                break;
            default:
                return;
        }
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(HEndEffectorTransform2.Instance.tmpModel.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);
        HEndEffectorTransform2.Instance.SetIKToModel(HEndEffectorTransform2.Instance.defaultOrientation, position);
    }

    public void SetUnit(string units) {
        switch (units) {
            case ("mm"):
                unit = 0.001f;
                break;
            case ("cm"):
                unit = 0.01f;
                break;
            case ("dm"):
                unit = 0.1f;
                break;
            default:
                break;
        }
    }
}
