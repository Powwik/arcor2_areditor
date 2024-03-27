using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Base;
using Hololens;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using RestSharp.Extensions;
using RosSharp.Urdf;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Utilities;

public class HEndEffectorTransform : Singleton<HEndEffectorTransform>
{
    public Transform sceneOrigin;

    public HActionPoint3D PointPrefab;
    public GameObject gizmoPrefab;
    public GameObject emptyGizmoPrefab;
    public Transform gizmoTransform;
    private HGizmo gizmo;
    private GameObject slowGizmo;
    public HActionPoint3D slowSphere;
    public HActionPoint3D tmpModel;

    public Gizmo.Axis selectedAxis;

    public bool isManipulating = false;
    public bool canMove = true;

    public HIRobot selectedRobot;
    public HRobotEE selectedEndEffector;

    public Material transparentMaterial;

    HInteractiveObject Robot;

    //private Dictionary<string, float> robotVisibilityBackup = new Dictionary<string, float>();
    public Orientation defaultOrientation = new Orientation(-0.3026389978045349m, 0.9531052601931577m, -0.0000000000000000185312939979m, 0.0000000000000000583608653073m);

    // Start is called before the first frame update
    private void Start()
    {
        selectedAxis = Gizmo.Axis.X;
    }

    // Update is called once per frame
    private void Update()
    {
        if (isManipulating)
        {
            MoveModel();
        }

        IMixedRealityPointer pointer = CoreServices.InputSystem.FocusProvider.PrimaryPointer;
        if (pointer != null)
        {
            GameObject focusedObject = CoreServices.InputSystem.FocusProvider.GetFocusedObject(pointer);
            if (focusedObject && focusedObject.transform.parent)
            {
                switch (focusedObject.transform.parent.name)
                {
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
            }
        }
    }

    public void UpdatePosition()
    {
        isManipulating = false;
        gizmoTransform.GetComponent<BoundsControl>().enabled = true;

        gizmoTransform.position = slowGizmo.transform.position;

        Vector3 vec = gizmoTransform.position + HConfirmationWindow.Instance.ConfirmationMenuVectorUp;
        HConfirmationWindow.Instance.ConfirmationWindow.transform.localPosition = vec;
        HConfirmationWindow.Instance.ConfirmationWindow.gameObject.SetActive(true);

        Vector3 sliderVector = gizmoTransform.position + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.position = sliderVector;
        HSliderMenu.Instance.SliderMenu.SetActive(true);

        HAxisMenu.Instance.AxisMenu.SetActive(true);

        Destroy(slowSphere.gameObject);

        foreach (var render in tmpModel.transform.GetComponentsInChildren<Renderer>())
        {
            render.enabled = true;
        }
    }

    public void Manipulation()
    {
        isManipulating = true;

        gizmoTransform.GetComponent<BoundsControl>().enabled = false;

        slowSphere = Instantiate(PointPrefab);
        slowSphere.gameObject.AddComponent<HFollowObject>().SetFollowingObject(gizmoTransform.gameObject);
        slowSphere.transform.SetParent(sceneOrigin);
        slowSphere.transform.position = tmpModel.transform.position;
        slowSphere.transform.localScale = Vector3.one / 2;

        slowGizmo = Instantiate(emptyGizmoPrefab);
        slowGizmo.transform.SetParent(slowSphere.transform);
        slowGizmo.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        slowGizmo.transform.position = tmpModel.transform.position;
        slowGizmo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        HSliderMenu.Instance.SliderMenu.SetActive(false);
        HAxisMenu.Instance.AxisMenu.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.gameObject.SetActive(false);

        foreach (var render in tmpModel.transform.GetComponentsInChildren<Renderer>())
        {
            render.enabled = false;
        }
    }

    public void ConfirmClicked()
    {
        MoveRobot();
    }

    public void ResetClicked()
    {
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);
        SetIKToModel(defaultOrientation, position);

        gizmoTransform.position = selectedEndEffector.transform.position;

        Vector3 sliderVector = gizmoTransform.position + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.position = sliderVector;
    }

    public async void ActiveEndEffectorTranform(HInteractiveObject robot)
    {
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => UpdatePosition());
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => Manipulation());
        Robot = robot;

        HSliderMenu.Instance.SliderMenu.SetActive(true);
        HAxisMenu.Instance.AxisMenu.SetActive(true);

        foreach (var collider in robot.transform.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        foreach (var renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = transparentMaterial;
        }

        // set default pose for the previously selected robot
        if (selectedRobot != (RobotActionObjectH) robot && selectedRobot != null)
        {
            Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
            Position position = DataHelper.Vector3ToPosition(point);
            SetIKToModel(defaultOrientation, position);
        }

        if (selectedRobot != (RobotActionObjectH) robot)
        {
            if (gizmo)
                Destroy(gizmo.gameObject);
            if (tmpModel)
                Destroy(tmpModel.gameObject);

            selectedRobot = (RobotActionObjectH) robot;
            List<HRobotEE> ee = await selectedRobot.GetAllEE();
            selectedEndEffector = ee[0];

            // create new action point for manipulation
            tmpModel = Instantiate(PointPrefab, selectedEndEffector.transform.position, selectedEndEffector.transform.rotation);
            tmpModel.transform.localScale = Vector3.one / 2;

            gizmoTransform.transform.position = tmpModel.transform.position;
            gizmoTransform.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            tmpModel.transform.SetParent(gizmoTransform);
            tmpModel.setInterarction(gizmoTransform.gameObject);
            tmpModel.EnableOffscreenIndicator(false);
            gizmoTransform.gameObject.SetActive(true);

            // create gizmo model for manipulation
            gizmo = Instantiate(gizmoPrefab).GetComponent<HGizmo>();
            gizmo.transform.SetParent(tmpModel.transform);
            gizmo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            gizmo.transform.localPosition = Vector3.zero;
            gizmo.transform.eulerAngles = tmpModel.transform.eulerAngles;
            gizmo.SetXDelta(0);
            gizmo.SetYDelta(0);
            gizmo.SetZDelta(0);

            ConstraintSource source = new ConstraintSource
            {
                sourceTransform = gizmoTransform
            };
            ObjectManipulator[] o = gizmo.gameObject.GetComponentsInChildren<ObjectManipulator>();
            gizmo.gameObject.GetComponent<ScaleConstraint>().AddSource(source);

            foreach (ObjectManipulator objectManipulator in o)
            {
                objectManipulator.HostTransform = gizmoTransform;
                objectManipulator.OnManipulationStarted.AddListener((s) => Manipulation());
                objectManipulator.OnManipulationEnded.AddListener((s) => UpdatePosition());
            }
            await PrepareRobotModel(selectedRobot.GetId(), false);
        }

        Vector3 p1 = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(selectedEndEffector.transform.position));
        Position position1 = DataHelper.Vector3ToPosition(p1);
        SetIKToModel(defaultOrientation, position1);
    }

    public void DeactiveEndEffectorTransform()
    {
        gizmoTransform.gameObject.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        if (tmpModel)
            Destroy(tmpModel.gameObject);
        if (gizmo)
            Destroy(gizmo.gameObject);
        if (slowGizmo)
            Destroy(slowGizmo.gameObject);

        slowGizmo = null;
        tmpModel = null;
        gizmo = null;
        selectedRobot = null;

        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

        HSliderMenu.Instance.SliderMenu.SetActive(false);

        foreach (var collider in Robot.transform.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }
    }

    private async void MoveModel()
    {
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(slowGizmo.transform.position));
        Position position = DataHelper.Vector3ToPosition(point);

        SetIKToModel(defaultOrientation, position);
    }

    public async void SetIKToModel(Orientation or, Position pos)
    {
        try
        {
            IO.Swagger.Model.Pose pose = new IO.Swagger.Model.Pose(orientation: or, position: pos);
            List<IO.Swagger.Model.Joint> startJoints = selectedRobot.GetJoints();
            SceneManagerH.Instance.SelectedRobot = SceneManagerH.Instance.GetRobot(selectedRobot.GetId());
            List<IO.Swagger.Model.Joint> modelJoints = await WebSocketManagerH.Instance.InverseKinematics(
                selectedRobot.GetId(),
                selectedEndEffector.GetName(),
                true,
                pose,
                startJoints);

            foreach (IO.Swagger.Model.Joint joint in modelJoints)
            {
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

    private async void MoveRobot()
    {
        try
        {
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
        if (SceneManagerH.Instance.SceneStarted)
        {
            await WebSocketManagerH.Instance.RegisterForRobotEvent(robotID, shadowRealRobot, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
            SceneManagerH.Instance.GetRobot(robotID).SetGrey(!shadowRealRobot, true);
        }
    }
}
