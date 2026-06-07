using System.Collections.Generic;
using UnityEngine.UIElements;
using WismCompanion.State;

namespace WismCompanion.UI
{
    /// <summary>
    /// Binds the per-channel event log to a virtualized ListView and shows the selected entry's
    /// detail. Mirrors the WinForms companion's "time | kind | event | result" log + detail pane.
    /// </summary>
    public sealed class LogView
    {
        private readonly ListView list;
        private readonly Label detail;
        private List<CompanionLogEntry> items = new();

        public LogView(ListView list, Label detail)
        {
            this.list = list;
            this.detail = detail;

            list.fixedItemHeight = 22f;
            list.selectionType = SelectionType.Single;
            list.makeItem = MakeItem;
            list.bindItem = BindItem;
            list.itemsSource = items;
            list.selectionChanged += OnSelectionChanged;
        }

        public void SetEntries(IReadOnlyList<CompanionLogEntry> entries)
        {
            items = new List<CompanionLogEntry>(entries);
            list.itemsSource = items;
            list.ClearSelection();
            list.RefreshItems();
        }

        private static VisualElement MakeItem()
        {
            var row = new VisualElement();
            row.AddToClassList("log-row");

            var time = new Label { name = "time" };
            time.AddToClassList("log-time");
            row.Add(time);

            var summary = new Label { name = "summary" };
            summary.AddToClassList("log-summary");
            row.Add(summary);

            return row;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return;
            }

            var entry = items[index];
            element.EnableInClassList("log-row--command", entry.Category == "Command");
            element.EnableInClassList("log-row--map", entry.Category == "Map");

            element.Q<Label>("time").text = entry.LocalTime;
            element.Q<Label>("summary").text = entry.Summary;
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            if (detail == null)
            {
                return;
            }

            foreach (var item in selection)
            {
                if (item is CompanionLogEntry entry)
                {
                    detail.text = entry.Detail;
                    return;
                }
            }

            detail.text = string.Empty;
        }
    }
}
