using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class FollowObject : Singleton<FollowObject>
{
    public GameObject followingObject;

    private Vector3 previousPosition;

    private void Start() {
        previousPosition = followingObject.transform.localPosition;
    }

    void Update() {
        Vector3 currentPosition = followingObject.transform.localPosition;
            if (Vector3.Distance(currentPosition, previousPosition) > 0f) {
                switch (HEndEffectorTransform3.Instance.selectedAxis) {
                    case Gizmo.Axis.X:
                        float lengthX = currentPosition.z - previousPosition.z;
                        gameObject.transform.localPosition += (Vector3.forward) * lengthX * HStepSelectorMenu3.Instance.slider.SliderValue;
                        break;
                    case Gizmo.Axis.Y:
                        float lengthY = currentPosition.x - previousPosition.x;
                        gameObject.transform.localPosition += (Vector3.right) * lengthY * HStepSelectorMenu3.Instance.slider.SliderValue;
                        break;
                    case Gizmo.Axis.Z:
                        float lengthZ = currentPosition.y - previousPosition.y;
                        gameObject.transform.position += (Vector3.up) * lengthZ * HStepSelectorMenu3.Instance.slider.SliderValue;
                        break;
                    default:
                        float lengthx = currentPosition.z - previousPosition.z;
                        gameObject.transform.localPosition += (Vector3.forward) * lengthx * HStepSelectorMenu3.Instance.slider.SliderValue;
                        float lengthy = currentPosition.x - previousPosition.x;
                        gameObject.transform.localPosition += (Vector3.right) * lengthy * HStepSelectorMenu3.Instance.slider.SliderValue;
                        float lengthz = currentPosition.y - previousPosition.y;
                        gameObject.transform.localPosition += (Vector3.up) * lengthz * HStepSelectorMenu3.Instance.slider.SliderValue;
                        break;
                }
            } else {
                gameObject.transform.localPosition = followingObject.transform.localPosition;
            }
        previousPosition = currentPosition;
    }

    public void SetFollowingObject(GameObject o) {
        followingObject = o;
    }
}
