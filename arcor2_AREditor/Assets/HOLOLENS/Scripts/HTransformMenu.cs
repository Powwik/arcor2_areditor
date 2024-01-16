using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;
using Hololens;
using RosSharp.Urdf;
using UnityEngine.Animations;



public class HTransformMenu : Singleton<HTransformMenu> {

    HInteractiveObject InteractiveObject;
    GameObject model;

    GameObject transformDetail;

    public Transform GizmoTransform;

     /// <summary>
    /// Prefab for transform gizmo
    /// </summary>
    public GameObject GizmoPrefab;

    private HGizmo gizmo;

    private bool manipulationStarted = false;

    private bool axisManipulationActive = false;


    void Start() {
        GizmoTransform.gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => updatePosition());
        GizmoTransform.gameObject.GetComponent<BoundsControl>().ScaleStopped.AddListener(() => updateScale());
        GizmoTransform.gameObject.GetComponent<BoundsControl>().RotateStopped.AddListener(() => updatePosition());
    }

    void Update(){
        if(/*axisManipulationActive && */manipulationStarted){
            Vector3 vec =  GizmoTransform.position - InteractiveObject.transform.position;
       /*     GizmoPrefab.GetComponent<HGizmo>().SetXDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).y));
             GizmoPrefab.GetComponent<HGizmo>().SetYDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).x));
             GizmoPrefab.GetComponent<HGizmo>().SetZDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).z));*/
            gizmo.SetXDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).y));
            gizmo.SetYDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).x));
            gizmo.SetZDelta(Mathf.Abs(TransformConvertor.UnityToROS(vec).z));

        }
    }


    public async Task updateScale() {
        try {
            IO.Swagger.Model.ObjectModel objectModel = ((ActionObjectH) InteractiveObject).ActionObjectMetadata.ObjectModel;
            Vector3 transformedScale = TransformConvertor.UnityToROSScale(model.transform.lossyScale);

            switch (objectModel.Type) {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                    objectModel.Box.SizeX = (decimal) transformedScale.x;
                    objectModel.Box.SizeY = (decimal) transformedScale.y;
                    objectModel.Box.SizeZ = (decimal) transformedScale.z;
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    objectModel.Cylinder.Radius = (decimal) transformedScale.x;
                    objectModel.Cylinder.Height = (decimal) transformedScale.z;
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    objectModel.Sphere.Radius = (decimal) transformedScale.x;
                    break;
            }
            await WebSocketManagerH.Instance.UpdateObjectModel(((ActionObjectH) InteractiveObject).ActionObjectMetadata.Type, objectModel);
        } catch (RequestFailedException e) {
            Debug.Log("Failed to update size of collision object   " +  e.Message);
        }
    }

    public async Task updatePosition() {
        manipulationStarted = false;
        if(InteractiveObject is ActionObjectH actionObject){
            await WebSocketManagerH.Instance.UpdateActionObjectPose(InteractiveObject.GetId(), new IO.Swagger.Model.Pose(position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(model.transform.position) /*InteractiveObject.transform.localPosition + model.transform.localPosition*/)),
                                                                                                                                orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(Quaternion.Inverse(GameManagerH.Instance.Scene.transform.rotation) * model.transform.rotation   /*InteractiveObject.transform.localRotation * model.transform.localRotation*/))));
        }
        else if(InteractiveObject is HActionPoint3D actionPoint){
            await WebSocketManagerH.Instance.UpdateActionPointPosition(InteractiveObject.GetId(), DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(InteractiveObject.transform.parent.InverseTransformPoint(model.transform.position))));

        }else if (InteractiveObject is HStartEndAction startEndAction) {
            startEndAction.transform.position = model.transform.position;
            startEndAction.SavePosition();
        }
  
    }

    public void deactiveTransform(){
      
        GizmoTransform.gameObject.SetActive(false);

        if (InteractiveObject != null) {
           InteractiveObject.EnableOffscreenIndicator(true);
           InteractiveObject.EnableVisual(true);
        }

        InteractiveObject = null;
        Destroy(model);
       // GizmoPrefab.SetActive(false);
        model = null;

    }

    public bool isDeactivated(){
        return InteractiveObject == null && model == null;
    }

    public void activeTransform(HInteractiveObject interactiveObject)
    {
        InteractiveObject = interactiveObject;
      
        GizmoTransform.transform.position = InteractiveObject.transform.position;
        GizmoTransform.transform.rotation = InteractiveObject.transform.rotation;
        GizmoTransform.transform.localScale = new Vector3(1f,1f,1f);
       
        if (interactiveObject is HActionPoint3D actionPoint)
        {
            model = actionPoint.GetModelCopy();
            model.transform.SetParent(GizmoTransform);
            model.transform.rotation = InteractiveObject.transform.rotation;
            model.transform.position = InteractiveObject.transform.position;
            GizmoTransform.gameObject.SetActive(true);
            actionPoint.setInterarction(GizmoTransform.gameObject);
            actionPoint.EnableOffscreenIndicator(false);
            actionPoint.EnableVisual(false);

        }
        else if(interactiveObject is RobotActionObjectH robot)
        {
            model = robot.GetModelCopy();
            model.transform.SetParent(p: GizmoTransform);
            model.transform.rotation = InteractiveObject.transform.rotation;
            model.transform.position = InteractiveObject.transform.position;
            GizmoTransform.gameObject.SetActive(true);
            robot.setInterarction(GizmoTransform.gameObject);
            robot.EnableOffscreenIndicator(false);
            robot.EnableVisual(false);

        }
        else if ( interactiveObject is ActionObject3DH actionObject)
        {
            model = actionObject.GetModelCopy();
            model.transform.SetParent(GizmoTransform);
            model.transform.rotation = InteractiveObject.transform.rotation;
            model.transform.position = InteractiveObject.transform.position;
            GizmoTransform.gameObject.SetActive(true);
            actionObject.setInterarction(GizmoTransform.gameObject);
            actionObject.EnableOffscreenIndicator(false);
            actionObject.EnableVisual(false);

        }
        else if (interactiveObject is HStartEndAction action)
        {
            model = action.GetModelCopy();
            model.transform.SetParent(GizmoTransform);
            model.transform.rotation = interactiveObject.transform.rotation;
            model.transform.position = interactiveObject.transform.position;
            GizmoTransform.gameObject.SetActive(true);
            action.setInterarction(GizmoTransform.gameObject);
            action.EnableOffscreenIndicator(false);
            action.EnableVisual(false);
        }
        else if( interactiveObject is HAction3D action3D)
        {
            HActionPoint3D actionPoint1 = (HActionPoint3D) action3D.ActionPoint;
            InteractiveObject = actionPoint1;
            
            model = actionPoint1.GetModelCopy();
            model.transform.SetParent(GizmoTransform);
            GizmoTransform.transform.position = actionPoint1.transform.position;
            GizmoTransform.transform.rotation = actionPoint1.transform.rotation;
            model.transform.rotation = actionPoint1.transform.rotation;
            model.transform.position = actionPoint1.transform.position;
            GizmoTransform.gameObject.SetActive(true);
            actionPoint1.setInterarction(GizmoTransform.gameObject);
            actionPoint1.EnableOffscreenIndicator(false);
            actionPoint1.EnableVisual(false);
        }
        
        gizmo = Instantiate(GizmoPrefab).GetComponent<HGizmo>();

        gizmo.transform.SetParent(model.transform);

        NormalizeGizmoScale();
        // 0.1 is default scale for our gizmo
      
       // GizmoPrefab.SetActive(true);
    //    GizmoPrefab.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
      //  GizmoPrefab.transform.position =/* model.transform.position; */ Vector3.zero;
        gizmo.transform.localPosition =/* model.transform.position;*/  Vector3.zero;
        gizmo.transform.eulerAngles = model.transform.eulerAngles;
      //  gizmo.transform.eulerAngles = new Vector3
    /*    GizmoPrefab.GetComponent<HGizmo>().SetXDelta(0);
        GizmoPrefab.GetComponent<HGizmo>().SetYDelta(0);
        GizmoPrefab.GetComponent<HGizmo>().SetZDelta(0);*/

        gizmo.SetXDelta(0);
        gizmo.SetYDelta(0);
        gizmo.SetZDelta(0);
        ConstraintSource source = new ConstraintSource();
        source.sourceTransform = GizmoTransform;

        GizmoTransform.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => manipulationStarted = true);
        //   GizmoTransform.GetComponent<BoxCollider>().
        ObjectManipulator[] o =   gizmo.gameObject.GetComponentsInChildren<ObjectManipulator>();
        gizmo.gameObject.GetComponent<ScaleConstraint>().AddSource(source);
        Vector3 boundsObject = GizmoTransform.GetComponent<BoxCollider>().bounds.max;
        foreach(ObjectManipulator objectManipulator in o){
            //Debug.Log(objectManipulator.gameObject.GetComponentInChildren<MeshRenderer>().bounds.max);
      /*      if(objectManipulator.gameObject.name  == "x_axis"){
                while(objectManipulator.gameObject.GetComponentInChildren<MeshRenderer>().bounds.max.x - boundsObject.x < 0.2f ){
                    Vector3 vecX =  objectManipulator.gameObject.transform.localScale;
                    objectManipulator.gameObject.transform.localScale = new Vector3(vecX.x, vecX.y, vecX.z + 0.1f) ;
                }
            }
            else if(objectManipulator.gameObject.name  == "y_axis"){
                while(objectManipulator.gameObject.GetComponentInChildren<MeshRenderer>().bounds.max.z - boundsObject.z < 0.2f ){
                    Vector3 vecX =  objectManipulator.gameObject.transform.localScale;
                    objectManipulator.gameObject.transform.localScale = new Vector3(vecX.x, vecX.y + 0.1f, vecX.z) ;
                }

            }else if(objectManipulator.gameObject.name  == "z_axis"){
                while(objectManipulator.gameObject.GetComponentInChildren<MeshRenderer>().bounds.max.y - boundsObject.y < 0.2f ){
                    Vector3 vecX =  objectManipulator.gameObject.transform.localScale;
                    objectManipulator.gameObject.transform.localScale = new Vector3(vecX.x, vecX.y + 0.1f, vecX.z) ;
                }
            }*/
          //  objectManipulator.gameObject.GetComponent<ScaleConstraint>().AddSource(source);
            objectManipulator.HostTransform = /* GizmoAxisTransform; */ GizmoTransform;
            objectManipulator.OnManipulationStarted.AddListener((s) => manipulationStarted = true);
            objectManipulator.OnManipulationEnded.AddListener((s) => updatePosition());

        }
       // gizmo.gameObject.SetActive(false);

        /*    BoxCollider collider = model.AddComponent<BoxCollider>();
            collider.size = ((RobotActionObjectH)  InteractiveObject).getInteractObject().transform.localScale;
            collider.center = ((RobotActionObjectH)  InteractiveObject).getInteractObject().transform.position;
            BoundsControl boundsControl = model.AddComponent<BoundsControl>();

            ObjectManipulator objectManipulator = model.AddComponent<ObjectManipulator>();*/

        /* Target target = model.AddComponent<Target>();
         target.SetTarget(Color.yellow, false, true, false);
         target.enabled = true;*/
    }

    private void NormalizeGizmoScale()
    {
        gizmo.transform.localScale = new Vector3(0.1f / model.transform.localScale.x, 0.1f / model.transform.localScale.y, 0.1f / model.transform.localScale.z);
    }


}
