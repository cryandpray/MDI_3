﻿namespace MDI_3
{
    partial class FormDoc
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.SuspendLayout();
            // 
            // FormDoc
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.DoubleBuffered = true;
            this.Name = "FormDoc";
            this.Text = "FormDoc";
            this.Load += new System.EventHandler(this.FormDoc_Load);
            this.MdiChildActivate += new System.EventHandler(this.FormDoc_MdiChildActivate);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FormDoc_MouseDown);
            this.MouseEnter += new System.EventHandler(this.FormDocument_MouseEnter);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FormDoc_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FormDoc_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}