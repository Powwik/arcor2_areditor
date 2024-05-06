using System.Collections;
using System.Collections.Generic;
using Base;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using LibTessDotNet;

/*********************************************************************
 * \file HAxisMenu.cs
 * \the script manipulates with the robot using (+, -) buttons
 * 
 * \author Daniel Zmrzlý
 *********************************************************************/
public class HAxisMenu : Singleton<HAxisMenu> {

    public GameObject AxisMenu;
    public Interactable PositiveButton;
    public Interactable NegativeButton;
    private float Unit;
    private Gizmo.Axis SelectedAxis;

    // Start is called before the first frame update
    private void Start()
    {
        Unit = 0.001f;
        PositiveButton.OnClick.AddListener(() => MoveObject(Unit));
        NegativeButton.OnClick.AddListener(() => MoveObject(-Unit));
    }

    // Update is called once per frame
    private void Update()
    {
        // get pointer of the hand
        IMixedRealityPointer pointer = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (pointer != null)
        {
            // check if the focused object is the axis of the gizmo
            GameObject focusedObject = CoreServices.InputSystem.FocusProvider.GetFocusedObject(pointer);
            if (focusedObject && focusedObject.transform.parent)
            {
                switch (focusedObject.transform.parent.name)
                {
                    case "x_axis":
                        SelectedAxis = Gizmo.Axis.X;
                        break;
                    case "y_axis":
                        SelectedAxis = Gizmo.Axis.Y;
                        break;
                    case "z_axis":
                        SelectedAxis = Gizmo.Axis.Z;
                        break;
                    default:
                        break;
                }
            }
        }

        // show manipulation buttons next to the selected axis
        if (SelectedAxis != Gizmo.Axis.NONE)
        {
            switch (SelectedAxis)
            {
                case Gizmo.Axis.X:
                    Vector3 vecX = 0.21f * Vector3.forward;
                    AxisMenu.transform.localPosition = HEndEffectorTransform.Instance.gizmoTransform.localPosition + vecX;
                    gameObject.GetComponent<GridObjectCollection>().Layout = LayoutOrder.Vertical;
                    break;
                case Gizmo.Axis.Y:
                    Vector3 vecY = 0.21f * Vector3.right;
                    AxisMenu.transform.localPosition = HEndEffectorTransform.Instance.gizmoTransform.localPosition + vecY;
                    gameObject.GetComponent<GridObjectCollection>().Layout = LayoutOrder.Vertical;
                    break;
                case Gizmo.Axis.Z:
                    Vector3 vecZ = 0.21f * Vector3.up;
                    AxisMenu.transform.localPosition = HEndEffectorTransform.Instance.gizmoTransform.localPosition + vecZ;
                    gameObject.GetComponent<GridObjectCollection>().Layout = LayoutOrder.Horizontal;
                    break;
                default:
                    break;
            }
            gameObject.GetComponent<GridObjectCollection>().UpdateCollection();
        }
    }

    /**
     * Function moves model of the robot to the position of the end effector
     * 
     * \param[in] s      unit expressed in float
     */
    private void MoveObject(float s)
    {
        switch (SelectedAxis)
        {
            case Gizmo.Axis.X:
                HEndEffectorTransform.Instance.gizmoTransform.localPosition += new Vector3(0.0f, 0.0f, s);
                float deltaX = HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().GetXDelta();
                HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().SetXDelta(deltaX + s);
                break;
            case Gizmo.Axis.Y:
                HEndEffectorTransform.Instance.gizmoTransform.localPosition += new Vector3(s, 0.0f, 0.0f);
                float deltaY = HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().GetYDelta();
                HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().SetYDelta(deltaY + s);
                break;
            case Gizmo.Axis.Z:
                HEndEffectorTransform.Instance.gizmoTransform.localPosition += new Vector3(0.0f, s, 0.0f);
                float deltaZ = HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().GetZDelta();
                HEndEffectorTransform.Instance.gizmo.gameObject.GetComponent<HGizmo>().SetZDelta(deltaZ + s);
                break;
            default:
                return;
        }

        // update position of the confirmation menu
        Vector3 confirmV = HEndEffectorTransform.Instance.gizmoTransform.localPosition + HConfirmationWindow.Instance.ConfirmationMenuVectorUp;
        HConfirmationWindow.Instance.ConfirmationWindow.transform.localPosition = confirmV;
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(true);

        // update position of the slider menu
        Vector3 sliderV = HEndEffectorTransform.Instance.gizmoTransform.localPosition + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.localPosition = sliderV;

        // move robot model
        HEndEffectorTransform.Instance.MoveModel(HEndEffectorTransform.Instance.DefaultOrientation, HEndEffectorTransform.Instance.gizmoTransform, HEndEffectorTransform.MoveOption.Buttons);
    }
}
