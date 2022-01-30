using Assets.Scripts.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

public class YesNoBox : MonoBehaviour
{
    private Text notificationText;
    private CanvasGroup canvasGroup;
    private bool? answer;
    private UnityManager unityManager;

    public bool? Answer { get => this.answer; set => this.answer = value; }

    public bool Cancelled { get; private set; }

    public void Start()
    {
        var yesNoGO = this.gameObject;

        this.unityManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<UnityManager>();
        this.notificationText = yesNoGO.transform.Find("Message").GetComponent<Text>();
        this.canvasGroup = yesNoGO.GetComponent<CanvasGroup>();
    }

    public void Update()
    {
        if (this.Answer.HasValue || this.Cancelled)
        {
            Hide();
        }
    }

    public void Ask(string message, params object[] args)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        this.notificationText.text = String.Format(message, args);
        Clear();
        Show();
    }

    public bool IsActive()
    {
        return this.canvasGroup.alpha == 1f;
    }

    private void Show()
    {
        this.canvasGroup.alpha = 1f;
        this.canvasGroup.interactable = true;
        this.canvasGroup.blocksRaycasts = true;

        // Automatic "Yes" if non-interactive UI
        if (!this.unityManager.InteractiveUI)
        {
            this.Answer = true;
        }
    }

    private void Hide()
    {
        this.canvasGroup.alpha = 0f;
        this.canvasGroup.interactable = false;
        this.canvasGroup.blocksRaycasts = false;
    }

    public void Clear()
    {
        this.Answer = null;
        this.Cancelled = false;
    }

    public void Yes()
    {
        this.Answer = true;
    }

    public void No()
    {
        this.Answer = false;
    }

    public void Cancel()
    {
        this.Cancelled = true;
    }
}
