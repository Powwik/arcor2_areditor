using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using Hololens;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using RosSharp.Urdf;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Utilities;

public class HEndEffectorTransform2 : Singleton<HEndEffectorTransform2>
{
    public Transform sceneOrigin;
    public GameObject testingEndPoint;
    public GameObject testingCube;
    public Material transparentMaterial;
    public HActionPoint3D point;
    public GameObject gizmoPrefab;
    public Transform gizmoTransform;
    private HGizmo gizmo;
    public HActionPoint3D tmpModel;
    public GameObject confirmationWindow;
    public Interactable confirmButton;
    public Interactable resetButton;

    public Gizmo.Axis selectedAxis;
    public bool isManipulating = false;
    public bool canMove = true;

    public HIRobot selectedRobot;
    public HRobotEE selectedEndEffector;

    HInteractiveObject Robot;

    float finalTime = 0.0f;
    private bool startCounting;

    //private Dictionary<string, float> robotVisibilityBackup = new Dictionary<string, float>();
    public Orientation defaultOrientation = new Orientation(-0.3026389978045349m, 0.9531052601931577m, -0.0000000000000000185312939979m, 0.0000000000000000583608653073m);

    // Start is called before the first frame update
    void Start() {
        confirmButton.OnClick.AddListener(() => confirmClicked());
        resetButton.OnClick.AddListener(() => resetClicked());
        selectedAxis = Gizmo.Axis.X;
    }

    // Update is called once per frame
    void Update() {
        if (isManipulating) {

            MoveModel();
        }
        if (startCounting)
            finalTime += Time.deltaTime;

        IMixedRealityPointer pointer = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (pointer != null)
        {
            GameObject focusedObject = CoreServices.InputSystem.FocusProvider.GetFocusedObject(pointer);
            if (focusedObject && focusedObject.transform.parent) {
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
                        break;
                }
            }
        }

        if (selectedAxis != Gizmo.Axis.NONE) {
            switch (selectedAxis) {
                case Gizmo.Axis.X:
                    Vector3 vecX = 0.25f * Vector3.forward;
                    HStepSelectorMenu2.Instance.StepSelectorMenu.transform.localPosition = gizmoTransform.localPosition + vecX;
                    break;
                case Gizmo.Axis.Y:
                    Vector3 vecY = 0.25f * Vector3.right;
                    HStepSelectorMenu2.Instance.StepSelectorMenu.transform.localPosition = gizmoTransform.localPosition + vecY;
                    break;
                case Gizmo.Axis.Z:
                    Vector3 vecZ = 0.23f * Vector3.up;
                    HStepSelectorMenu2.Instance.StepSelectorMenu.transform.localPosition = gizmoTransform.localPosition + vecZ;
                    break;
                default:
                    break;
            }

            if (HSelectorManager.Instance.whichExperiment == 2) {
                if (selectedAxis == Gizmo.Axis.Z) {
                    Vector3 vec = gizmoTransform.position + new Vector3(0.0f, 0.32f, 0.0f);
                    confirmationWindow.transform.position = vec;
                } else {
                    Vector3 vec = gizmoTransform.position + new Vector3(0.0f, 0.25f, 0.0f);
                    confirmationWindow.transform.position = vec;
                }
                testingEndPoint.transform.localPosition = gizmoTransform.localPosition - new Vector3(0.0f, 0.01367547f, 0.0f);
            }
        }
    }

    public void updatePosition() {
        isManipulating = false;
        // set position of the confirmation window
        Vector3 vec = gizmoTransform.position + new Vector3(0.0f, 0.38f, 0.0f);
        confirmationWindow.transform.position = vec;
        confirmationWindow.gameObject.SetActive(true);

        Vector3 stepMenuVector = gizmoTransform.position + new Vector3(0.0f, 0.3f, 0.0f);
        HStepSelectorMenu2.Instance.StepSelectorMenu.transform.position = stepMenuVector;
        HStepSelectorMenu2.Instance.StepSelectorMenu.SetActive(true);
    }

    public void manipulation() {
        isManipulating = true;
        HStepSelectorMenu2.Instance.StepSelectorMenu.gameObject.SetActive(false);
        confirmationWindow.gameObject.SetActive(false);
    }

    public void confirmClicked() {
        if (HSelectorManager.Instance.whichExperiment == 2) {
            MoveRobot();
            Debug.Log("FINAL TIME: " + finalTime);
            Debug.Log("FINAL DISTANCE: " + Vector3.Distance(testingCube.transform.localPosition, testingEndPoint.transform.localPosition));
            startCounting = false;
            confirmationWindow.SetActive(false);
        }
    }

    public void resetClicked() {
        finalTime = 0.0f;
        confirmationWindow.gameObject.SetActive(false);
        HStepSelectorMenu.Instance.stepSelectorMenu.SetActive(false);
        gizmoTransform.position = selectedEndEffector.transform.position;
        Vector3 p = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
        Position position = DataHelper.Vector3ToPosition(p);
        SetIKToModel(defaultOrientation, position);
    }

    public async void activeEndEffectorTranform2(HInteractiveObject robot) {
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => updatePosition());
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => manipulation());
        startCounting = true;
        finalTime = 0.0f;
        HStepSelectorMenu2.Instance.StepSelectorMenu.SetActive(true);
        Robot = robot;

        testingCube.transform.SetParent(sceneOrigin);
        testingCube.gameObject.SetActive(true);
        testingCube.transform.localPosition = new Vector3(-0.14f, 0.03f, -0.065f);
        testingCube.transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

        foreach (var collider in robot.transform.GetComponentsInChildren<Collider>()) {
            collider.enabled = false;
        }

        foreach (var renderer in robot.GetComponentsInChildren<Renderer>()) {
            renderer.material = transparentMaterial;
        }

        // set default pose for the previously selected robot
        if (selectedRobot != (RobotActionObjectH) robot && selectedRobot != null) {
            Vector3 p1 = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
            Position position1 = DataHelper.Vector3ToPosition(p1);
            SetIKToModel(defaultOrientation, position1);
        }

        if (selectedRobot != (RobotActionObjectH) robot) {
            if (gizmo)
                Destroy(gizmo.gameObject);
            if (tmpModel)
                Destroy(tmpModel.gameObject);

            selectedRobot = (RobotActionObjectH) robot;
            List<HRobotEE> ee = await selectedRobot.GetAllEE();
            selectedEndEffector = ee[0];

            // create new action point for manipulation
            tmpModel = Instantiate(point, selectedEndEffector.transform.position, selectedEndEffector.transform.rotation);
            tmpModel.transform.localScale = Vector3.one / 2;

            gizmoTransform.transform.position = tmpModel.transform.position;
            gizmoTransform.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            tmpModel.transform.SetParent(gizmoTransform);
            tmpModel.setInterarction(gizmoTransform.gameObject);
            tmpModel.EnableOffscreenIndicator(false);
            gizmoTransform.gameObject.SetActive(true);

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
                sourceTransform = gizmoTransform
            };
            ObjectManipulator[] o = gizmo.gameObject.GetComponentsInChildren<ObjectManipulator>();
            gizmo.gameObject.GetComponent<ScaleConstraint>().AddSource(source);

            foreach (ObjectManipulator objectManipulator in o) {
                objectManipulator.HostTransform = gizmoTransform;
                objectManipulator.OnManipulationStarted.AddListener((s) => manipulation());
                objectManipulator.OnManipulationEnded.AddListener((s) => updatePosition());
            }
            await PrepareRobotModel(selectedRobot.GetId(), false);
        }
        Vector3 p = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
        Position position = DataHelper.Vector3ToPosition(p);
        SetIKToModel(defaultOrientation, position);
    }

    public void deactiveEndEffectorTransform2() {
        gizmoTransform.gameObject.SetActive(false);
        confirmationWindow.SetActive(false);
        if(tmpModel)
            Destroy(tmpModel.gameObject);
        if (gizmo)
            Destroy(gizmo.gameObject);
        tmpModel = null;
        gizmo = null;
        selectedRobot = null;
        HStepSelectorMenu2.Instance.StepSelectorMenu.gameObject.SetActive(false);
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

        foreach (var collider in Robot.transform.GetComponentsInChildren<Collider>()) {
            collider.enabled = true;
        }
        startCounting = false;
        finalTime = 0.0f;
    }

    public async void MoveModel() {
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(tmpModel.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);

        SetIKToModel(defaultOrientation, position);
    }

    public async void SetIKToModel(Orientation or, Position pos) {
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

            foreach (IO.Swagger.Model.Joint joint in modelJoints) {
                SceneManagerH.Instance.SelectedRobot.SetJointValue(joint.Name, (float) joint.Value);
            }

        } catch (ItemNotFoundException ex) {
            canMove = false;
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        } catch (RequestFailedException ex) {
            canMove = false;
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        }
    }

    private async void MoveRobot() {
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

    private async Task PrepareRobotModel(string robotID, bool shadowRealRobot) {
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
