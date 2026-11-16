using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

public class YesNoBox : MonoBehaviour
{
    private Text notificationText;
    private CanvasGroup canvasGroup;
    private bool? answer;
    private UnityManager unityManager;
    private float minimumDockHeight;
    private Vector2 lastDockParentSize;

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
            if (this.canvasGroup != null && IsActive() && WismUiInputAdapter.CancelPressedThisFrame()) No();
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

    public void AskFullWidth(string message)
    {
        var rect = (RectTransform)this.transform;
        var parent = (RectTransform)rect.parent;
        if (!IsActive() || this.lastDockParentSize != parent.rect.size)
        {
            LayoutDock(rect, parent);
        }
        Ask(message);
    }

    private void LayoutDock(RectTransform rect, RectTransform parent)
    {
        var dock = UnityUtilities.GameObjectHardFind("InformationPanel")?.transform as RectTransform;
        if (this.minimumDockHeight <= 0f) this.minimumDockHeight = rect.rect.height;
        this.lastDockParentSize = parent.rect.size;
        var height = this.minimumDockHeight;
        if (dock != null)
        {
            var corners = new Vector3[4];
            dock.GetWorldCorners(corners);
            var dockTop = parent.InverseTransformPoint(corners[1]).y;
            height = Mathf.Max(height, dockTop - parent.rect.yMin);
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.right;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, Mathf.Min(height, parent.rect.height));
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        PlaceDockControl("Message", new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.98f));
        PlaceDockControl("Question", new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.62f));
        PlaceDockControl("YesButton", new Vector2(0.04f, 0.04f), new Vector2(0.19f, 0.32f));
        PlaceDockControl("NoButton", new Vector2(0.20f, 0.04f), new Vector2(0.35f, 0.32f));
        PlaceDockControl("CancelButton", new Vector2(0.65f, 0.04f), new Vector2(0.96f, 0.32f));
    }

    private void PlaceDockControl(string name, Vector2 min, Vector2 max)
    {
        if (!(this.transform.Find(name) is RectTransform control)) return;
        control.anchorMin = min;
        control.anchorMax = max;
        control.offsetMin = control.offsetMax = Vector2.zero;
        var label = control.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.resizeTextMaxSize = label.fontSize;
            label.resizeTextMinSize = Mathf.Min(10, label.fontSize);
            label.resizeTextForBestFit = true;
        }
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
