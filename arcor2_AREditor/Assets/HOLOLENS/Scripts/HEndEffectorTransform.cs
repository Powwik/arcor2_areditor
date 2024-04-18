using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Base;
using Hololens;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using Newtonsoft.Json;
using TriLibCore.General;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

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

            if (Vector3.Distance(currentPosition, previousPosition) > 0.00001f)
            {
                MoveModel(DefaultOrientation, slowGizmo.transform, MoveOption.DirectManipulation);
            }
            previousPosition = currentPosition;

            Vector3 vec = slowGizmo.transform.position - startPosition;
            slowGizmo.gameObject.GetComponent<HGizmo>().SetXDelta(TransformConvertor.UnityToROS(vec).x);
            slowGizmo.gameObject.GetComponent<HGizmo>().SetYDelta(TransformConvertor.UnityToROS(vec).y);
            slowGizmo.gameObject.GetComponent<HGizmo>().SetZDelta(TransformConvertor.UnityToROS(vec).z);

            gizmo.gameObject.GetComponent<HGizmo>().SetXDelta(TransformConvertor.UnityToROS(vec).x);
            gizmo.gameObject.GetComponent<HGizmo>().SetYDelta(TransformConvertor.UnityToROS(vec).y);
            gizmo.gameObject.GetComponent<HGizmo>().SetZDelta(TransformConvertor.UnityToROS(vec).z);
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
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(true);

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
        HConfirmationWindow.Instance.ConfirmationWindow.SetActive(false);

        foreach (Renderer render in tmpModel.transform.GetComponentsInChildren<Renderer>())
        {
            render.enabled = false;
        }
    }

    public void ConfirmClicked()
    {
        MoveRobot();
        startPosition = gizmoTransform.position;
        gizmo.gameObject.GetComponent<HGizmo>().SetXDelta(0);
        gizmo.gameObject.GetComponent<HGizmo>().SetYDelta(0);
        gizmo.gameObject.GetComponent<HGizmo>().SetZDelta(0);
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
        gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.RemoveAllListeners();
        gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.RemoveAllListeners();

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

        if (selectedRobot != (RobotActionObjectH) robot)
        {
            gizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => UpdatePosition());
            gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => Manipulation());
            
            if (tmpModel) {
                Destroy(tmpModel.gameObject);
            }

            selectedRobot = (RobotActionObjectH) robot;
            List<HRobotEE> ee = await selectedRobot.GetAllEE();
            selectedEndEffector = ee[0];

            startPosition = selectedEndEffector.transform.position;

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

    public async void MoveModel(Orientation or, Transform t, MoveOption moveOption)
    {
        Vector3 transformPosition = t.position;
        Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(transformPosition));
        IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);

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
            IO.Swagger.Model.Position position = DataHelper.Vector3ToPosition(point);

            await WebSocketManagerH.Instance.MoveToPose(selectedRobot.GetId(), selectedEndEffector.GetName(), 0.5m, position, DefaultOrientation);

        } catch {
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
        yield return new WaitForSeconds(0.5f);
        foreach (Renderer renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

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

    public async Task GetRangeVisual(HInteractiveObject robot)
    {
        Quaternion previousRotation = robot.transform.localRotation;
        robot.transform.localRotation = SceneOrigin.rotation;

        float radius = 0.0f;
        int segments = 45;
        int angleStep = 360 / segments;
        float step = 0.05f;

        HIRobot HIRobot = (RobotActionObjectH) robot;

        List<HRobotEE> ee = await HIRobot.GetAllEE();
        HRobotEE effector = ee[0];

        OrderedDictionary<int, float> degreesValues = new OrderedDictionary<int, float>();

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
                    // dokroceni
                    float startStep = currentStep - step;
                    Vector3 finalPosition = lastPostition;
                    while (startStep < currentStep)
                    {
                        float x1 = Mathf.Sin(angle) * (radius + startStep);
                        float z1 = Mathf.Cos(angle) * (radius + startStep);
                        Vector3 currentPosition1 = new Vector3(robot.transform.position.x + x1, SceneOrigin.position.y, robot.transform.position.z + z1);

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
                            degreesValues.Add(i * angleStep, Vector3.Distance(new Vector3(robot.transform.position.x, SceneOrigin.position.y, robot.transform.position.z), finalPosition));
                            isValid = false;
                            break;
                        }
                        startStep += 0.01f;
                    }
                }
                currentStep += step;
            }
        }
        RobotActionObjectH r = (RobotActionObjectH) robot;
        RobotRangeStorage.Instance.InsertRobotRanges(r.Data.Type, degreesValues);

        robot.transform.localRotation = previousRotation;
    }

    private void ShowRangeVisual(HInteractiveObject robot)
    {
        RobotActionObjectH r = (RobotActionObjectH) robot;
        if (!RobotRangeStorage.Instance.RobotsRange.ContainsKey(r.Data.Type)) {
            return;
        }
        OrderedDictionary<int, float> valuesDictionary = RobotRangeStorage.Instance.RobotsRange[r.Data.Type];
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
        foreach (KeyValuePair<int, float> pair in valuesDictionary)
        {
            float angle = Mathf.Deg2Rad * pair.Key;
            float x = Mathf.Sin(angle) * pair.Value;
            float z = Mathf.Cos(angle) * pair.Value;
            Vector3 position = new Vector3(robot.transform.position.x + x, SceneOrigin.position.y, robot.transform.position.z + z);

            lineRenderer.SetPosition(linerendererPosition, position);
            linerendererPosition++;
        }


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

        // Plane Object
        planeObject = new GameObject("PlaneObject");
        MeshFilter meshFilter = planeObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        planeObject.AddComponent<MeshRenderer>().material = TransparentMaterial;
    }

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
