using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
    
    public class PrimaryPointerHandler : Base.Singleton<PrimaryPointerHandler>
    {
        public GameObject VirtualPointer;
        bool isHand = false;

        private void OnEnable()
        {
            CoreServices.InputSystem?.FocusProvider?.SubscribeToPrimaryPointerChanged(OnPrimaryPointerChanged, true);
        }

        void Update(){
            if(isHand){
                if(HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose pose)){
                    VirtualPointer.transform.position = pose.Position;
                }
            }
        }

        private void OnPrimaryPointerChanged(IMixedRealityPointer oldPointer, IMixedRealityPointer newPointer)
        {
            if (VirtualPointer != null)
            {
                if (newPointer != null)
                {
                    Transform parentTransform = null;

                    if(HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose pose)){
                        // parentTransform = pose.Position;
                        isHand = true;
                        VirtualPointer.transform.SetParent(null, false);
                        VirtualPointer.SetActive(true);
                        VirtualPointer.transform.position = pose.Position;
                    }
                    else {
                        isHand = false;

                        // For this example scene, use special logic if a GGVPointer becomes the primary pointer. 
                        // In particular, the GGV pointer defers its cursor management to the GazeProvider, which has its own internal pointer definition as well
                        // In the future, the pointer/cursor relationship will be reworked and standardized to remove this awkward set of co-dependencies
                        if (newPointer is GGVPointer)
                        {
                            parentTransform = CoreServices.InputSystem.GazeProvider.GazePointer.BaseCursor.TryGetMonoBehaviour(out MonoBehaviour baseCursor) ? baseCursor.transform : null;
                        }
                        else
                        {
                            parentTransform = newPointer.BaseCursor.TryGetMonoBehaviour(out MonoBehaviour baseCursor) ? baseCursor.transform : null;
                        }

                        // If there's no cursor try using the controller pointer transform instead
                        if (parentTransform == null)
                        {
                            if (newPointer.TryGetMonoBehaviour(out MonoBehaviour controllerPointer))
                            {
                                parentTransform = controllerPointer.transform;
                            }
                        }

                        if (parentTransform != null)
                        {
                            VirtualPointer.transform.SetParent(parentTransform, false);
                            VirtualPointer.SetActive(true);
                            return;
                        }
                    }
                }

                VirtualPointer.SetActive(false);
                VirtualPointer.transform.SetParent(null, false);
            }
        }

        private void OnDisable()
        {
            CoreServices.InputSystem?.FocusProvider?.UnsubscribeFromPrimaryPointerChanged(OnPrimaryPointerChanged);
            OnPrimaryPointerChanged(null, null);
        }
    
    }
