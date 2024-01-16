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
    public HActionPoint3D point;
    public GameObject gizmoPrefab;
    public Transform newTransform;
    private HGizmo gizmo;
    HActionPoint3D tmpModel;

    private bool isPressed;
    private bool isManipulating = false;

    // Start is called before the first frame update
    void Start()
    {
        isPressed = false;
        newTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => updatePosition());
        newTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => isManipulating = true);
    }

    // Update is called once per frame
    void Update()
    {
        if (isManipulating)
        {
            Debug.Log("MANIPULATING");
        }
    }


    public void updatePosition()
    {
        Debug.Log("Manipulation ENDED!!!");
        isManipulating = false;
    }

    public async void activeEndEffectorTranform(HInteractiveObject robot)
    {
        if (!isPressed)
        {
            isPressed = false;
            Dictionary<string, List<HRobotEE>> end = robot.GetComponent<RobotActionObjectH>().EndEffectors;
            HRobotEE parent = null;
        
            foreach (var kvp in end["default"]) {
                parent = kvp;
            }

            // create new action point for manipulation
            tmpModel = Instantiate(point, parent.transform.position, parent.transform.rotation);

            newTransform.transform.position = tmpModel.transform.position;
            newTransform.transform.localScale = new Vector3(1f, 1f, 1f);

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
                objectManipulator.OnManipulationStarted.AddListener((s) => isManipulating = true);
                objectManipulator.OnManipulationEnded.AddListener((s) => updatePosition());
            }
        }
    }

    public void deactiveEndEffectorTransform()
    {
        Destroy(tmpModel);
        tmpModel = null;
        Destroy(gizmo);
        gizmo = null;
        newTransform.gameObject.SetActive(false);
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
