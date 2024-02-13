using System.Collections;
using System.Collections.Generic;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HStepSelectorMenu : MonoBehaviour
{
    public GameObject stepSelectorMenu;
    public float step;
    public TextMeshPro text;
    public Interactable stepDefaultButton;
    public Interactable step1Button;
    public Interactable step2Button;
    public Interactable step5Button;

    public Interactable positiveButton;
    public Interactable negativeButton;
    // Start is called before the first frame update
    void Start() {
        //stepSelectorMenu.SetActive(false);
        step = 0.05f;
        stepDefaultButton.OnClick.AddListener(() => setStep(0));
        step1Button.OnClick.AddListener(() => setStep(1));
        step2Button.OnClick.AddListener(() => setStep(2));
        step5Button.OnClick.AddListener(() => setStep(5));

        positiveButton.OnClick.AddListener(() => MoveObject(step));
        negativeButton.OnClick.AddListener(() => MoveObject(-step));

        Vector3 vec = new Vector3(0.1f, 0.08f, 0.0f);
        stepSelectorMenu.transform.position = HEndEffectorTransform.Instance.newTransform.transform.position + vec;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Selected axis: " + HEndEffectorTransform.Instance.selectedAxis;
    }

    private void MoveObject(float step) {

        switch (HEndEffectorTransform.Instance.selectedAxis) {
            case Gizmo.Axis.X:
                HEndEffectorTransform.Instance.newTransform.position += new Vector3(0.0f, 0.0f, step);
                break;
            case Gizmo.Axis.Y:
                HEndEffectorTransform.Instance.newTransform.position += new Vector3(step, 0.0f, 0.0f);
                break;
            case Gizmo.Axis.Z:
                HEndEffectorTransform.Instance.newTransform.position += new Vector3(0.0f, step, 0.0f);
                break;
            default:
                return;
        }

        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(HEndEffectorTransform.Instance.tmpModel.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);
        HEndEffectorTransform.Instance.SetIKToModel(HEndEffectorTransform.Instance.defaultOrientation, position);
    }


    public void setStep(float number) {
        Debug.Log(number);
        step = number;
    }

}
