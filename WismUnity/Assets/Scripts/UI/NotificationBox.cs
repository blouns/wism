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
    public const double DefaultInterval = 5000;

    private Text notificationText;
    private Timer timer;
    private bool timerElapsed;
    private string message;

    public void Start()
    {
        this.notificationText = GameObject.FindGameObjectWithTag("NotificationBox")
            .GetComponent<Text>();
    }

    public void Update()
    {        
        if (timerElapsed)
        {
            ClearNotification();
            this.timerElapsed = false;
        }

        ShowNotifications();
    }

    private void ShowNotifications()
    {
        this.notificationText.text = this.message;
    }

    public void ClearNotification()
    {
        this.message = "";
    }

    public void Notify(string message, double interval = DefaultInterval)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        this.message = message;
        this.timer = new Timer(interval);
        this.timer.Elapsed += Timer_Elapsed;
        this.timer.Start();
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        this.timerElapsed = true;
    }
}
