using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class HFollowObject : Singleton<HFollowObject>
{
    public GameObject FollowingObject;

    private Vector3 previousPosition;

    private void Start()
    {
        previousPosition = FollowingObject.transform.localPosition;
    }

    private void Update()
    {
        Vector3 currentPosition = FollowingObject.transform.localPosition;

        if (Vector3.Distance(currentPosition, previousPosition) > 0.00001f)
        {
            switch (HEndEffectorTransform.Instance.selectedAxis)
            {
                case Gizmo.Axis.X:
                    float lengthX = currentPosition.z - previousPosition.z;
                    Vector3 vAxisX = (Vector3.forward) * lengthX * HSliderMenu.Instance.Slider.SliderValue;
                    gameObject.transform.localPosition += vAxisX;
                    break;
                case Gizmo.Axis.Y:
                    float lengthY = currentPosition.x - previousPosition.x;
                    Vector3 vAxisY = (Vector3.right) * lengthY * HSliderMenu.Instance.Slider.SliderValue;
                    gameObject.transform.localPosition += vAxisY;
                    break;
                case Gizmo.Axis.Z:
                    float lengthZ = currentPosition.y - previousPosition.y;
                    Vector3 vAxisZ = (Vector3.up) * lengthZ * HSliderMenu.Instance.Slider.SliderValue;
                    gameObject.transform.localPosition += vAxisZ;
                    break;
                default:
                    float lengthx = currentPosition.z - previousPosition.z;
                    gameObject.transform.localPosition += (Vector3.forward) * lengthx * HSliderMenu.Instance.Slider.SliderValue;
                    float lengthy = currentPosition.x - previousPosition.x;
                    gameObject.transform.localPosition += (Vector3.right) * lengthy * HSliderMenu.Instance.Slider.SliderValue;
                    float lengthz = currentPosition.y - previousPosition.y;
                    gameObject.transform.localPosition += (Vector3.up) * lengthz * HSliderMenu.Instance.Slider.SliderValue;
                    break;
            }
        }
        else
        {
            gameObject.transform.localPosition = FollowingObject.transform.localPosition;
        }
        previousPosition = currentPosition;
    }

    public void SetFollowingObject(GameObject o)
    {
        FollowingObject = o;
    }
}
