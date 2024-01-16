using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Swagger.Model;
using RosSharp.Urdf;
using TMPro;
using UnityEngine;
using Base;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI;


namespace Hololens {

    public class RobotActionObjectH : ActionObjectH, HIRobot
    {

        public TextMeshPro ActionObjectName;
        public GameObject RobotPlaceholderPrefab;
        public GameObject LockIcon;

        public GameObject interactObject;
  
        public bool ResourcesLoaded = false;

        [SerializeField]
        private GameObject EEOrigin;

        private bool eeVisible = false;

        public RobotModelH RobotModel {
            get; private set;
        }
        public bool manipulationStarted {
            get;
            private set;
        }
        public bool updatePose {
            get;
            private set;
        }

        private bool robotVisible = false;

        private bool interactSet = false;

        public Dictionary<string, List<HRobotEE>> EndEffectors = new Dictionary<string, List<HRobotEE>>();
        
        private GameObject RobotPlaceholder;

        private List<Renderer> robotRenderers = new List<Renderer>();
        private List<Collider> robotColliders = new List<Collider>();

        private bool transparent = false;
        private bool ghost = false;

        private Shader standardShader;
        private Shader ghostShader;
        private Shader transparentShader;

        private bool jointStateSubscribeIsValid = true;
        private bool modelLoading = false;

        private bool loadingEndEffectors = false;

        private bool isGreyColorForced;

        public RobotMeta RobotMeta;

        private GameObject model;

        // Start is called before the first frame update
        protected override void  Start()
        {
            base.Start();
            if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.PackageRunning && SceneManagerH.Instance.RobotsEEVisible && SceneManagerH.Instance.SceneStarted) {
                _ = EnableVisualisationOfEE();
            }
            SceneManagerH.Instance.OnSceneStateEvent += OnSceneStateEvent;
        }

        // ONDESTROY CANNOT BE USED BECAUSE OF ITS DELAYED CALL - it causes mess when directly creating project from scene
        private void OnDestroy() {
        //    base.OnDestroy();
            SceneManagerH.Instance.OnSceneStateEvent -= OnSceneStateEvent;
        //    DeleteActionObject();
        }

        private void OnSceneStateEvent(object sender, SceneStateEventArgs args) {
            UpdateColor();
            if (HasUrdf() && RobotModel != null)
                SetDefaultJoints();

            if (args.Event.State == SceneStateData.StateEnum.Stopped) {
                HideRobotEE();
            }
        }


        private void OnDisable() {
            SceneManagerH.Instance.OnShowRobotsEE -= OnShowRobotsEE;
            SceneManagerH.Instance.OnHideRobotsEE -= OnHideRobotsEE;
            SceneManagerH.Instance.OnSceneStateEvent -= OnSceneStateEvent;
        }

        private void OnEnable() {
            SceneManagerH.Instance.OnShowRobotsEE += OnShowRobotsEE;
            SceneManagerH.Instance.OnHideRobotsEE += OnHideRobotsEE;
            SceneManagerH.Instance.OnSceneStateEvent += OnSceneStateEvent;
        }
        
        private void OnShowRobotsEE(object sender, EventArgs e) {
            _ = EnableVisualisationOfEE();            
        }

        private void OnHideRobotsEE(object sender, EventArgs e) {
            _ = DisableVisualisationOfEE();
        }

        // Update is called once per frame
        protected override async void  Update()
        {
            base.Update();
        }

        private async void UpdatePose() {
            try {
                await WebSocketManagerH.Instance.UpdateActionObjectPose(Data.Id, GetPose());
            } catch (RequestFailedException e) {
            //    Notifications.Instance.ShowNotification("Failed to update action object pose", e.Message);
                ResetPosition();
            }
        }

        public void ShowRobotEE() {
            foreach (List<HRobotEE> eeList in EndEffectors.Values)
                foreach (HRobotEE ee in eeList) {
                    ee.gameObject.SetActive(true);
                }            
        }

        public void HideRobotEE() {
            foreach (List<HRobotEE> eeList in EndEffectors.Values) {
                foreach (HRobotEE ee in eeList) {
                    try {
                        ee.gameObject.SetActive(false);
                    } catch (Exception ex) when (ex is NullReferenceException || ex is MissingReferenceException) {
                        continue;
                    }
                }              
            }           
        }

        public async Task DisableVisualisationOfEE() {
            if (!eeVisible)
                return;
            eeVisible = false;
            if (EndEffectors.Count > 0) {
                await WebSocketManagerH.Instance.RegisterForRobotEvent(GetId(), false, RegisterForRobotEventRequestArgs.WhatEnum.Eefpose);
                HideRobotEE();
            }
        }
        

        public async Task EnableVisualisationOfEE() {
            if (eeVisible)
                return;
            eeVisible = true;
            if (!ResourcesLoaded)
                await LoadResources();
            if (EndEffectors.Count > 0) {
                await WebSocketManagerH.Instance.RegisterForRobotEvent(GetId(), true, RegisterForRobotEventRequestArgs.WhatEnum.Eefpose);
                ShowRobotEE();
            }
        }


        public async override void InitActionObject(IO.Swagger.Model.SceneObject sceneObject, Vector3 position, Quaternion orientation, ActionObjectMetadataH actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResources = true) {
            base.InitActionObject(sceneObject, position, orientation, actionObjectMetadata);
            // if there should be an urdf robot model
            if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(sceneObject.Type, out RobotMeta robotMeta)) {
               
                RobotMeta = robotMeta;
                if (!string.IsNullOrEmpty(robotMeta.UrdfPackageFilename)) {
                    // Get the robot model, if it returns null, the robot will be loading itself
                    RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(robotMeta.Type, robotMeta.UrdfPackageFilename);
                
                    if (RobotModel != null) {
                        RobotModelLoaded();
                    } else {
                        // Robot is not loaded yet, let's wait for it to be loaded
                    
                        UrdfManagerH.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                        modelLoading = true;
                    }
                }
            }
            
            ResourcesLoaded = false;
        }

         private void OnRobotModelLoaded(object sender, RobotUrdfModelArgs args) {
            Debug.Log("URDF:" + args.RobotType + " robot is fully loaded");

            // check if the robot of the type we need was loaded
            if (args.RobotType == Data.Type) {
                // if so, lets ask UrdfManagerH for the robot model
                RobotModel = UrdfManagerH.Instance.GetRobotModelInstance(Data.Type);
               
                RobotModelLoaded();
                
                // if robot is loaded, unsubscribe from UrdfManagerH event
                UrdfManagerH.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
                modelLoading = false;
            }
        }


        public GameObject getInteractObject(){
            return interactObject;
        }

        private async void RobotModelLoaded() {
            RobotModel.RobotModelGameObject.transform.localScale = new Vector3(1f,1f,1f);
            RobotModel.RobotModelGameObject.transform.parent = transform;
            RobotModel.RobotModelGameObject.transform.localPosition = Vector3.zero;
            RobotModel.RobotModelGameObject.transform.localRotation = Quaternion.identity;
           
            RobotModel.SetActiveAllVisuals(true);
            RobotPlaceholder.SetActive(false);

          /*  Outline oldOutline = gameObject.GetComponent<Outline>();
            Color colorX = oldOutline.OutlineColor;
            float widthO = oldOutline.OutlineWidth;

            Destroy(gameObject.GetComponent<Outline>());*/

            Destroy(RobotPlaceholder);


            robotColliders.Clear();
            robotRenderers.Clear();
            robotRenderers.AddRange(RobotModel.RobotModelGameObject.GetComponentsInChildren<Renderer>(true));
            robotColliders.AddRange(RobotModel.RobotModelGameObject.GetComponentsInChildren<Collider>(true));
          //  interactObject.GetComponentInChildren<Interactable>().OnClick.AddListener(() => HSelectorManager.Instance.OnSelectObject(this) );
            gameObject.GetComponent<Interactable>().OnClick.AddListener(() => HSelectorManager.Instance.OnSelectObject(this) );

             /*UrdfRobot urdfRobot = RobotModel.RobotModelGameObject.GetComponent<UrdfRobot>();

             urdfRobot.g*/

         //   Bounds totalBounds = new Bounds ();
            if (robotRenderers.Count > 0) 
            {
                Bounds totalBounds = new Bounds ();

                totalBounds = robotRenderers[0].bounds;

                foreach(Renderer renderer in robotRenderers){
                    totalBounds.Encapsulate (renderer.bounds);
                }
          //      totalBounds.Encapsulate (robotColliders[6].bounds);
                interactObject.transform.localScale = transform.InverseTransformVector(totalBounds.size);
                interactObject.transform.position = totalBounds.center;      
                interactObject.transform.localRotation =  Quaternion.identity;
            }
     //       totalBounds += robotRenderers[4].bounds;
        //    totalBounds.
         /*   if (robotRenderers.Count > 5)*/
     //         if (robotRenderers.Count > 0) totalBounds = robotRenderers[0].bounds;
           
       /*     for (i = 1; i < count; i++) { 
                totalBounds.Encapsulate (robotColliders[i].bounds);
            }
*/
            if(RobotModel.RobotModelGameObject.GetComponent<Outline>() == null){
                 Outline outline =  RobotModel.RobotModelGameObject.AddComponent<Outline>();

                outline.OutlineColor = new Color(45,76,255,255);
                outline.OutlineWidth = 4;
                outline.enabled = false;

           
               // Destroy(RobotModel.RobotModelGameObject.GetComponent<Outline>());
            }
           
                gameObject.GetComponent<ObjectManipulator>().OnHoverEntered.AddListener((a) =>  RobotModel.RobotModelGameObject.GetComponent<Outline>().enabled = true );
                gameObject.GetComponent<ObjectManipulator>().OnHoverExited.AddListener((a) => RobotModel.RobotModelGameObject.GetComponent<Outline>().enabled = false );
         
        
            SetOutlineSizeBasedOnScale();

            SetVisibility(visibility, forceShaderChange: true);
            UpdateColor();

            SetDefaultJoints();

            if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.PackageRunning || GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.LoadingPackage)
                await WebSocketManagerH.Instance.RegisterForRobotEvent(GetId(), true, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
        }

        private void SetOutlineSizeBasedOnScale() {
            float robotScale = 0f;
            foreach (RobotLink link in RobotModel.Links.Values) {
                robotScale = link.LinkScale;
                if (!link.IsBaseLink && robotScale != 0) {
                    break;
                }
            }
     //       outlineOnClick.CompensateOutlineByModelScale(robotScale);
        }

        private void SetDefaultJoints() {
            foreach (var joint in RobotModel.Joints) {
                SetJointValue(joint.Key, 0f);
            }
        }

        public override Vector3 GetScenePosition() {
            return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
        }

        public override void SetScenePosition(Vector3 position) {
            Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
        }

        public override Quaternion GetSceneOrientation() {
            return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
        }

        public override void SetSceneOrientation(Quaternion orientation) {
            Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
        }

        public override void Show() {
            robotVisible = true;
            SetVisibility(1);
            UpdateColor();
        }

        public override void Hide() {
            robotVisible = false;
            SetVisibility(0);
        }

        public override void SetInteractivity(bool interactive) {
            foreach (Collider collider in robotColliders) {
                collider.enabled = interactive;
            }
        }

        public override void SetVisibility(float value, bool forceShaderChange = false) {
         /*   base.SetVisibility(value);

            if (standardShader == null) {
                standardShader = Shader.Find("Standard");
            }

            if (ghostShader == null) {
                ghostShader = Shader.Find("Custom/Ghost");
            }

            if (transparentShader == null) {
                transparentShader = Shader.Find("Transparent/Diffuse");
            }

            // Set opaque shader
            if (value >= 1) {
                transparent = false;
                ghost = false;
                foreach (Renderer renderer in robotRenderers) {
                    // Robot has its outline active, we need to select second material,
                    // (first is mask, second is object material, third is outline)
                    if (renderer.materials.Length == 3) {
                        renderer.materials[1].shader = standardShader;
                    } else {
                        renderer.material.shader = standardShader;
                    }
                }
            }
            // Set transparent shader
            else if (value <= 0.1) {
                ghost = false;
                if (forceShaderChange || !transparent) {
                    foreach (Renderer renderer in robotRenderers) {
                        // Robot has its outline active, we need to select second material,
                        // (first is mask, second is object material, third is outline)
                        if (renderer.materials.Length == 3) {
                            renderer.materials[1].shader = transparentShader;
                        } else {
                            renderer.material.shader = transparentShader;
                        }

                        Material mat;
                        if (renderer.materials.Length == 3) {
                            mat = renderer.materials[1];
                        } else {
                            mat = renderer.material;
                        }
                        Color color = mat.color;
                        color.a = 0f;
                        mat.color = color;
                    }
                    transparent = true;
                }
            } else {
                transparent = false;
                if (forceShaderChange || !ghost) {
                    foreach (Renderer renderer in robotRenderers) {
                        if (renderer.materials.Length == 3) {
                            renderer.materials[1].shader = ghostShader;
                        } else {
                            renderer.material.shader = ghostShader;
                        }
                    }
                    ghost = true;
                }
                // set alpha of the material
                foreach (Renderer renderer in robotRenderers) {
                    Material mat;
                    if (renderer.materials.Length == 3) {
                        mat = renderer.materials[1];
                    } else {
                        mat = renderer.material;
                    }
                    Color color = mat.color;
                    color.a = value;
                    mat.color = color;
                }
            }*/
        }
        
        public async Task<List<string>> GetEndEffectorIds(string arm_id = null) {
            await LoadResources();
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(arm_id)) {
                foreach (List<HRobotEE> eeList in EndEffectors.Values) {
                    foreach (HRobotEE ee in eeList) {
                        result.Add(ee.EEId);
                    }
                }
            } else if (EndEffectors.ContainsKey(arm_id)) {
                foreach (HRobotEE ee in EndEffectors[arm_id]) {
                    result.Add(ee.EEId);
                }
            } else {
                throw new KeyNotFoundException($"Robot {GetName()} does not contain arm {arm_id}");
            }
            
            return result;
        }

        private async Task LoadResources() {
            if (!ResourcesLoaded) {
                ResourcesLoaded = await LoadEndEffectorsAndArms();
            }
        }

        private Task<bool> WaitUntilResourcesReady() {
            return Task.Run(() => {
                while (true) {
                    if (ResourcesLoaded) {
                        return true; 
                    } else if (!loadingEndEffectors) {
                        return false;
                    } else {
                        Thread.Sleep(10);
                    }
                }
            });

        }

        public async Task<bool> LoadEndEffectorsAndArms() {
            if (!SceneManagerH.Instance.Valid) {
                Debug.LogError("SceneManager instance not valid");
                return false;
            }
            if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.PackageRunning) {
                loadingEndEffectors = false;
                GameManagerH.Instance.HideLoadingScreen();
                return true;
            }
            if (loadingEndEffectors) {
                await WaitUntilResourcesReady();
                return true;
            } else {
                loadingEndEffectors = true;
            }
            try {
                Dictionary<string, List<string>> endEffectors = new Dictionary<string, List<string>>();
                
                if (RobotMeta.MultiArm) {
                    List<string> arms = await WebSocketManagerH.Instance.GetRobotArms(Data.Id);
                    foreach (string arm in arms) {
                        endEffectors[arm] = await WebSocketManagerH.Instance.GetEndEffectors(Data.Id, arm);
                    }
                } else {
                    endEffectors["default"] = await WebSocketManagerH.Instance.GetEndEffectors(Data.Id);
                }
                foreach (KeyValuePair<string, List<string>> eeList in endEffectors) {
                    foreach (string eeId in eeList.Value) {
                        CreateEndEffector(eeList.Key, eeId);
                    }
                }
                
                return true;
            } catch (RequestFailedException ex) {
                Debug.LogError(ex.Message);
          //      Notifications.Instance.ShowNotification("Failed to load end effectors", ex.Message);
                return false;
            } finally {
                loadingEndEffectors = false;
          //      GameManagerH.Instance.HideLoadingScreen();
            }            
        }

        private HRobotEE CreateEndEffector(string armId, string eeId) {
            HRobotEE ee = Instantiate(SceneManagerH.Instance.RobotEEPrefab, EEOrigin.transform).GetComponent<HRobotEE>();
            ee.InitEE(this, armId, eeId);
            ee.gameObject.SetActive(false);
            if (!EndEffectors.ContainsKey(armId)) {
                EndEffectors.Add(armId, new List<HRobotEE>());
            }
            EndEffectors[armId].Add(ee);
            return ee;
        }

        public override void CreateModel(CollisionModels customCollisionModels = null) {
            RobotPlaceholder = Instantiate(RobotPlaceholderPrefab, transform);
            RobotPlaceholder.transform.parent = transform;
            RobotPlaceholder.transform.localPosition = Vector3.zero;
            RobotPlaceholder.transform.localPosition = Vector3.zero;

         //   RobotPlaceholder.GetComponent<OnClickCollider>().Target = gameObject;

            robotColliders.Clear();
            robotRenderers.Clear();
            robotRenderers.AddRange(RobotPlaceholder.GetComponentsInChildren<Renderer>());
            robotColliders.AddRange(RobotPlaceholder.GetComponentsInChildren<Collider>());

            Bounds totalBounds = new Bounds ();
            if (robotRenderers.Count > 0) totalBounds = robotRenderers[0].bounds;
    
            foreach (Renderer r in robotRenderers) {
                totalBounds.Encapsulate (r.bounds);
            }

            interactObject.transform.localScale = transform.InverseTransformVector(totalBounds.size);
            interactObject.transform.position = totalBounds.center;            
            interactObject.transform.rotation = transform.rotation;
        }

        public override GameObject GetModelCopy() {
            if (RobotModel?.RobotModelGameObject != null)
                return Instantiate(RobotModel.RobotModelGameObject);
            else
                return Instantiate(RobotPlaceholder);
        }

        public bool HasUrdf() {
            if (ActionsManagerH.Instance.RobotsMeta.TryGetValue(Data.Type, out RobotMeta robotMeta)) {
                return !string.IsNullOrEmpty(robotMeta.UrdfPackageFilename);
            }
            return false;
        }

        public override void UpdateObjectName(string newUserId) {
            base.UpdateObjectName(newUserId);
            ActionObjectName.text = newUserId;
        }

        public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger) {
            base.ActionObjectUpdate(actionObjectSwagger);
            ActionObjectName.text = actionObjectSwagger.Name;
            // update label on each end effector
            foreach (List<HRobotEE> arm in EndEffectors.Values) {
                foreach (HRobotEE ee in arm)
                    ee.UpdateLabel();
            }
            ResetPosition();
        }

        public async Task<HRobotEE> GetEE(string ee_id, string arm_id) {
            bool packageRunning = GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.PackageRunning ||
                GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.LoadingPackage;
            if (!packageRunning && !ResourcesLoaded) {
                await LoadResources();
            }

            string realArmId = arm_id;
            if (!MultiArm())
                realArmId = "default";

            if (!EndEffectors.ContainsKey(realArmId)) {
                if (packageRunning) {
                    EndEffectors.Add(realArmId, new List<HRobotEE>());
                } else {
                    throw new ItemNotFoundException($"Robot {GetName()} does not have arm {realArmId}");
                }
            }
            foreach (HRobotEE ee in EndEffectors[realArmId])
                if (ee.EEId == ee_id)
                    return ee;
            if (packageRunning) {
                return CreateEndEffector(realArmId, ee_id);
            }
            throw new ItemNotFoundException("End effector with ID " + ee_id + " not found for " + GetName());
        }

        /// <summary>
        /// Sets value of joints specified in List joints. Firstly checks if joint names are really equal or not.
        /// If some joint name is not correct, method will not allow to set the joints nor to check if they are valid, unless option forceJointsValidCheck is set to true.
        /// </summary>
        /// <param name="joints">List of joints with new angle values.</param>
        /// <param name="angle_in_degrees">Whether the joint angle is in degrees.</param>
        /// <param name="forceJointsValidCheck">If true, check for valid joint names will be called even if previous one failed.</param>
        public void SetJointValue(List<IO.Swagger.Model.Joint> joints, bool angle_in_degrees = false, bool forceJointsValidCheck = false) {
            if (RobotModel != null && (jointStateSubscribeIsValid || forceJointsValidCheck)) {
                if (CheckJointsAreValid(joints)) {
                    foreach (IO.Swagger.Model.Joint joint in joints) {
                        SetJointValue(joint.Name, (float) joint.Value);
                    }
                    jointStateSubscribeIsValid = true;
                } else {
                  //  Notifications.Instance.ShowNotification("Wrong joint names received!", "Unregistering joint state receiving for robot " + RobotModel.RobotType + ". Joints has to be named same as in urdf.");
                    jointStateSubscribeIsValid = false;
                }
            }
        }

        /// <summary>
        /// Checks if the joint names in joints corresponds to the joint names in RobotModel.
        /// </summary>
        /// <param name="joints"></param>
        /// <returns>True if joints have equal names, false if not.</returns>
        public bool CheckJointsAreValid(List<IO.Swagger.Model.Joint> joints) {
            if (RobotModel != null) {
                List<string> receivedJoints = new List<string>();
                foreach (IO.Swagger.Model.Joint joint in joints) {
                    receivedJoints.Add(joint.Name);
                }

                foreach (string jointName in RobotModel.Joints.Keys) {
                    receivedJoints.Remove(jointName);
                }

                if (receivedJoints.Count != 0) {
                    Debug.LogError("Received wrong joints: " + string.Join(",", joints) + " .. but expected: " + string.Join(",", RobotModel.GetJoints()));
             //       Notifications.Instance.ShowNotification("Received wrong joints!", "Received:" + string.Join(",", joints) + ".. but expected: " + string.Join(",", RobotModel.GetJoints()));
                    return false;
                } else {
                    return true;
                }
            } else {
                //Debug.LogError("Trying to set joint values, but robot urdf model is not loaded nor assigned.");
            }
            return false;
        }

        /// <summary>
        /// Sets the value of individual joint.
        /// </summary>
        /// <param name="name">Joint name.</param>
        /// <param name="angle">Joint angle (in radians by default).</param>
        /// <param name="angle_in_degrees">Whether the joint angle is in degrees.</param>
        public void SetJointValue(string name, float angle, bool angle_in_degrees = false) {
            RobotModel?.SetJointAngle(name, angle, angle_in_degrees);
        }

        public List<IO.Swagger.Model.Joint> GetJoints() {
            if (RobotModel == null) {
                // if urdf model is still loading, return empty joint list
                if (modelLoading) {
                    return new List<IO.Swagger.Model.Joint>();
                } else {
                    throw new RequestFailedException("Model not found for this robot.");
                }
            }
            else
                return RobotModel.GetJoints();
        }

	    public override void DeleteActionObject() {
            UnloadRobotModel();
            UrdfManagerH.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
            modelLoading = false;
            base.DeleteActionObject();
        }

        private void UnloadRobotModel() {
            // if RobotModel was present, lets return it to the UrdfManagerH robotModel pool
            if (RobotModel != null) {
                if (UrdfManagerH.Instance != null) {
                    // remove every outlines on the robot
              //      outlineOnClick.UnHighlight();
                    UrdfManagerH.Instance.ReturnRobotModelInstace(RobotModel);
                }
            }
        }

        /// <summary>
        /// Sets grey color of robot model (indicates that model is not in position of real robot)
        /// </summary>
        /// <param name="grey">True for setting grey, false for standard state.</param>
        public void SetGrey(bool grey, bool force = false) {
            isGreyColorForced = force && grey;
            if (force) {
                UpdateColor();
                return;
            }

            if (grey) {
                foreach (Renderer renderer in robotRenderers) {
                    foreach (Material mat in renderer.materials) {
                        mat.SetColor("_EmissionColor", new Color(0.2f, 0.05f, 0.05f));
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            } else {
                foreach (Renderer renderer in robotRenderers) {
                    foreach (Material mat in renderer.materials) {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }


        public override async void StartManipulation() {
            throw new NotImplementedException();
        }

        public async Task<List<HRobotEE>> GetAllEE() {
            await LoadResources();
            List<HRobotEE> eeList = new List<HRobotEE>();
            foreach (List<HRobotEE> ee in EndEffectors.Values)
                eeList.AddRange(ee);
            return eeList;
        }

        public override string GetObjectTypeName() {
            return "Robot";
        }

        public override void UpdateColor() {
            if (!HasUrdf())
                return;

            SetGrey(!SceneManagerH.Instance.SceneStarted || IsLockedByOtherUser || isGreyColorForced);
        }

        public override void OnObjectLocked(string owner) {
            base.OnObjectLocked(owner);
            if (IsLockedByOtherUser) {
                ActionObjectName.text = GetLockedText();
                LockIcon?.SetActive(true);
            }
        }

        public override void OnObjectUnlocked() {
            base.OnObjectUnlocked();
            ActionObjectName.text = GetName();
            LockIcon?.SetActive(false);
        }

        public async Task<List<string>> GetArmsIds() {
            await LoadResources();
            return EndEffectors.Keys.ToList();
        }

        public bool MultiArm() {
            return RobotMeta.MultiArm;
        }

        public override void EnableVisual(bool enable) {
            if (RobotModel != null){
                RobotModel.RobotModelGameObject.SetActive(enable);
             //   interactObject.SetActive(enable);

            }
             //   RobotModel.RobotModelGameObject.SetActive(enable);
        }

        string HIRobot.LockOwner() {
            return LockOwner;
        }

        public override void UpdateModel() {
            return;
        }

        public override async Task<RequestResult> Movable() {
            RequestResult result = await base.Movable();
            if (result.Success && SceneManagerH.Instance.SceneStarted) {
                result.Success = false;
                result.Message = "Robot could only be manipulated when scene is offline.";
            }
            return result;
        }

        public HInteractiveObject GetInteractiveObject() {
            return this;
        }

    public void OnManipulationStarted(){

        model = GetModelCopy();
         
 /*       model.transform.SetParent(GizmoTransform);
        model.transform.rotation = interactiveObject.transform.rotation;
        model.transform.position = interactiveObject.transform.position;*/


    }

  

    public void setInterarction(GameObject interactComponents){

        BoxCollider collider = interactComponents.GetComponent<BoxCollider>();
        collider.size = getInteractObject().transform.localScale;
        collider.center = getInteractObject().transform.localPosition;

        BoundsControl boundsControl = interactComponents.GetComponent<BoundsControl>();
        ObjectManipulator objectManipulator = interactComponents.GetComponent<ObjectManipulator>();
        boundsControl.BoundsOverride = collider;
       
        boundsControl.ScaleLerpTime = 1L;
        objectManipulator.ScaleLerpTime = 1L;

        boundsControl.ScaleHandlesConfig.ShowScaleHandles = false;
        boundsControl.RotationHandlesConfig.ShowHandleForX = false;
        //boundsControl.RotationHandlesConfig.ShowHandleForY = false;
        boundsControl.RotationHandlesConfig.ShowHandleForZ = false;
        boundsControl.UpdateBounds();
    }

    }
}
