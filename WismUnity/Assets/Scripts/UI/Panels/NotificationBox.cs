using System;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;


public class Message
{
    public string Text { get; set; }

    public int MyProperty { get; set; }
}

public class NotificationBox : MonoBehaviour
{
    public const float DefaultInterval = 3f;

    private Text notificationText;   
    private string message;
    private CanvasGroup infoPanelGroup;
    private float timer;
    private float waitTime = DefaultInterval;

    public void Start()
    {
        this.notificationText = UnityUtilities.GameObjectHardFind("NotificationBox")
            .GetComponent<Text>();

        this.infoPanelGroup = UnityUtilities.GameObjectHardFind("InformationPanel")
            .GetComponent<CanvasGroup>();
    }

    public void Update()
    {
        timer += Time.deltaTime;
        if (timer > waitTime)
        {
            ClearNotification();
        }
        else
        {
            ShowNotifications();
        }
    }

    private void ShowNotifications()
    {        
        this.notificationText.text = this.message;
        infoPanelGroup.alpha = 0f;
    }

    public void ClearNotification()
    {
        this.message = "";
        infoPanelGroup.alpha = 1f;
    }

    public void Notify(string message, double interval = DefaultInterval)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        this.infoPanelGroup.alpha = 0f;
        this.message = message;
        timer = 0f;
        ShowNotifications();
    }
}
