using System.Collections;
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
using RosSharp.Urdf;
using UnityEngine;
using UnityEngine.Animations;

public class HEndEffectorTransform : Singleton<HEndEffectorTransform>
{
    public Transform SceneOrigin;

    public HActionPoint3D PointPrefab;
    public GameObject GizmoPrefab;
    public GameObject EmptyGizmoPrefab;
    public Transform gizmoTransform;
    private HGizmo gizmo;
    private GameObject slowGizmo;
    public HActionPoint3D slowSphere;
    private HActionPoint3D tmpModel;

    public Gizmo.Axis selectedAxis;

    private bool isManipulating = false;

    public HIRobot selectedRobot;
    public HRobotEE selectedEndEffector;

    public Material TransparentMaterial;
    public Material RedMaterial;

    private HInteractiveObject Robot;

    private Vector3 lastValidPosition;
    private Vector3 previousPosition;

    private Dictionary<string, Material> materialsBackup = new Dictionary<string, Material>();

    public Orientation DefaultOrientation = new Orientation(0.0m, 1.0m, 0.0m, 0.0m);

    public enum MoveOption {
        Buttons,
        DirectManipulation,
        None,
    }

    // Start is called before the first frame update
    private void Start()
    {
        selectedAxis = Gizmo.Axis.X;
    }

    // Update is called once per frame
    private async void Update()
    {
        if (isManipulating)
        {
            Vector3 currentPosition = slowSphere.transform.localPosition;

            if (Vector3.Distance(currentPosition, previousPosition) > 0.00001f)
            {
                await MoveModel(DefaultOrientation, slowGizmo.transform, MoveOption.DirectManipulation);
            }
            previousPosition = currentPosition;
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

        gizmoTransform.position = lastValidPosition;

        Vector3 vec = gizmoTransform.localPosition + HConfirmationWindow.Instance.ConfirmationMenuVectorUp;
        HConfirmationWindow.Instance.ConfirmationWindow.transform.localPosition = vec;
        HConfirmationWindow.Instance.ConfirmationWindow.gameObject.SetActive(true);

        Vector3 sliderVector = gizmoTransform.localPosition + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.localPosition = sliderVector;
        HSliderMenu.Instance.SliderMenu.SetActive(true);

        HAxisMenu.Instance.AxisMenu.SetActive(true);

        Destroy(slowSphere.gameObject);

        foreach (Renderer render in tmpModel.transform.GetComponentsInChildren<Renderer>())
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
        slowSphere.transform.SetParent(SceneOrigin);
        slowSphere.transform.position = gizmoTransform.position;
        slowSphere.transform.localScale = Vector3.one / 2;

        slowGizmo = Instantiate(EmptyGizmoPrefab);
        slowGizmo.transform.SetParent(slowSphere.transform);
        slowGizmo.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        slowGizmo.transform.position = gizmoTransform.position;
        slowGizmo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        HSliderMenu.Instance.SliderMenu.SetActive(false);
        HAxisMenu.Instance.AxisMenu.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.gameObject.SetActive(false);

        foreach (Renderer render in tmpModel.transform.GetComponentsInChildren<Renderer>())
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
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);

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

        foreach (Collider collider in robot.transform.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        ChangeMaterialOfRobot(Robot, TransparentMaterial);

        // set default pose for the previously selected robot
        if (selectedRobot != (RobotActionObjectH) robot && selectedRobot != null)
        {
            MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);
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
            gizmo = Instantiate(GizmoPrefab).GetComponent<HGizmo>();
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
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);
    }

    public void DeactiveEndEffectorTransform()
    {
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);

        gizmoTransform.gameObject.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        if (tmpModel)
            Destroy(tmpModel.gameObject);
        if (gizmo)
            Destroy(gizmo.gameObject);
        if (slowSphere)
            Destroy(slowSphere.gameObject);

        slowSphere = null;
        tmpModel = null;
        gizmo = null;
        selectedRobot = null;

        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

        HSliderMenu.Instance.SliderMenu.SetActive(false);
        HAxisMenu.Instance.AxisMenu.SetActive(false);

        foreach (Collider collider in Robot.transform.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

        foreach (Renderer renderer in Robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = materialsBackup[renderer.transform.name];
        }
        materialsBackup.Clear();
    }

    public async Task MoveModel(Orientation or, Transform t, MoveOption moveOption)
    {
        Vector3 transformPosition = t.position;
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(transformPosition));
        Position position = DataHelper.Vector3ToPosition(point);
        try {
            IO.Swagger.Model.Pose pose = new(orientation: or, position: position);
            List<IO.Swagger.Model.Joint> startJoints = selectedRobot.GetJoints();
            SceneManagerH.Instance.SelectedRobot = SceneManagerH.Instance.GetRobot(selectedRobot.GetId());
            List<IO.Swagger.Model.Joint> modelJoints = await WebSocketManagerH.Instance.InverseKinematics(
                selectedRobot.GetId(),
                selectedEndEffector.GetName(),
                true,
                pose,
                startJoints);

            lastValidPosition = transformPosition;

            foreach (IO.Swagger.Model.Joint joint in modelJoints)
            {
                SceneManagerH.Instance.SelectedRobot.SetJointValue(joint.Name, (float) joint.Value);
            }
        }
        catch (RequestFailedException)
        {
            SetErrorMaterial(Robot, RedMaterial);

            switch (moveOption)
            {
                case MoveOption.DirectManipulation:
                    if (slowSphere)
                        slowSphere.transform.position = lastValidPosition;
                    break;
                case MoveOption.Buttons:
                    gizmoTransform.position = lastValidPosition;
                    break;
            }
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

            await WebSocketManagerH.Instance.MoveToPose(selectedRobot.GetId(), selectedEndEffector.GetName(), 1.0m, position, DefaultOrientation);

        } catch (ItemNotFoundException ex) {
            Notifications.Instance.ShowNotification("Failed to move robot", ex.Message);
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to move robot", ex.Message);
            HNotificationWindow.Instance.ShowNotification("Unable to move robot here.");
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

    private void ChangeMaterialOfRobot(HInteractiveObject robot, Material material)
    {
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    private void SetErrorMaterial(HInteractiveObject robot, Material material)
    {
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
        StartCoroutine(HideErrorMaterial(robot, TransparentMaterial));
    }

    private IEnumerator HideErrorMaterial(HInteractiveObject robot, Material material)
    {
        yield return new WaitForSeconds(0.3f);
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    private void RemoveColliderFromActionObjects()
    {

    }

    private void SetColliderFromActionObjects()
    {

    }
}
