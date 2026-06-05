using Wism.CompanionApp.WinForms;

namespace Wism.Companion.WinForms
{
    partial class DebugConsoleForm
    {
        private System.ComponentModel.IContainer components = null;
        private MapRenderer mapRenderer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            buttonRecord = new Button();
            buttonReplay = new Button();
            buttonClear = new Button();
            comboChannels = new ComboBox();
            labelChannel = new Label();
            panelToolbar = new Panel();
            labelLogStats = new Label();
            labelStatus = new Label();
            splitMain = new SplitContainer();
            mapRenderer = new MapRenderer();
            splitLog = new SplitContainer();
            dataGridLog = new DataGridView();
            textLogDetail = new TextBox();
            panelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitLog).BeginInit();
            splitLog.Panel1.SuspendLayout();
            splitLog.Panel2.SuspendLayout();
            splitLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridLog).BeginInit();
            SuspendLayout();
            // 
            // buttonRecord
            // 
            buttonRecord.Location = new Point(4, 4);
            buttonRecord.Name = "buttonRecord";
            buttonRecord.Size = new Size(79, 31);
            buttonRecord.TabIndex = 0;
            buttonRecord.Text = "Record";
            buttonRecord.UseVisualStyleBackColor = true;
            buttonRecord.Click += buttonRecord_Click;
            // 
            // buttonReplay
            // 
            buttonReplay.Location = new Point(89, 4);
            buttonReplay.Name = "buttonReplay";
            buttonReplay.Size = new Size(82, 31);
            buttonReplay.TabIndex = 1;
            buttonReplay.Text = "Replay";
            buttonReplay.UseVisualStyleBackColor = true;
            buttonReplay.Click += buttonReplay_Click;
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(177, 4);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(67, 31);
            buttonClear.TabIndex = 2;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // comboChannels
            // 
            comboChannels.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboChannels.DropDownStyle = ComboBoxStyle.DropDownList;
            comboChannels.FormattingEnabled = true;
            comboChannels.Location = new Point(313, 8);
            comboChannels.Name = "comboChannels";
            comboChannels.Size = new Size(304, 23);
            comboChannels.TabIndex = 4;
            comboChannels.SelectedIndexChanged += comboChannels_SelectedIndexChanged;
            // 
            // labelChannel
            // 
            labelChannel.AutoSize = true;
            labelChannel.Location = new Point(255, 12);
            labelChannel.Name = "labelChannel";
            labelChannel.Size = new Size(51, 15);
            labelChannel.TabIndex = 3;
            labelChannel.Text = "Channel";
            // 
            // panelToolbar
            // 
            panelToolbar.Controls.Add(labelLogStats);
            panelToolbar.Controls.Add(labelStatus);
            panelToolbar.Controls.Add(labelChannel);
            panelToolbar.Controls.Add(comboChannels);
            panelToolbar.Controls.Add(buttonClear);
            panelToolbar.Controls.Add(buttonReplay);
            panelToolbar.Controls.Add(buttonRecord);
            panelToolbar.Dock = DockStyle.Bottom;
            panelToolbar.Location = new Point(0, 485);
            panelToolbar.Name = "panelToolbar";
            panelToolbar.Size = new Size(784, 56);
            panelToolbar.TabIndex = 1;
            // 
            // labelLogStats
            // 
            labelLogStats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelLogStats.Location = new Point(623, 12);
            labelLogStats.Name = "labelLogStats";
            labelLogStats.Size = new Size(153, 15);
            labelLogStats.TabIndex = 5;
            labelLogStats.Text = "0 events";
            labelLogStats.TextAlign = ContentAlignment.TopRight;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            labelStatus.Location = new Point(6, 38);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(770, 15);
            labelStatus.TabIndex = 6;
            labelStatus.Text = "Disconnected";
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 0);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(mapRenderer);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(splitLog);
            splitMain.Size = new Size(784, 485);
            splitMain.SplitterDistance = 296;
            splitMain.TabIndex = 0;
            // 
            // mapRenderer
            // 
            mapRenderer.Dock = DockStyle.Fill;
            mapRenderer.Location = new Point(0, 0);
            mapRenderer.Name = "mapRenderer";
            mapRenderer.Size = new Size(784, 296);
            mapRenderer.TabIndex = 0;
            // 
            // splitLog
            // 
            splitLog.Dock = DockStyle.Fill;
            splitLog.Location = new Point(0, 0);
            splitLog.Name = "splitLog";
            // 
            // splitLog.Panel1
            // 
            splitLog.Panel1.Controls.Add(dataGridLog);
            // 
            // splitLog.Panel2
            // 
            splitLog.Panel2.Controls.Add(textLogDetail);
            splitLog.Size = new Size(784, 185);
            splitLog.SplitterDistance = 544;
            splitLog.TabIndex = 0;
            // 
            // dataGridLog
            // 
            dataGridLog.AllowUserToAddRows = false;
            dataGridLog.AllowUserToDeleteRows = false;
            dataGridLog.AllowUserToResizeRows = false;
            dataGridLog.BackgroundColor = SystemColors.Window;
            dataGridLog.BorderStyle = BorderStyle.None;
            dataGridLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridLog.Dock = DockStyle.Fill;
            dataGridLog.Location = new Point(0, 0);
            dataGridLog.MultiSelect = false;
            dataGridLog.Name = "dataGridLog";
            dataGridLog.ReadOnly = true;
            dataGridLog.RowHeadersVisible = false;
            dataGridLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridLog.Size = new Size(544, 185);
            dataGridLog.TabIndex = 0;
            dataGridLog.SelectionChanged += dataGridLog_SelectionChanged;
            // 
            // textLogDetail
            // 
            textLogDetail.BackColor = SystemColors.Window;
            textLogDetail.BorderStyle = BorderStyle.None;
            textLogDetail.Dock = DockStyle.Fill;
            textLogDetail.Font = new Font("Consolas", 9F);
            textLogDetail.Location = new Point(0, 0);
            textLogDetail.Multiline = true;
            textLogDetail.Name = "textLogDetail";
            textLogDetail.ReadOnly = true;
            textLogDetail.ScrollBars = ScrollBars.Vertical;
            textLogDetail.Size = new Size(236, 185);
            textLogDetail.TabIndex = 0;
            // 
            // DebugConsoleForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 541);
            Controls.Add(splitMain);
            Controls.Add(panelToolbar);
            Name = "DebugConsoleForm";
            Text = "WISM Companion";
            Load += DebugConsoleForm_Load;
            panelToolbar.ResumeLayout(false);
            panelToolbar.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            splitLog.Panel1.ResumeLayout(false);
            splitLog.Panel2.ResumeLayout(false);
            splitLog.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitLog).EndInit();
            splitLog.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridLog).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonRecord;
        private Button buttonReplay;
        private Button buttonClear;
        private ComboBox comboChannels;
        private Label labelChannel;
        private Panel panelToolbar;
        private Label labelLogStats;
        private Label labelStatus;
        private SplitContainer splitMain;
        private SplitContainer splitLog;
        private DataGridView dataGridLog;
        private TextBox textLogDetail;
    }
}
