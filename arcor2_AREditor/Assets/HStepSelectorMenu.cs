using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class HStepSelectorMenu : MonoBehaviour
{
    public GameObject stepSelectorMenu;
    public float step;

    public Interactable step1Button;
    public Interactable step2Button;
    public Interactable step5Button;
    // Start is called before the first frame update
    void Start()
    {
        //stepSelectorMenu.SetActive(false);
        step = 0;
        step1Button.OnClick.AddListener(() => setStep(1));
        step2Button.OnClick.AddListener(() => setStep(2));
        step5Button.OnClick.AddListener(() => setStep(5));
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void setStep(float number) {
        Debug.Log(number);
        step = number;
    }
}
