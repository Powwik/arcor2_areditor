using System.Collections;
using Base;
using TMPro;
using UnityEngine;

public class HNotificationWindow : Singleton<HNotificationWindow>
{
    public GameObject notificationWindow;
    public TextMeshPro notificationText;

    // Start is called before the first frame update
    private void Start()
    {
        notificationWindow.SetActive(false);
    }

    public void ShowNotification(string text)
    {
        notificationWindow.SetActive(true);
        notificationText.text = text;
        StartCoroutine(HideNotification());
    }

    private IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(3f);
        notificationWindow.SetActive(false);
    }
}
