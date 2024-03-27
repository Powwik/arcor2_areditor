using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Hololens;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI;

public class HActionObjectPickerMenu : Singleton<HActionObjectPickerMenu>
{

    public GameObject models;
    public GameObject collisonObjects;
    public GameObject objectCubePrefab;

    private int loadedModels;
    private int allModels;
   public bool Loaded = false;

    public Dictionary<string, GameObject> objectsModels = new Dictionary<string, GameObject>();


    
    public enum CollisionObjectType {
        Cube,
        Sphere,
        Cylinder,
        Plane
    }
    // Start is called before the first frame update
    void Start()
    {
       loadedModels = 0;
        allModels = 0;

        ActionsManagerH.Instance.OnActionsLoaded += LoadModels;

       
    }

    void Update(){
        if (!Loaded && allModels != 0 && allModels == loadedModels){
            Loaded = true;
            GameManagerH.Instance.HideLoadingScreen();
             UrdfManagerH.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
             MeshImporterH.Instance.OnMeshImported -= OnModelLoaded;

        }
    }

    public void destroyObjects(){

        Loaded = false;

        loadedModels = 0;
        allModels = 0;
        foreach(KeyValuePair<string, GameObject> kvp in objectsModels){
            Destroy(kvp.Value);
        }
        
        objectsModels.Clear (); 
    }



        public void OnModelLoaded(object sender, ImportedMeshEventArgsH args) {

            if (objectsModels.TryGetValue(args.Name, out GameObject gameObject)) {         
                args.RootGameObject.gameObject.transform.parent = gameObject.transform;
                Vector3 vec = gameObject.transform.Find("FrontPlate").transform.localPosition;
                
                args.RootGameObject.gameObject.transform.localPosition = vec;
                args.RootGameObject.gameObject.transform.localScale /= 2; // args.RootGameObject.gameObject.transform.localScale
                loadedModels++;
            }         

            
        }

         private void OnRobotModelLoaded(object sender, RobotUrdfModelArgs args) {

            // check if the robot of the type we need was loaded
            if (objectsModels.TryGetValue(args.RobotType, out GameObject gameObject)) {
                // if so, lets ask UrdfManagerH for the robot model
               RobotModelH RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(args.RobotType);
               
         
              RobotModel.RobotModelGameObject.gameObject.transform.parent = gameObject.transform;
           
              Vector3 vec = gameObject.transform.Find("FrontPlate").transform.localPosition;
              if (RobotModel.RobotModelGameObject.name.Equals("Eddie")){
               vec = new Vector3(-0.0229000002f,-0.0340999998f,-0.0498000011f);
              }
              //Vector3(-0.0229000002,-0.0340999998,-0.0498000011)
              RobotModel.RobotModelGameObject.gameObject.transform.localPosition = vec;
              RobotModel.RobotModelGameObject.gameObject.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
              RobotModel.RobotModelGameObject.gameObject.transform.localEulerAngles = new Vector3(0,90,0);

              RobotModel.SetActiveAllVisuals(true);
              loadedModels++;
                
                // if robot is loaded, unsubscribe from UrdfManagerH event
           //     UrdfManagerH.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
            }
        }

    

    public void LoadModels(object sender, EventArgs args) {
        GameManagerH.Instance.ShowLoadingScreen();
        //        if ( GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor) {
        destroyObjects();
         UrdfManagerH.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
         MeshImporterH.Instance.OnMeshImported += OnModelLoaded;

        ActionsManagerH.Instance.OnActionsLoaded -= LoadModels;


            // create one button for each object type
        foreach (ActionObjectMetadataH actionObject in ActionsManagerH.Instance.ActionObjectsMetadata.Values.OrderBy(x => x.Type)) {

            // TODO: remove and find a problem with milling machine not loaded model
            if (actionObject.Type == "MillingMachine")
                continue;

            if (actionObject.Abstract || actionObject.CollisionObject)
                    continue;
            
                if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(actionObject.Type, out RobotMeta robotMeta)) {
                        allModels++;
                if (!string.IsNullOrEmpty(robotMeta.UrdfPackageFilename)) {
                    GameObject selectCube = Instantiate(objectCubePrefab);
                    selectCube.GetComponentInChildren<TextMeshPro>().text = robotMeta.Type;
                    selectCube.transform.localScale = new Vector3(3f,3f,3f);
                    selectCube.transform.parent = models.transform;
                    selectCube.transform.GetComponentInChildren<Interactable>().OnClick.AddListener(() => CreateActionObject(actionObject.Type));
                //    selectCube.transform.
                    objectsModels.Add(robotMeta.Type, selectCube);
                    models.GetComponent<GridObjectCollection>().UpdateCollection();
                        // Get the robot model, if it returns null, the robot will be loading itself
                    RobotModelH RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(robotMeta.Type, robotMeta.UrdfPackageFilename);
                    
                        if (RobotModel != null) {
                            loadedModels++;
                                
                            RobotModel.RobotModelGameObject.gameObject.transform.parent = selectCube.transform;
                            GameObject frontPlate = selectCube.transform.Find("FrontPlate").gameObject;
                            //   frontPlate.GetComponent<Interactable>().OnClick.AddListener(() => AddObjectToScene(actionObject.Type));
                            if (RobotModel.RobotModelGameObject.name.Equals("Eddie")){
                                    RobotModel.RobotModelGameObject.gameObject.transform.localPosition = new Vector3(-0.0229000002f,-0.0340999998f,-0.0498000011f);
                            }
                            else{
                                RobotModel.RobotModelGameObject.gameObject.transform.localPosition =  frontPlate.transform.localPosition;
                            } 
                            RobotModel.RobotModelGameObject.gameObject.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
                            RobotModel.RobotModelGameObject.gameObject.transform.localEulerAngles = new Vector3(0,90,0);
                            RobotModel.SetActiveAllVisuals(true);
                        } else {
                            // Robot is not loaded yet, let's wait for it to be loaded
                          //  UrdfManagerH.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                        }
                    }
                }                
                else if(actionObject.HasPose){
                    allModels++;
           
                          GameObject selectCube = Instantiate(objectCubePrefab);
                          selectCube.GetComponentInChildren<TextMeshPro>().text = actionObject.Type;
                          selectCube.transform.localScale = new Vector3(3f,3f,3f);
                          selectCube.transform.parent = models.transform;
                          selectCube.transform.GetComponentInChildren<Interactable>().OnClick.AddListener(() => CreateActionObject(actionObject.Type));

                          objectsModels.Add(actionObject.Type, selectCube);
                          models.GetComponent<GridObjectCollection>().UpdateCollection();
                          MeshImporterH.Instance.LoadModel(actionObject.ObjectModel.Mesh, actionObject.Type);
                }
                
        }
            //    }
        
        
    }

  /*  private void AddObjectToScene(string type) {
        if (ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(type, out ActionObjectMetadataH actionObjectMetadata)) {            
            ShowAddObjectDialog(type);
        } else {

            //ERROR
        }

    }*/

    public async void CreateActionObject(string type) {
        string newActionObjectName = SceneManagerH.Instance.GetFreeAOName(type);

        if (ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(type, out ActionObjectMetadataH actionObjectMetadata)) {
               List<IO.Swagger.Model.Parameter> parameters = new List<IO.Swagger.Model.Parameter>();
            foreach (IO.Swagger.Model.ParameterMeta meta in actionObjectMetadata.Settings) {
                IO.Swagger.Model.ActionParameter ap = new IO.Swagger.Model.ActionParameter(name: meta.Name, value: JsonConvert.SerializeObject(meta.DefaultValue), type: meta.Type);
                parameters.Add(DataHelper.ActionParameterToParameter(ap));

               
            }

         /*
            foreach (IParameter actionParameter in actionParameters) {
                if (!parametersMetadata.TryGetValue(actionParameter.GetName(), out Base.ParameterMetadata actionParameterMetadata)) {
                    Base.Notifications.Instance.ShowNotification("Failed to create new action object", "Failed to get metadata for action object parameter: " + actionParameter.GetName());
                    return;
                }
                IO.Swagger.Model.ActionParameter ap = new IO.Swagger.Model.ActionParameter(name: actionParameter.GetName(), value: JsonConvert.SerializeObject(actionParameter.GetValue()), type: actionParameterMetadata.Type);
                parameters.Add(DataHelper.ActionParameterToParameter(ap));
            }*/
            try {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0f));
                Vector3 point = TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(ray.GetPoint(0.5f)));
                IO.Swagger.Model.Pose pose = null;
                if (actionObjectMetadata.HasPose)
                    pose = new IO.Swagger.Model.Pose(position: DataHelper.Vector3ToPosition(point), orientation: DataHelper.QuaternionToOrientation(Quaternion.identity));
                
                await WebSocketManagerH.Instance.AddObjectToScene(newActionObjectName, type, pose, parameters);
             //   callback?.Invoke();
         //       Close();
            } catch (Base.RequestFailedException e) {
                HNotificationManager.Instance.ShowNotification("Failed to add action " + e.Message);
            }
        }
    }



    private void AddVirtualCollisionObjectResponseCallback(string objectType, string data) {
        AddVirtualCollisionObjectToSceneResponse response = JsonConvert.DeserializeObject<AddVirtualCollisionObjectToSceneResponse>(data);
        if (response == null || !response.Result) {
            HNotificationManager.Instance.ShowNotification($"Failed to add {objectType} :" +  response.Messages.FirstOrDefault());
        } 
    }
    public async void CreateCube() {
        ObjectTypeMeta newObjectType = CreateObjectTypeMeta(CollisionObjectType.Cube);
        await WebSocketManagerH.Instance.AddVirtualCollisionObjectToScene(newObjectType.Type, newObjectType.ObjectModel, HSight.Instance.CreatePoseInTheView(), AddVirtualCollisionObjectResponseCallback);        
    }

     public async void CreatePlne() {
        ObjectTypeMeta newObjectType = CreateObjectTypeMeta(CollisionObjectType.Plane);
        await WebSocketManagerH.Instance.AddVirtualCollisionObjectToScene(newObjectType.Type, newObjectType.ObjectModel, HSight.Instance.CreatePoseInTheView(), AddVirtualCollisionObjectResponseCallback);        
    }

    public async void CreateCylinder() {
        ObjectTypeMeta newObjectType = CreateObjectTypeMeta(CollisionObjectType.Cylinder);
        await WebSocketManagerH.Instance.AddVirtualCollisionObjectToScene(newObjectType.Type, newObjectType.ObjectModel, HSight.Instance.CreatePoseInTheView(), AddVirtualCollisionObjectResponseCallback);
    }

    public async void CreateSphere() {
        ObjectTypeMeta newObjectType = CreateObjectTypeMeta(CollisionObjectType.Sphere);
        await WebSocketManagerH.Instance.AddVirtualCollisionObjectToScene(newObjectType.Type, newObjectType.ObjectModel, HSight.Instance.CreatePoseInTheView(), AddVirtualCollisionObjectResponseCallback);
    }

     public IO.Swagger.Model.ObjectTypeMeta CreateObjectTypeMeta(CollisionObjectType collisionObjectType) {
        string name;
        IO.Swagger.Model.ObjectModel objectModel = new IO.Swagger.Model.ObjectModel();
        
        IO.Swagger.Model.ObjectTypeMeta objectTypeMeta;
        IO.Swagger.Model.ObjectModel.TypeEnum modelType = new IO.Swagger.Model.ObjectModel.TypeEnum();
        switch (collisionObjectType) {
            case CollisionObjectType.Cube:
                name = SceneManagerH.Instance.GetFreeObjectTypeName("Cube");
                modelType = IO.Swagger.Model.ObjectModel.TypeEnum.Box;
                decimal sizeX = 0.1m;
                decimal sizeY = 0.1m;
                decimal sizeZ = 0.1m;
                IO.Swagger.Model.Box box = new IO.Swagger.Model.Box(name, sizeX, sizeY, sizeZ);
                objectModel.Box = box;
                break;
            case CollisionObjectType.Sphere:
                name = SceneManagerH.Instance.GetFreeObjectTypeName("Sphere");
                modelType = IO.Swagger.Model.ObjectModel.TypeEnum.Sphere;
                decimal radius = 0.1m;
                IO.Swagger.Model.Sphere sphere = new IO.Swagger.Model.Sphere(name, radius);
                objectModel.Sphere = sphere;
                break;
            case CollisionObjectType.Cylinder:
                name = SceneManagerH.Instance.GetFreeObjectTypeName("Cylinder");
                modelType = IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder;
                decimal cylinderRadius = 0.1m;
                decimal cylinderHeight = 0.1m;
                IO.Swagger.Model.Cylinder cylinder = new IO.Swagger.Model.Cylinder(name, cylinderHeight, cylinderRadius);
                objectModel.Cylinder = cylinder;
                break;
            case CollisionObjectType.Plane:
                name = SceneManagerH.Instance.GetFreeObjectTypeName("Plane");
                modelType = IO.Swagger.Model.ObjectModel.TypeEnum.Box;
                IO.Swagger.Model.Box plane = new IO.Swagger.Model.Box(name, 1.2m, 0.02m, 0.8m);
                objectModel.Box = plane;
                break;

            default:
                
               // break;
                throw new Exception();
        }
        objectModel.Type = modelType;
        objectTypeMeta = new IO.Swagger.Model.ObjectTypeMeta(builtIn: false, description: "", type: name, objectModel: objectModel,
            _base: "CollisionObject", hasPose: true, modified: DateTime.Now);


        return objectTypeMeta;
    }

}
