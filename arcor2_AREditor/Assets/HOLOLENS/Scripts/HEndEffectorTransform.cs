using System.Collections;
using System.Collections.Generic;
using Base;
using Hololens;
using IO.Swagger.Model;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Animations;

public class HEndEffectorTransform : Singleton<HEndEffectorTransform>
{
    HInteractiveObject InteractiveObject;
    public HActionPoint3D point;
    public GameObject gizmoPrefab;
    public Transform gizmoTransform;
    private HGizmo gizmo;
    HActionPoint3D tmpModel;

    private bool isPressed;

    // Start is called before the first frame update
    void Start()
    {
        isPressed = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async void activeEndEffectorTranform(HInteractiveObject robot)
    {
        //HInteractiveObject copyOfRobot = Instantiate(robot);
        //robot.gameObject.SetActive(false);
        if (!isPressed)
        {
            isPressed = false;
            Dictionary<string, List<HRobotEE>> end = robot.GetComponent<RobotActionObjectH>().EndEffectors;
            HRobotEE parent = null;
        
            foreach (var kvp in end["default"]) {
                parent = kvp;
            }

            tmpModel = Instantiate(point, parent.transform.position, parent.transform.rotation);

            gizmoTransform.transform.position = tmpModel.transform.position;
            gizmoTransform.transform.rotation = tmpModel.transform.rotation;
            gizmoTransform.transform.localScale = new Vector3(1f, 1f, 1f);

            tmpModel.transform.SetParent(gizmoTransform);
            tmpModel.transform.rotation = tmpModel.transform.rotation;
            tmpModel.transform.position = tmpModel.transform.position;
            gizmoTransform.gameObject.SetActive(true);
            tmpModel.setInterarction(gizmoTransform.gameObject);
            tmpModel.EnableOffscreenIndicator(false);

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

            //gizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => manipulationStarted = true);
        }
    }

    public async void MoveModel()
    {
        List<IO.Swagger.Model.Joint> modelJoints; //joints to move the model to
        //string robotId;

        try {
            //IO.Swagger.Model.Pose pose = new IO.Swagger.Model.Pose(orientation: orientation.Orientation, position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(CurrentActionPoint.transform.position)));
            List<IO.Swagger.Model.Joint> startJoints = SceneManager.Instance.SelectedRobot.GetJoints();
            Debug.Log("START JOINTS = " + startJoints);
            //modelJoints = await WebsocketManager.Instance.InverseKinematics(SceneManager.Instance.SelectedRobot.GetId(), SceneManager.Instance.SelectedEndEffector.GetName(), true, pose, startJoints);
            //await PrepareRobotModel(SceneManager.Instance.SelectedRobot.GetId(), false);
            //if (!avoid_collision) {
            //    Notifications.Instance.ShowNotification("The model is in a collision with other object!", "");
            //}
        } catch (ItemNotFoundException ex) {
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        } catch (RequestFailedException ex) {
            //if (avoid_collision) //if this is first call, try it again without avoiding collisions
            //    MoveHereModel(false);
            //else
            Notifications.Instance.ShowNotification("Unable to move here model", ex.Message);
            return;
        }
    }
}
