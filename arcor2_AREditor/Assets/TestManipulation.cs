using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Unity.Services.Analytics.Internal;
using UnityEngine.UIElements;

public class HandMovement : MonoBehaviour {

    public GameObject gizmoPrefab;
    public bool manipulation;
    public GameObject followingObject;
    private Vector3 previousPosition;
    private float value;
    
    private void Start()
    {
        value = 0.1f;
        manipulation = false;
        previousPosition = followingObject.transform.position;
        gameObject.GetComponent<ObjectManipulator>().OnManipulationStarted.AddListener((s) => ManipulationStarted());
        gameObject.GetComponent<ObjectManipulator>().OnManipulationEnded.AddListener((s) => ManipulationEnded());
    }

    private void Update()
    {
        Vector3 currentPosition = followingObject.transform.position;
        if (Vector3.Distance(currentPosition, previousPosition) > 0f)
        {
            switch (HEndEffectorTransform.Instance.selectedAxis)
            {
                case Gizmo.Axis.X:
                    float lengthX = currentPosition.x - previousPosition.x;
                    transform.position += (Vector3.right) * lengthX * value;
                    break;
                case Gizmo.Axis.Y:
                    float lengthY = currentPosition.y - previousPosition.y;
                    transform.position += (Vector3.up) * lengthY * value;
                    break;
                case Gizmo.Axis.Z:
                    float lengthZ = currentPosition.z - previousPosition.z;
                    transform.position += (Vector3.forward) * lengthZ * value;
                    break;
                default:
                    break;
            }
        }
        previousPosition = currentPosition;
    }

    private void ManipulationStarted()
    {

    }

    private void ManipulationEnded()
    {
        //gameObject.transform.parent.parent.gameObject.SetActive(false);
    }
}
