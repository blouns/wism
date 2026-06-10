using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using WismCompanion.State;

namespace WismCompanion.UI
{
    public enum LogViewMode { Raw, Simple }

    /// <summary>
    /// Binds the per-channel event log to a virtualized ListView and shows the selected entry's
    /// detail. Raw mode shows all entries; Simple mode shows Commands only. A text filter narrows
    /// the visible set in either mode.
    /// </summary>
    public sealed class LogView
    {
        private readonly ListView list;
        private readonly Label detail;
        private List<CompanionLogEntry> source = new();
        private List<CompanionLogEntry> displayed = new();
        private LogViewMode mode = LogViewMode.Simple;
        private string filter = string.Empty;

        public LogView(ListView list, Label detail)
        {
            this.list = list;
            this.detail = detail;

            list.fixedItemHeight = 22f;
            list.selectionType = SelectionType.Single;
            list.makeItem = MakeItem;
            list.bindItem = BindItem;
            list.itemsSource = displayed;
            list.selectionChanged += OnSelectionChanged;
        }

        public int DisplayedCount => displayed.Count;
        public int SourceCount    => source.Count;

        public void SetEntries(IReadOnlyList<CompanionLogEntry> entries)
        {
            source = new List<CompanionLogEntry>(entries);
            ApplyFilter();
        }

        public void SetMode(LogViewMode newMode)
        {
            mode = newMode;
            ApplyFilter();
        }

        public void SetFilter(string text)
        {
            filter = text ?? string.Empty;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<CompanionLogEntry> view = source;

            if (mode == LogViewMode.Simple)
                view = view.Where(e => e.Category == "Command" || e.Category == "Battle");

            if (!string.IsNullOrWhiteSpace(filter))
                view = view.Where(e => e.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase));

            displayed = view.ToList();
            list.itemsSource = displayed;
            list.ClearSelection();
            list.Rebuild();
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
            if (index < 0 || index >= displayed.Count)
                return;

            var entry = displayed[index];
            element.EnableInClassList("log-row--command", entry.Category == "Command");
            element.EnableInClassList("log-row--battle",  entry.Category == "Battle");
            element.EnableInClassList("log-row--map",     entry.Category == "Map");

            element.Q<Label>("time").text = entry.LocalTime;
            element.Q<Label>("summary").text = entry.Summary;
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            if (detail == null)
                return;

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
