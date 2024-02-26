using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using Hololens;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Animations;

public class HEndEffectorTransform : Singleton<HEndEffectorTransform>
{
    public HActionPoint3D point;
    public GameObject gizmoPrefab;
    public Transform newTransform;
    private HGizmo gizmo;
    public HActionPoint3D tmpModel;
    public GameObject confirmationWindow;
    public Interactable confirmButton;
    public Interactable resetButton;

    public Gizmo.Axis selectedAxis;
    GameObject axis;

    public bool isManipulating = false;

    public HIRobot selectedRobot;
    public HRobotEE selectedEndEffector;

    private ProjectRobotJoints joints;

    //private Dictionary<string, float> robotVisibilityBackup = new Dictionary<string, float>();
    public Orientation defaultOrientation = new Orientation(-0.3026389978045349m, 0.9531052601931577m, -0.0000000000000000185312939979m, 0.0000000000000000583608653073m);

    // Start is called before the first frame update
    void Start()
    {
        newTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => updatePosition());
        newTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => manipulation());
        confirmButton.OnClick.AddListener(() => confirmClicked());
        resetButton.OnClick.AddListener(() => resetClicked());
        selectedAxis = Gizmo.Axis.NONE;
    }

    private IMixedRealityHand currentHand;
    private Vector3 previousHandPosition;
    private Vector3 previousModelPosition;
    private float movementThreshold = 0.001f;

    private IMixedRealityHand GetHand() {
        // Get the first detected hand from the input system
        foreach (var inputSource in CoreServices.InputSystem.DetectedInputSources) {
            if (inputSource.Pointers[0].Controller != null && inputSource.Pointers[0].Controller is IMixedRealityHand hand) {
                return hand;
            }
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if (isManipulating) {

            MoveModel();
        }
    }

    private void UpdateTranslate(Vector3 direction) {
        if (tmpModel == null)
            return;

        if (selectedAxis != Gizmo.Axis.NONE) {
            float step = 0.01f;
            Vector3 movement = Vector3.zero;

            // Determine the movement along the selected axis based on the direction of the hand movement
            switch (selectedAxis) {
                case Gizmo.Axis.X:
                    movement = direction.x > 0 ? Vector3.right : Vector3.left;
                    break;
                case Gizmo.Axis.Y:
                    movement = direction.y > 0 ? Vector3.up : Vector3.down;
                    break;
                case Gizmo.Axis.Z:
                    movement = direction.z > 0 ? Vector3.forward : Vector3.back;
                    break;
                default:
                    break;
            }

            // Apply the step movement along the selected axis
            tmpModel.transform.position += movement * step;
        }
    }


    public void updatePosition()
    {
        isManipulating = false;
        // set position of the confirmation window
        Vector3 vec = new Vector3(0.1f, 0.08f, 0.0f);
        confirmationWindow.transform.position = tmpModel.transform.position + vec;

        confirmationWindow.gameObject.SetActive(true);

        if (selectedAxis != Gizmo.Axis.NONE) {
            Debug.Log("Gameobject: " + axis);
            Vector3 objectPosition = axis.transform.GetChild(0).position;
            HStepSelectorMenu.Instance.stepSelectorMenu.transform.position = objectPosition;
            
            HStepSelectorMenu.Instance.stepSelectorMenu.SetActive(true);
        }
    }

    public void manipulation()
    {
        isManipulating = true;

        IMixedRealityPointer pointer = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (point != null) {
            GameObject focusedObject = CoreServices.InputSystem.FocusProvider.GetFocusedObject(pointer);
            switch (focusedObject.transform.parent.name) {
                case "x_axis":
                    selectedAxis = Gizmo.Axis.X;
                    break;
                case "y_axis":
                    selectedAxis = Gizmo.Axis.Y;
                    break;
                case "z_axis":
                    selectedAxis = Gizmo.Axis.Z;
                    break;
                default:
                    selectedAxis = Gizmo.Axis.NONE;
                    break;
            }

            if (selectedAxis != Gizmo.Axis.NONE)
                axis = focusedObject.transform.parent.gameObject;
        }
        HStepSelectorMenu.Instance.stepSelectorMenu.SetActive(false);
        confirmationWindow.gameObject.SetActive(false);
    }

    public void confirmClicked()
    {
        MoveRobot();
    }

    public void resetClicked()
    {
        confirmationWindow.gameObject.SetActive(false);
        HStepSelectorMenu.Instance.stepSelectorMenu.SetActive(false);
        newTransform.position = selectedEndEffector.transform.position;
        Vector3 p = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
        Position position = DataHelper.Vector3ToPosition(p);
        SetIKToModel(defaultOrientation, position);
    }

    public async void activeEndEffectorTranform(HInteractiveObject robot)
    {
        // set default pose for the previously selected robot
        if (selectedRobot != (RobotActionObjectH) robot && selectedRobot != null)
        {
            Vector3 p = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
            Position position = DataHelper.Vector3ToPosition(p);
            SetIKToModel(defaultOrientation, position);
        }

        if (selectedRobot != (RobotActionObjectH) robot)
        {
            if (gizmo)
                Destroy(gizmo.gameObject);
            if(tmpModel)
                Destroy(tmpModel.gameObject);

            selectedRobot = (RobotActionObjectH) robot;
            List<HRobotEE> ee = await selectedRobot.GetAllEE();
            selectedEndEffector = ee[0];

            // create new action point for manipulation
            tmpModel = Instantiate(point, selectedEndEffector.transform.position, selectedEndEffector.transform.rotation);
            tmpModel.transform.localScale = Vector3.one / 2;

            newTransform.transform.position = tmpModel.transform.position;
            newTransform.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            tmpModel.transform.SetParent(newTransform);
            tmpModel.setInterarction(newTransform.gameObject);
            tmpModel.EnableOffscreenIndicator(false);
            newTransform.gameObject.SetActive(true);

            // create gizmo model for manipulation
            gizmo = Instantiate(gizmoPrefab).GetComponent<HGizmo>();
            gizmo.transform.SetParent(tmpModel.transform);

            gizmo.transform.localScale = new Vector3(0.1f / tmpModel.transform.localScale.x, 0.1f / tmpModel.transform.localScale.y, 0.1f / tmpModel.transform.localScale.z);

            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.eulerAngles = tmpModel.transform.eulerAngles;
            gizmo.SetXDelta(0);
            gizmo.SetYDelta(0);
            gizmo.SetZDelta(0);
            
            ConstraintSource source = new ConstraintSource {
                sourceTransform = newTransform
            };
            ObjectManipulator[] o = gizmo.gameObject.GetComponentsInChildren<ObjectManipulator>();
            gizmo.gameObject.GetComponent<ScaleConstraint>().AddSource(source);

            foreach (ObjectManipulator objectManipulator in o) {
                objectManipulator.HostTransform = newTransform;
                objectManipulator.OnManipulationStarted.AddListener((s) => manipulation());
                objectManipulator.OnManipulationEnded.AddListener((s) => updatePosition());
            }
            await PrepareRobotModel(selectedRobot.GetId(), false);
        }
    }

    public void deactiveEndEffectorTransform()
    {
        newTransform.gameObject.SetActive(false);
        confirmationWindow.SetActive(false);
        Destroy(tmpModel.gameObject);
        Destroy(gizmo.gameObject);
        tmpModel = null;
        gizmo = null;
    }

    public async void MoveModel()
    {
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(tmpModel.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);

        SetIKToModel(defaultOrientation, position);
    }

    public async void SetIKToModel(Orientation or, Position pos)
    {
        try {
            IO.Swagger.Model.Pose pose = new IO.Swagger.Model.Pose(orientation: or, position: pos);
            List<IO.Swagger.Model.Joint> startJoints = selectedRobot.GetJoints();
            SceneManagerH.Instance.SelectedRobot = SceneManagerH.Instance.GetRobot(selectedRobot.GetId());
            List<IO.Swagger.Model.Joint> modelJoints = await WebSocketManagerH.Instance.InverseKinematics(
                selectedRobot.GetId(),
                selectedEndEffector.GetName(),
                true,
                pose,
                startJoints);

            joints = new ProjectRobotJoints("joints", selectedRobot.GetId(), modelJoints);


            foreach (IO.Swagger.Model.Joint joint in modelJoints) {
                SceneManagerH.Instance.SelectedRobot.SetJointValue(joint.Name, (float) joint.Value);
            }

        } catch (ItemNotFoundException ex) { 
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        }
    }

    private async void MoveRobot()
    {
        try {
            string armId = null;
            if (SceneManagerH.Instance.SelectedRobot.MultiArm())
                armId = SceneManagerH.Instance.SelectedArmId;

            Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(tmpModel.transform.position));
            Position position = DataHelper.Vector3ToPosition(point);

            await WebSocketManagerH.Instance.MoveToPose(selectedRobot.GetId(), selectedEndEffector.GetName(), 1.0m, position, defaultOrientation);

        } catch (ItemNotFoundException ex) {
            Notifications.Instance.ShowNotification("Failed to move robot", ex.Message);
            return;
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to move robot", ex.Message);
            return;
        }
    }

    private async Task PrepareRobotModel(string robotID, bool shadowRealRobot)
    {
        //if (shadowRealRobot) {
        //    robotVisibilityBackup.TryGetValue(robotID, out float originalVisibility);
        //    SceneManagerH.Instance.GetActionObject(robotID).SetVisibility(originalVisibility);
        //} else {
        //    if (!robotVisibilityBackup.TryGetValue(robotID, out _)) {
        //        robotVisibilityBackup.Add(robotID, SceneManagerH.Instance.GetActionObject(robotID).GetVisibility());
        //        SceneManagerH.Instance.GetActionObject(robotID).SetVisibility(1f);
        //    }
        //}

        if (SceneManagerH.Instance.SceneStarted) {
            await WebSocketManagerH.Instance.RegisterForRobotEvent(robotID, shadowRealRobot, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
            SceneManagerH.Instance.GetRobot(robotID).SetGrey(!shadowRealRobot, true);
            //SceneManagerH.Instance.GetActionObject(robotID).SetInteractivity(shadowRealRobot);
        }
        //await WebSocketManagerH.Instance.RegisterForRobotEvent(robotID, shadowRealRobot, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
    }
}
