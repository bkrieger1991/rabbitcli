
namespace RabbitMQ.Windows.UI.Forms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.foreverTabPage1 = new ReaLTaiizor.Controls.ForeverTabPage();
            this._tabConfig = new System.Windows.Forms.TabPage();
            this._configCombobox = new ReaLTaiizor.Controls.CrownComboBox();
            this.foxLabel2 = new ReaLTaiizor.Controls.FoxLabel();
            this.foxLabel1 = new ReaLTaiizor.Controls.FoxLabel();
            this.spaceBorderLabel1 = new ReaLTaiizor.Controls.SpaceBorderLabel();
            this._tabLogging = new System.Windows.Forms.TabPage();
            this.gridLog1 = new Serilog.Sinks.WinForms.GridLog();
            this.foreverTabPage1.SuspendLayout();
            this._tabConfig.SuspendLayout();
            this._tabLogging.SuspendLayout();
            this.SuspendLayout();
            // 
            // foreverTabPage1
            // 
            this.foreverTabPage1.ActiveColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(168)))), ((int)(((byte)(109)))));
            this.foreverTabPage1.ActiveFontColor = System.Drawing.Color.White;
            this.foreverTabPage1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.foreverTabPage1.BaseColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(47)))), ((int)(((byte)(49)))));
            this.foreverTabPage1.BGColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(70)))), ((int)(((byte)(73)))));
            this.foreverTabPage1.Controls.Add(this._tabConfig);
            this.foreverTabPage1.Controls.Add(this._tabLogging);
            this.foreverTabPage1.DeactiveFontColor = System.Drawing.Color.White;
            this.foreverTabPage1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.foreverTabPage1.ItemSize = new System.Drawing.Size(120, 40);
            this.foreverTabPage1.Location = new System.Drawing.Point(0, 0);
            this.foreverTabPage1.Name = "foreverTabPage1";
            this.foreverTabPage1.SelectedIndex = 0;
            this.foreverTabPage1.Size = new System.Drawing.Size(1117, 600);
            this.foreverTabPage1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.foreverTabPage1.TabIndex = 0;
            // 
            // _tabConfig
            // 
            this._tabConfig.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(70)))), ((int)(((byte)(73)))));
            this._tabConfig.Controls.Add(this._configCombobox);
            this._tabConfig.Controls.Add(this.foxLabel2);
            this._tabConfig.Controls.Add(this.foxLabel1);
            this._tabConfig.Controls.Add(this.spaceBorderLabel1);
            this._tabConfig.Location = new System.Drawing.Point(4, 44);
            this._tabConfig.Name = "_tabConfig";
            this._tabConfig.Padding = new System.Windows.Forms.Padding(3);
            this._tabConfig.Size = new System.Drawing.Size(1109, 552);
            this._tabConfig.TabIndex = 0;
            this._tabConfig.Text = "default";
            // 
            // _configCombobox
            // 
            this._configCombobox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this._configCombobox.FormattingEnabled = true;
            this._configCombobox.Location = new System.Drawing.Point(640, 15);
            this._configCombobox.Name = "_configCombobox";
            this._configCombobox.Size = new System.Drawing.Size(163, 26);
            this._configCombobox.TabIndex = 4;
            this._configCombobox.SelectedIndexChanged += new System.EventHandler(this._configCombobox_SelectedIndexChanged);
            // 
            // foxLabel2
            // 
            this.foxLabel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.foxLabel2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.foxLabel2.ForeColor = System.Drawing.Color.White;
            this.foxLabel2.Location = new System.Drawing.Point(499, 19);
            this.foxLabel2.Name = "foxLabel2";
            this.foxLabel2.Size = new System.Drawing.Size(135, 19);
            this.foxLabel2.TabIndex = 3;
            this.foxLabel2.Text = "Select configuration:";
            // 
            // foxLabel1
            // 
            this.foxLabel1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.foxLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.foxLabel1.Location = new System.Drawing.Point(187, 19);
            this.foxLabel1.Name = "foxLabel1";
            this.foxLabel1.Size = new System.Drawing.Size(144, 19);
            this.foxLabel1.TabIndex = 1;
            this.foxLabel1.Text = "default";
            // 
            // spaceBorderLabel1
            // 
            this.spaceBorderLabel1.Customization = "Kioq/yoqKv/+/v7/IyMj/yoqKv8=";
            this.spaceBorderLabel1.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.spaceBorderLabel1.Image = null;
            this.spaceBorderLabel1.Location = new System.Drawing.Point(8, 13);
            this.spaceBorderLabel1.Name = "spaceBorderLabel1";
            this.spaceBorderLabel1.NoRounding = false;
            this.spaceBorderLabel1.Size = new System.Drawing.Size(330, 31);
            this.spaceBorderLabel1.TabIndex = 0;
            this.spaceBorderLabel1.Text = " Currently using configuration: ";
            this.spaceBorderLabel1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Left;
            this.spaceBorderLabel1.Transparent = false;
            // 
            // _tabLogging
            // 
            this._tabLogging.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(70)))), ((int)(((byte)(73)))));
            this._tabLogging.Controls.Add(this.gridLog1);
            this._tabLogging.Location = new System.Drawing.Point(4, 44);
            this._tabLogging.Name = "_tabLogging";
            this._tabLogging.Padding = new System.Windows.Forms.Padding(3);
            this._tabLogging.Size = new System.Drawing.Size(1109, 552);
            this._tabLogging.TabIndex = 1;
            this._tabLogging.Text = "Logs";
            // 
            // gridLog1
            // 
            this.gridLog1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridLog1.Location = new System.Drawing.Point(4, 3);
            this.gridLog1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gridLog1.Name = "gridLog1";
            this.gridLog1.Size = new System.Drawing.Size(898, 387);
            this.gridLog1.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(819, 600);
            this.Controls.Add(this.foreverTabPage1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1920, 1040);
            this.MinimumSize = new System.Drawing.Size(598, 65);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "formTheme1";
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.foreverTabPage1.ResumeLayout(false);
            this._tabConfig.ResumeLayout(false);
            this._tabLogging.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private ReaLTaiizor.Controls.ForeverTabPage foreverTabPage1;
        private System.Windows.Forms.TabPage _tabConfig;
        private System.Windows.Forms.TabPage _tabLogging;
        private Serilog.Sinks.WinForms.GridLog gridLog1;
        private ReaLTaiizor.Controls.SpaceBorderLabel spaceBorderLabel1;
        private ReaLTaiizor.Controls.FoxLabel foxLabel2;
        private ReaLTaiizor.Controls.FoxLabel foxLabel1;
        private ReaLTaiizor.Controls.CrownComboBox _configCombobox;
    }
}

