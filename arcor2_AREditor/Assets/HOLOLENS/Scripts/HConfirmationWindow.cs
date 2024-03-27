using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class HConfirmationWindow : Singleton<HConfirmationWindow>
{
    public GameObject ConfirmationWindow;
    public Interactable ConfirmButton;
    public Interactable ResetButton;
    public Vector3 ConfirmationMenuVectorUp = new Vector3(0.0f, 0.33f, 0.0f);

    // Start is called before the first frame update
    private void Start()
    {
        ConfirmButton.OnClick.AddListener(() => ConfirmClicked());
        ResetButton.OnClick.AddListener(() => ResetClicked());
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    private void ConfirmClicked()
    {
        ConfirmationWindow.SetActive(false);
        HEndEffectorTransform.Instance.ConfirmClicked();
    }

    private void ResetClicked()
    {
        ConfirmationWindow.gameObject.SetActive(false);
        HEndEffectorTransform.Instance.ResetClicked();
    }
}
