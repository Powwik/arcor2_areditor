using System.Collections;
using Base;
using TMPro;
using UnityEngine;

/*********************************************************************
 * \file HNotificationWindow.cs
 * \the main script for the notification window
 * 
 * \author Daniel Zmrzlý
 *********************************************************************/
public class HNotificationWindow : Singleton<HNotificationWindow>
{
    public GameObject notificationWindow;
    public TextMeshPro notificationText;

    // Start is called before the first frame update
    private void Start()
    {
        notificationWindow.SetActive(false);
    }

    /**
     * Function shows notification in the space in front of the user
     * 
     * \param[in] text      text of the notification
     */
    public void ShowNotification(string text)
    {
        notificationWindow.SetActive(true);
        notificationText.text = text;
        StartCoroutine(HideNotification());
    }

    /**
     * Function hides active notification window after 3 seconds
     * 
     */
    private IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(3f);
        notificationWindow.SetActive(false);
    }
}
