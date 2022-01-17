using Assets.Scripts.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SolicitInput : MonoBehaviour
{
    [SerializeField]
    private Button okButton;
    [SerializeField]
    private InputField inputField;
    [SerializeField]
    private Text message;

    private bool isInitialized;
    private CanvasGroup canvasGroup;

    public OkCancel OkCancelResult { get; private set; }

    public void Start()
    {
        this.canvasGroup = gameObject.GetComponent<CanvasGroup>();
    }

    public void Initialize(string message, string defaultInputText)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (defaultInputText is null)
        {
            throw new ArgumentNullException(nameof(defaultInputText));
        }
        
        this.OkCancelResult = OkCancel.None;
        this.message.text = message;
        this.inputField.text = defaultInputText;

        if (String.IsNullOrWhiteSpace(defaultInputText))
        {
            this.okButton.interactable = false;
        }        

        this.isInitialized = true;
    }

    public bool IsInitialized()
    {
        return this.isInitialized;
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(
            this.inputField.gameObject);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Ok()
    {
        this.OkCancelResult = OkCancel.Ok;
        Close();
    }

    public void Clear()
    {
        this.isInitialized = false;
        this.message.text = "";
        this.OkCancelResult = OkCancel.None;
    }

    public void OnEndEdit()
    {
        this.okButton.interactable = !String.IsNullOrWhiteSpace(this.inputField.text);
    }

    public string GetInputText()
    {
        return this.inputField.text;
    }

    public void SetInputText(string text)
    {
        this.inputField.text = text;
    }

    private void Close()
    {
        this.isInitialized = false;
        Hide();
    }
}
