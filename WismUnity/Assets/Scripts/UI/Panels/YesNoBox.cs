using System;
using UnityEngine;
using UnityEngine.UI;

public class YesNoBox : MonoBehaviour
{
    private Text notificationText;
    private CanvasGroup canvasGroup;
    private bool? answer;

    public bool? Answer { get => answer; set => answer = value; }

    public void Start()
    {
        var yesNoGO = gameObject;

        this.notificationText = yesNoGO.transform.Find("Message").GetComponent<Text>();
        this.canvasGroup = yesNoGO.GetComponent<CanvasGroup>();
    }

    public void Update()
    {
        if (Answer.HasValue)
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
        return canvasGroup.alpha == 1f;
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Clear()
    {
        this.Answer = null;
    }

    public void Yes()
    {
        this.Answer = true;
    }

    public void No()
    {
        this.Answer = false;
    }
}
