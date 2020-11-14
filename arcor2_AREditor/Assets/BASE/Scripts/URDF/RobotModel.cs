using System.Collections;
using System.Collections.Generic;
using RosSharp;
using RosSharp.RosBridgeClient;
using RosSharp.Urdf;
using UnityEngine;

public class RobotModel {

    public string RobotType { get; private set; }
    public GameObject RobotModelGameObject { get; private set; }

    public bool IsBeingUsed { get; set; }

    public Dictionary<string, RobotLink> Links = new Dictionary<string, RobotLink>();
    public Dictionary<string, string> Joints = new Dictionary<string, string>();

    public bool RobotLoaded { get; set; }

    public RobotModel(string robotType, GameObject robotModel) {
        RobotType = robotType;
        RobotModelGameObject = robotModel;
        IsBeingUsed = false;
    }

    /// <summary>
    /// Initializes RobotLinks and sets a boolean to its Visuals dictionary,
    /// telling whether the model of individual visual was already imported (is type of box, cylinder, capsule)
    /// or not yet (is mesh - is going to be continually imported from UrdfAssetImporterRuntime).
    /// </summary>
    /// <param name="copyOfRobotModel">If set to true, robotModel is being copied from another robotModel, it assumed, that Visuals of every Link are already imported.</param>
    public void LoadLinks(bool copyOfRobotModel = false) {
        // Get all UrdfLink components in builded Robot
        foreach (UrdfLink link in RobotModelGameObject.GetComponentsInChildren<UrdfLink>()) {

            // Get all UrdfVisuals of each UrdfLink
            GameObject visualsGameObject = link.gameObject.GetComponentInChildren<UrdfVisuals>().gameObject;
            Dictionary<UrdfVisual, bool> visuals = new Dictionary<UrdfVisual, bool>();
            // Traverse each UrdfVisual and set a boolean indicating whether its visual is already loaded (is of some basic type - box, cylinder, capsule)
            // or is going to be loaded by ColladaImporter (in case its type of mesh)
            foreach (UrdfVisual visual in visualsGameObject.GetComponentsInChildren<UrdfVisual>()) {
                visuals.Add(visual, copyOfRobotModel ? true : (visual.GeometryType == GeometryTypes.Mesh ? false : true));
                // hide visual if it is mesh.. mesh will be displayed when fully loaded
                visual.gameObject.SetActive(copyOfRobotModel ? true : (visual.GeometryType == GeometryTypes.Mesh ? false : true));
            }

            UrdfJoint urdfJoint = link.GetComponent<UrdfJoint>();
            JointStateWriter jointWriter = null;
            if (urdfJoint != null) {
                if (urdfJoint.JointType != UrdfJoint.JointTypes.Fixed) {
                    jointWriter = urdfJoint.transform.AddComponentIfNotExists<JointStateWriter>();
                    Joints.Add(urdfJoint.JointName, link.gameObject.name);
                }
            }
            Links.Add(link.gameObject.name, new RobotLink(link.gameObject.name, urdfJoint, jointWriter, visuals_gameObject:visuals, is_base_link: link.IsBaseLink));
        }
    }

    /// <summary>
    /// Sets visual of a given link when loaded.
    /// </summary>
    /// <param name="linkName"></param>
    /// <param name="urdfVisual"></param>
    public void SetLinkVisualLoaded(string linkName, UrdfVisual urdfVisual) {
        Links.TryGetValue(linkName, out RobotLink link);
        link?.SetVisualLoaded(urdfVisual);

        IsRobotLoaded();

        // if robot is loaded, show its visuals, otherwise hide them
        //link?.SetActiveVisuals(RobotLoaded);
    }

    /// <summary>
    /// Sets collision of a given link when loaded.
    /// </summary>
    /// <param name="linkName"></param>
    /// <param name="urdfVisual"></param>
    public void SetLinkCollisionLoaded(string linkName, UrdfCollision urdfCollision) {
        Links.TryGetValue(linkName, out RobotLink link);
        link?.SetCollisionLoaded(urdfCollision);

        IsRobotLoaded();
    }

    /// <summary>
    /// Checks that all visuals (meshes, primitive types - box, cylinder..) of the robot are imported and created.
    /// </summary>
    /// <returns></returns>
    private bool IsRobotLoaded() {
        if (!RobotLoaded) {
            foreach (RobotLink link in Links.Values) {
                if (!link.HasVisualsLoaded()) {
                    return false;
                }
            }
        }
        RobotLoaded = true;
        OnRobotLoaded();

        return true;
    }

    private void OnRobotLoaded() {
        //Debug.Log("URDF: robot is fully loaded");

        SetActiveAllVisuals(true);

        AddOnClickScriptToColliders(RobotModelGameObject);

        UrdfManager.Instance.RobotModelLoaded(RobotType);
    }

    private void AddOnClickScriptToColliders(GameObject gameObject) {
        Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders) {
            // Add OnClick functionality
            collider.gameObject.AddComponent<OnClickCollider>();
        }
    }


    /// <summary>
    /// Displays or hides all visuals of the robot.
    /// </summary>
    /// <param name="active"></param>
    public void SetActiveAllVisuals(bool active) {
        foreach (RobotLink link in Links.Values) {
            link.SetActiveVisuals(active);
        }
    }

    public void SetRandomJointAngles() {
        foreach (RobotLink link in Links.Values) {
            link.SetJointAngle(Random.Range(-6.28f, 6.28f));
        }
    }

    /// <summary>
    /// Sets angle of the joint. Uses radians by default, if degrees are passed in, angle_in_degrees needs to be set to true.
    /// </summary>
    /// <param name="jointName">Name of the joint.</param>
    /// <param name="angle">Angle in radians (by default) or degrees.</param>
    /// <param name="angle_in_degrees">Needs to be true, if angle is set in degrees.</param>
    public void SetJointAngle(string jointName, float angle, bool angle_in_degrees = false) {
        if (RobotLoaded) {
            Joints.TryGetValue(jointName, out string linkName);
            Links.TryGetValue(linkName, out RobotLink link);
            if (angle_in_degrees) {
                angle *= Mathf.Deg2Rad;
            }
            link?.SetJointAngle(angle);
        }
    }

    public List<IO.Swagger.Model.Joint> GetJoints() {
        List<IO.Swagger.Model.Joint> joints = new List<IO.Swagger.Model.Joint>();
        foreach (KeyValuePair<string, string> joint in Joints) {
            Links.TryGetValue(joint.Value, out RobotLink link);
            if (link != null) {
                joints.Add(new IO.Swagger.Model.Joint(link.LinkName, link.GetJointAngle()));
            }
        }
        return joints;
    }
}
