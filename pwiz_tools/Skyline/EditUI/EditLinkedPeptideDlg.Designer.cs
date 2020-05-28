﻿namespace pwiz.Skyline.EditUI
{
    partial class EditLinkedPeptideDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditLinkedPeptideDlg));
            this.label1 = new System.Windows.Forms.Label();
            this.tbxPeptideSequence = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbxAttachmentOrdinal = new System.Windows.Forms.TextBox();
            this.btnEditModifications = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tbxPeptideSequence
            // 
            resources.ApplyResources(this.tbxPeptideSequence, "tbxPeptideSequence");
            this.tbxPeptideSequence.Name = "tbxPeptideSequence";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // tbxAttachmentOrdinal
            // 
            resources.ApplyResources(this.tbxAttachmentOrdinal, "tbxAttachmentOrdinal");
            this.tbxAttachmentOrdinal.Name = "tbxAttachmentOrdinal";
            // 
            // btnEditModifications
            // 
            resources.ApplyResources(this.btnEditModifications, "btnEditModifications");
            this.btnEditModifications.Name = "btnEditModifications";
            this.btnEditModifications.UseVisualStyleBackColor = true;
            this.btnEditModifications.Click += new System.EventHandler(this.btnEditModifications_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // EditLinkedPeptideDlg
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnEditModifications);
            this.Controls.Add(this.tbxAttachmentOrdinal);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbxPeptideSequence);
            this.Controls.Add(this.label1);
            this.Name = "EditLinkedPeptideDlg";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxPeptideSequence;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbxAttachmentOrdinal;
        private System.Windows.Forms.Button btnEditModifications;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}