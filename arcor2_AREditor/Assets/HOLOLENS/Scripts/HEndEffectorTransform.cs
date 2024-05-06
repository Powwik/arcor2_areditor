using System;
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
using TriLibCore.General;
using UnityEngine;
using UnityEngine.Animations;


/*********************************************************************
 * \file HEndEffectorTransform.cs
 * \the main script for the manipulation
 * 
 * \author Daniel Zmrzlý
 *********************************************************************/
public class HEndEffectorTransform : Singleton<HEndEffectorTransform>
{
    public Transform SceneOrigin;

    public HActionPoint3D PointPrefab;
    public GameObject GizmoPrefab;
    public GameObject EmptyGizmoPrefab;
    public Transform gizmoTransform;
    public HGizmo gizmo;
    private GameObject slowGizmo;
    public HActionPoint3D slowSphere;
    private HActionPoint3D tmpModel;

    public Gizmo.Axis selectedAxis;
    Vector3 rotation;

    private bool isManipulating = false;

    public HIRobot selectedRobot;
    public HRobotEE selectedEndEffector;

    public Material TransparentMaterial;
    public Material RedMaterial;

    private HInteractiveObject Robot;

    private LineRenderer lineRenderer;
    private GameObject planeObject;

    private Vector3 lastValidPosition;
    private Vector3 previousPosition;
    private Vector3 startPosition;

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
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => UpdatePosition());
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => Manipulation());
    }

    // Update is called once per frame
    private void Update()
    {
        if (isManipulating)
        {
            Vector3 currentPosition = slowSphere.transform.localPosition;

            // move robot model if the manipulation started
            if (Vector3.Distance(currentPosition, previousPosition) > 0.00001f)
            {
                MoveModel(DefaultOrientation, slowGizmo.transform, MoveOption.DirectManipulation);
            }
            previousPosition = currentPosition;

            // update delta values for the "fake" gizmo
            Vector3 vec = slowGizmo.transform.position - startPosition;
            slowGizmo.gameObject.GetComponent<HGizmo>().SetXDelta(TransformConvertor.UnityToROS(vec).x);
            slowGizmo.gameObject.GetComponent<HGizmo>().SetYDelta(TransformConvertor.UnityToROS(vec).y);
            slowGizmo.gameObject.GetComponent<HGizmo>().SetZDelta(TransformConvertor.UnityToROS(vec).z);

            // update delta values for the original gizmo
            gizmo.gameObject.GetComponent<HGizmo>().SetXDelta(TransformConvertor.UnityToROS(vec).x);
            gizmo.gameObject.GetComponent<HGizmo>().SetYDelta(TransformConvertor.UnityToROS(vec).y);
            gizmo.gameObject.GetComponent<HGizmo>().SetZDelta(TransformConvertor.UnityToROS(vec).z);
        }

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

    /**
     * The function is called after the end of the manipulation
     */
    public void UpdatePosition()
    {
        isManipulating = false;
        gizmoTransform.GetComponent<BoundsControl>().enabled = true;
        gizmoTransform.position = lastValidPosition;

        // update position of the confirmation window
        Vector3 vec = gizmoTransform.localPosition + HConfirmationWindow.Instance.ConfirmationMenuVectorUp;
        HConfirmationWindow.Instance.ConfirmationWindow.transform.localPosition = vec;
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(true);

        // update position of the slider menu
        Vector3 sliderVector = gizmoTransform.localPosition + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.localPosition = sliderVector;
        HSliderMenu.Instance.SliderMenu.SetActive(true);

        HAxisMenu.Instance.AxisMenu.SetActive(true);

        // destroy "fake" gizmo
        Destroy(slowSphere.gameObject);

        // show original gizmo
        foreach (Renderer render in tmpModel.transform.GetComponentsInChildren<Renderer>())
        {
            render.enabled = true;
        }
    }

    /**
     * The function is called at the start of the manipulation
     */
    public void Manipulation()
    {
        isManipulating = true;
        
        gizmoTransform.GetComponent<BoundsControl>().enabled = false;

        // instantiate "fake" end effector of the robot
        slowSphere = Instantiate(PointPrefab);
        slowSphere.gameObject.AddComponent<HFollowObject>().SetFollowingObject(gizmoTransform.gameObject);
        slowSphere.transform.SetParent(SceneOrigin);
        slowSphere.transform.position = gizmoTransform.position;
        slowSphere.transform.localScale = Vector3.one / 2;

        // instantiate "fake" gizmo
        slowGizmo = Instantiate(EmptyGizmoPrefab);
        slowGizmo.transform.SetParent(slowSphere.transform);
        slowGizmo.transform.rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        slowGizmo.transform.position = gizmoTransform.position;
        slowGizmo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // disable UI objects
        HSliderMenu.Instance.SliderMenu.SetActive(false);
        HAxisMenu.Instance.AxisMenu.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        // hide original gizmo
        foreach (Renderer render in tmpModel.transform.GetComponentsInChildren<Renderer>())
        {
            render.enabled = false;
        }
    }

    /**
     * The function is called when the confirm button is clicked
     */
    public void ConfirmClicked()
    {
        // move real robot
        MoveRobot();

        // reset values
        startPosition = gizmoTransform.position;
        gizmo.gameObject.GetComponent<HGizmo>().SetXDelta(0);
        gizmo.gameObject.GetComponent<HGizmo>().SetYDelta(0);
        gizmo.gameObject.GetComponent<HGizmo>().SetZDelta(0);
    }

    /**
     * The function is called when the reset button is clicked
     */
    public void ResetClicked()
    {
        // move robot model to the previous position before manipulation
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);

        // reset gizmo position
        gizmoTransform.position = selectedEndEffector.transform.position;

        // update slider menu position
        Vector3 sliderVector = gizmoTransform.position + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.position = sliderVector;
    }

    /**
     * The function enables manipulation for the selected robot 
     */
    public async void ActiveEndEffectorTranform(HInteractiveObject robot)
    {
        // remove previous listeners if any
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

        // check if the robot supports inverse kinematics
        RobotActionObjectH r = (RobotActionObjectH) robot;
        if (!GameManagerH.Instance.RobotsWithInverseKinematics.Contains(r.Data.Type))
        {
            HNotificationWindow.Instance.ShowNotification("Robot does not support IK");
            return;
        }

        ResetVisuals();

        Robot = robot;

        // set default pose for the previously selected robot
        if (selectedRobot != (RobotActionObjectH) robot && selectedRobot != null)
        {
            MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);
        }

        // allows only for the different robot
        if (selectedRobot != (RobotActionObjectH) robot)
        {
            // add listeners for the gizmo transform to allow manipulation
            gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => UpdatePosition());
            gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => Manipulation());
            
            if (tmpModel) {
                Destroy(tmpModel.gameObject);
            }

            // get end effector of the selected robot
            selectedRobot = (RobotActionObjectH) robot;
            List<HRobotEE> ee = await selectedRobot.GetAllEE();
            selectedEndEffector = ee[0];

            startPosition = selectedEndEffector.transform.position;

            // create end effector point for the manipulation
            tmpModel = Instantiate(PointPrefab, selectedEndEffector.transform.position, selectedEndEffector.transform.rotation);

            tmpModel.transform.localScale = Vector3.one / 2;

            gizmoTransform.transform.position = tmpModel.transform.position;
            gizmoTransform.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            tmpModel.transform.SetParent(gizmoTransform);
            tmpModel.setInterarction(gizmoTransform.gameObject);
            tmpModel.EnableOffscreenIndicator(false);
            gizmoTransform.gameObject.SetActive(true);

            // create gizmo model for the manipulation
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

            // add listeners for the each game object of the gizmo (axis movement)
            foreach (ObjectManipulator objectManipulator in o)
            {
                objectManipulator.HostTransform = gizmoTransform;
                objectManipulator.OnManipulationStarted.AddListener((s) => Manipulation());
                objectManipulator.OnManipulationEnded.AddListener((s) => UpdatePosition());
            }
            await PrepareRobotModel(selectedRobot.GetId(), false);
        }

        // move robot model to the position of the real robot
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);

        // update slider position
        Vector3 sliderVector = gizmoTransform.localPosition + HSliderMenu.Instance.SliderMenuVectorUp;
        HSliderMenu.Instance.SliderMenu.transform.localPosition = sliderVector;

        HSliderMenu.Instance.SliderMenu.SetActive(true);
        HAxisMenu.Instance.AxisMenu.SetActive(true);
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        // disable robot colliders
        foreach (Collider collider in robot.transform.GetComponentsInChildren<Collider>()) {
            collider.enabled = false;
        }

        // save robot materials
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>()) {
            materialsBackup.Add(renderer.transform.name, renderer.material);
            renderer.material = TransparentMaterial;
        }

        RemoveVisualsFromActionObjects();
        ShowRangeVisual(robot);
    }

    /**
     * The function disables manipulation process
     */
    public void DeactiveEndEffectorTransform()
    {
        // move robot model to the default position
        MoveModel(DefaultOrientation, selectedEndEffector.transform, MoveOption.None);

        gizmoTransform.gameObject.SetActive(false);
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        if (tmpModel)
            Destroy(tmpModel.gameObject);
        if (gizmo)
            Destroy(gizmo.gameObject);
        if (slowSphere)
            Destroy(slowSphere.gameObject);

        ResetVisuals();

        slowSphere = null;
        tmpModel = null;
        gizmo = null;
        selectedRobot = null;
        Robot = null;

        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

        HSliderMenu.Instance.SliderMenu.SetActive(false);
        HAxisMenu.Instance.AxisMenu.SetActive(false);

        SetVisualsToActionObjects();
    }

    /**
     * Function moves model of the robot to the position of the end effector
     * 
     * \param[in] or      orientation
     * \param[in] t      transform of the object 
     * \param[in] moveOption      selected option for the manipulation
     */
    public async void MoveModel(Orientation or, Transform t, MoveOption moveOption)
    {
        // set position
        Vector3 transformPosition = t.position;
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(transformPosition));
        IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);

        // try calculate inverse kinematics to get model joints
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

            // set model joints to the selected robot
            foreach (IO.Swagger.Model.Joint joint in modelJoints)
            {
                SceneManagerH.Instance.SelectedRobot.SetJointValue(joint.Name, (float) joint.Value);
            }
        }
        catch (RequestFailedException)
        {
            // set red material to the robot
            SetErrorMaterial(Robot, RedMaterial);

            // reset position of the manipulation object depending on the manipulation option
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

    /**
     * Function moves real robot to the position of the end effector
     * 
     */
    private async void MoveRobot()
    {
        try
        {
            string armId = null;
            if (SceneManagerH.Instance.SelectedRobot.MultiArm())
                armId = SceneManagerH.Instance.SelectedArmId;

            Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(tmpModel.transform.position));
            IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);

            await WebSocketManagerH.Instance.MoveToPose(selectedRobot.GetId(), selectedEndEffector.GetName(), 0.5m, position, DefaultOrientation);

        } catch {
            HNotificationWindow.Instance.ShowNotification("Unable to move robot here.");
        }
    }

    /**
     * Function prepares robot for the manipulation
     * 
     * \param[in] robotID      ID of the robot
     * \param[in] shadowRealRobot      value if robot should be shaded
     */
    private async Task PrepareRobotModel(string robotID, bool shadowRealRobot)
    {
        if (SceneManagerH.Instance.SceneStarted)
        {
            await WebSocketManagerH.Instance.RegisterForRobotEvent(robotID, shadowRealRobot, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
            SceneManagerH.Instance.GetRobot(robotID).SetGrey(!shadowRealRobot, true);
        }
    }

    /**
     * Function sets error material to the robot
     * 
     * \param[in] robot      selected robot
     * \param[in] material      what material it will be changed to
     */
    private void SetErrorMaterial(HInteractiveObject robot, Material material)
    {
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
        StartCoroutine(HideErrorMaterial(robot, TransparentMaterial));
    }

    /**
     * Function hides error material of the robot
     * 
     * \param[in] robot      selected robot
     * \param[in] material      what material it will be changed to
     */
    private IEnumerator HideErrorMaterial(HInteractiveObject robot, Material material)
    {
        // wait for 0,5 seconds
        yield return new WaitForSeconds(0.5f);

        // set default material to robot parts
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    /**
     * Function removes colliders and mesh renderers from the START, END, ActionPoints objects
     * 
     */
    private void RemoveVisualsFromActionObjects()
    {
        Transform start = SceneOrigin.Find("START");
        Transform end = SceneOrigin.Find("END");
        Transform actionPoints = SceneOrigin.Find("ActionPoints");

        if (start) {
            foreach (Collider collider in start.GetComponentsInChildren<Collider>()) {
                collider.enabled = false;
            }
            foreach (Transform t in start.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        if (end) {
            foreach (Collider collider in end.GetComponentsInChildren<Collider>()) {
                collider.enabled = false;
            }
            foreach (Transform t in end.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        if (actionPoints) {
            foreach (Collider collider in actionPoints.GetComponentsInChildren<Collider>()) {
                collider.enabled = false;
            }
            foreach (Transform t in actionPoints.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    /**
     * Function adds colliders and mesh renderers to the START, END, ActionPoints objects
     * 
     */
    private void SetVisualsToActionObjects()
    {
        Transform start = SceneOrigin.Find("START");
        Transform end = SceneOrigin.Find("END");
        Transform actionPoints = SceneOrigin.Find("ActionPoints");

        if (start) {
            foreach (Collider collider in start.GetComponentsInChildren<Collider>()) {
                collider.enabled = true;
            }
            foreach (Transform t in start.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        if (end) {
            foreach (Collider collider in end.GetComponentsInChildren<Collider>()) {
                collider.enabled = true;
            }
            foreach (Transform t in end.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        if (actionPoints) {
            foreach (Collider collider in actionPoints.GetComponentsInChildren<Collider>()) {
                collider.enabled = true;
            }
            foreach (Transform t in actionPoints.GetComponentsInChildren<Transform>()) {
                if (t.name == "Cube")
                    t.GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }

    /**
     * Function gets points for the range object around the robot
     * 
     * \param[in] robot      selected robot
     */
    public async Task GetRangeVisual(HInteractiveObject robot)
    {
        float radius = 0.0f;
        int segments = 45;
        int angleStep = 360 / segments;
        float step = 0.05f;

        rotation = robot.transform.localRotation.eulerAngles;
        Quaternion previousRotation = robot.transform.rotation;
        Quaternion previousLocalRotation = robot.transform.localRotation;
        robot.transform.rotation = Quaternion.identity;
        robot.transform.localRotation = Quaternion.identity;

        HIRobot HIRobot = (RobotActionObjectH) robot;

        // get default end effector
        List<HRobotEE> ee = await HIRobot.GetAllEE();
        HRobotEE effector = ee[0];

        OrderedDictionary<int, float> degreesValues = new OrderedDictionary<int, float>();

        // iterate 
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * i * angleStep;
            bool isValid = true;
            float currentStep = step;

            Vector3 lastPostition = new Vector3(robot.transform.position.x, SceneOrigin.position.y, robot.transform.position.z);

            while (isValid)
            {
                float x = Mathf.Sin(angle) * (radius + currentStep);
                float z = Mathf.Cos(angle) * (radius + currentStep);
                Vector3 currentPosition = new Vector3(robot.transform.position.x + x, SceneOrigin.position.y, robot.transform.position.z + z);

                // try to get point in the current angle with added step
                try {
                    Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(currentPosition));
                    IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);
                    IO.Swagger.Model.Pose pose = new(orientation: DefaultOrientation, position: position);
                    List<IO.Swagger.Model.Joint> startJoints = new();
                    SceneManagerH.Instance.SelectedRobot = SceneManagerH.Instance.GetRobot(HIRobot.GetId());
                    List<IO.Swagger.Model.Joint> modelJoints = await WebSocketManagerH.Instance.InverseKinematics(
                        robot.GetId(),
                        effector.GetName(),
                        true,
                        pose,
                        startJoints);

                    lastPostition = currentPosition;
                }
                catch (RequestFailedException)
                {
                    // set last valid position as start position
                    float startStep = currentStep - step;
                    Vector3 finalPosition = lastPostition;


                    // calculate with a smaller step
                    while (startStep < currentStep)
                    {
                        float x1 = Mathf.Sin(angle) * (radius + startStep);
                        float z1 = Mathf.Cos(angle) * (radius + startStep);
                        Vector3 currentPosition1 = new Vector3(robot.transform.position.x + x1, SceneOrigin.position.y, robot.transform.position.z + z1);

                        // try to get point in the current angle with added step
                        try {
                            Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(currentPosition1));
                            IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);
                            IO.Swagger.Model.Pose pose = new(orientation: DefaultOrientation, position: position);
                            List<IO.Swagger.Model.Joint> startJoints = new();
                            SceneManagerH.Instance.SelectedRobot = SceneManagerH.Instance.GetRobot(HIRobot.GetId());
                            List<IO.Swagger.Model.Joint> modelJoints = await WebSocketManagerH.Instance.InverseKinematics(
                                HIRobot.GetId(),
                                effector.GetName(),
                                true,
                                pose,
                                startJoints);

                            finalPosition = currentPosition1;
                        }
                        catch (RequestFailedException)
                        {
                            // last valid point in the current angle was found
                            degreesValues.Add(i * angleStep, Vector3.Distance(new Vector3(robot.transform.position.x, SceneOrigin.position.y, robot.transform.position.z), finalPosition));
                            isValid = false;
                            break;
                        }
                        startStep += 0.005f;
                    }
                }
                currentStep += step;
            }
        }
        RobotActionObjectH r = (RobotActionObjectH) robot;

        // save last valid points around robot to the storage
        RobotRangeStorage.Instance.InsertRobotRanges(r.Data.Type, degreesValues);

        robot.transform.rotation = previousRotation;
        robot.transform.localRotation = previousLocalRotation;
    }

    /**
     * Function shows range object
     * 
     * \param[in] robot      selected robot
     */
    private void ShowRangeVisual(HInteractiveObject robot)
    {
        RobotActionObjectH r = (RobotActionObjectH) robot;

        // check if the range values are available
        if (!RobotRangeStorage.Instance.RobotsRange.ContainsKey(r.Data.Type)) {
            return;
        }

        OrderedDictionary<int, float> valuesDictionary = RobotRangeStorage.Instance.RobotsRange[r.Data.Type];

        // create line renderer object
        GameObject newObject = new GameObject("LineRenderer");
        lineRenderer = newObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.numCornerVertices = 10;
        lineRenderer.material = RedMaterial;
        lineRenderer.positionCount = valuesDictionary.Count;
        lineRenderer.transform.position = new Vector3(robot.transform.position.x, SceneOrigin.position.y, robot.transform.position.z);
        lineRenderer.transform.localRotation = SceneOrigin.localRotation;

        // setting positions to a linerenderer
        int linerendererPosition = 0;
        float need = rotation.y - robot.transform.localRotation.eulerAngles.y;

        // connect all available points for the line renderer
        foreach (KeyValuePair<int, float> pair in valuesDictionary)
        {
            float angle = Mathf.Deg2Rad * (pair.Key - need);
            float x = Mathf.Sin(angle) * pair.Value;
            float z = Mathf.Cos(angle) * pair.Value;
            Vector3 position = new Vector3(robot.transform.position.x + x, SceneOrigin.position.y, robot.transform.position.z + z);
            lineRenderer.SetPosition(linerendererPosition, position);
            linerendererPosition++;
        }

        // insert artificial point (center of the robot)
        Vector3[] vectors = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(vectors);
        List<Vector3> linePoints = vectors.ToList();
        linePoints.Insert(0, new Vector3(robot.transform.position.x, SceneOrigin.position.y, robot.transform.position.z));

        // Mesh
        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        Vector3[] vertices = linePoints.ToArray();

        
        int[] triangles = CreateTriangles(vertices);
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        GameObject pivot = new GameObject("pivot");
        pivot.transform.position = linePoints.First();

        // create plane object
        planeObject = new GameObject("PlaneObject");
        planeObject.AddComponent<MeshFilter>().mesh = mesh;
        planeObject.AddComponent<MeshRenderer>().material = TransparentMaterial;
        planeObject.transform.SetParent(pivot.transform);
    }

    /**
     * Function creates a triangular mesh from the given positions in 3D space
     * 
     * \param[in] positions      positions of the points
     * 
     * \return      array of indices that specify how the vertices should be connected
     */
    private int[] CreateTriangles(Vector3[] positions)
    {
        int[] triangles = new int[(positions.Length - 2) * 3 + 3]; // +3 for last triangle

        for (int i = 0; i < positions.Length - 2; i++)
        {
            triangles[i * 3] = 0; // center
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        // last triangle
        triangles[(positions.Length - 2) * 3] = 0; // center
        triangles[(positions.Length - 2) * 3 + 1] = positions.Length - 1;
        triangles[(positions.Length - 2) * 3 + 2] = 1;

        return triangles;
    }

    /**
     * Function resets visuals of the robot at the end of the manipulation process
     * 
     */
    private void ResetVisuals()
    {
        // set materials for the previously selected robot
        if (materialsBackup.Count > 0)
        {
            foreach (Renderer renderer in Robot.GetComponentsInChildren<Renderer>())
            {
                renderer.material = materialsBackup[renderer.transform.name];
            }
            materialsBackup.Clear();
        }

        // enable colliders for the previously selected robot
        if (Robot)
        {
            foreach (Collider collider in Robot.transform.GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
        }

        if (lineRenderer)
            Destroy(lineRenderer.gameObject);
        if (planeObject)
            Destroy(planeObject.gameObject);
    }
}
