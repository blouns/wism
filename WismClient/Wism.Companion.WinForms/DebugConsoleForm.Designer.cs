using Wism.CompanionApp.WinForms;

namespace Wism.Companion.WinForms
{
    partial class DebugConsoleForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private MapRenderer mapRenderer;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonRecord = new Button();
            buttonReplay = new Button();
            panel1 = new Panel();
            split = new SplitContainer();
            mapRenderer = new MapRenderer();
            listBoxLog = new ListBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            SuspendLayout();
            // 
            // buttonRecord
            // 
            buttonRecord.Location = new Point(4, 3);
            buttonRecord.Name = "buttonRecord";
            buttonRecord.Size = new Size(79, 31);
            buttonRecord.TabIndex = 2;
            buttonRecord.Text = "Record";
            buttonRecord.UseVisualStyleBackColor = true;
            buttonRecord.Click += buttonRecord_Click;
            // 
            // buttonReplay
            // 
            buttonReplay.Location = new Point(89, 3);
            buttonReplay.Name = "buttonReplay";
            buttonReplay.Size = new Size(82, 31);
            buttonReplay.TabIndex = 3;
            buttonReplay.Text = "Replay";
            buttonReplay.UseVisualStyleBackColor = true;
            buttonReplay.Click += buttonReplay_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(buttonReplay);
            panel1.Controls.Add(buttonRecord);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 379);
            panel1.Name = "panel1";
            panel1.Size = new Size(621, 39);
            panel1.TabIndex = 4;
            // 
            // split
            // 
            split.Dock = DockStyle.Fill;
            split.Location = new Point(0, 0);
            split.Name = "split";
            split.Orientation = Orientation.Horizontal;
            // 
            // split.Panel1
            // 
            split.Panel1.Controls.Add(mapRenderer);
            // 
            // split.Panel2
            // 
            split.Panel2.Controls.Add(listBoxLog);
            split.Size = new Size(621, 379);
            split.SplitterDistance = 238;
            split.TabIndex = 0;
            // 
            // mapRenderer
            // 
            mapRenderer.Location = new Point(0, 0);
            mapRenderer.Name = "mapRenderer";
            mapRenderer.Dock = DockStyle.Fill;
            mapRenderer.TabIndex = 0;
            // 
            // listBoxLog
            // 
            listBoxLog.Dock = DockStyle.Fill;
            listBoxLog.FormattingEnabled = true;
            listBoxLog.ItemHeight = 15;
            listBoxLog.Location = new Point(0, 0);
            listBoxLog.Name = "listBoxLog";
            listBoxLog.Size = new Size(621, 137);
            listBoxLog.TabIndex = 1;
            // 
            // DebugConsoleForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(621, 418);
            Controls.Add(split);
            Controls.Add(panel1);
            Name = "DebugConsoleForm";
            Text = "DebugConsoleForm";
            Load += DebugConsoleForm_Load;
            panel1.ResumeLayout(false);
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button buttonRecord;
        private Button buttonReplay;
        private Panel panel1;
        private SplitContainer split;
        private ListBox listBoxLog;
    }
}