using System.Collections;
using Base;
using TMPro;
using UnityEngine;

public class HNotificationWindow : Singleton<HNotificationWindow>
{
    public GameObject notificationWindow;
    public TextMeshPro notificationText;

    // Start is called before the first frame update
    void Start()
    {
        notificationWindow.gameObject.SetActive(false);
    }

    public void ShowNotification(string text)
    {
        notificationWindow.gameObject.SetActive(true);
        notificationText.text = text;
        StartCoroutine(HideNotification());
    }

    private IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(2f);
        notificationWindow.gameObject.SetActive(false);
    }
}
