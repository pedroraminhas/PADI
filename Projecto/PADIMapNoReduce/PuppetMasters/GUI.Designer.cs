namespace PADIMapNoReduce
{
    partial class GUI
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
            this.label1 = new System.Windows.Forms.Label();
            this.button_runAll = new System.Windows.Forms.Button();
            this.button_next = new System.Windows.Forms.Button();
            this.textBox_nextInstruction = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.button_submit = new System.Windows.Forms.Button();
            this.button_browse = new System.Windows.Forms.Button();
            this.button_instructionSubmit = new System.Windows.Forms.Button();
            this.textBox_loadScript = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_instruction = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Load a script";
            // 
            // button_runAll
            // 
            this.button_runAll.Location = new System.Drawing.Point(215, 160);
            this.button_runAll.Name = "button_runAll";
            this.button_runAll.Size = new System.Drawing.Size(75, 23);
            this.button_runAll.TabIndex = 12;
            this.button_runAll.Text = "Run all";
            this.button_runAll.UseVisualStyleBackColor = true;
            this.button_runAll.Click += new System.EventHandler(this.button_runAll_Click);
            // 
            // button_next
            // 
            this.button_next.Location = new System.Drawing.Point(134, 160);
            this.button_next.Name = "button_next";
            this.button_next.Size = new System.Drawing.Size(75, 23);
            this.button_next.TabIndex = 14;
            this.button_next.Text = "Next";
            this.button_next.UseVisualStyleBackColor = true;
            this.button_next.Click += new System.EventHandler(this.button_next_Click);
            // 
            // textBox_nextInstruction
            // 
            this.textBox_nextInstruction.Location = new System.Drawing.Point(134, 134);
            this.textBox_nextInstruction.Name = "textBox_nextInstruction";
            this.textBox_nextInstruction.Size = new System.Drawing.Size(635, 20);
            this.textBox_nextInstruction.TabIndex = 15;
            this.textBox_nextInstruction.TextChanged += new System.EventHandler(this.textBox_nextInstruction_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(28, 137);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Next instruction";
            // 
            // button_submit
            // 
            this.button_submit.Location = new System.Drawing.Point(580, 59);
            this.button_submit.Name = "button_submit";
            this.button_submit.Size = new System.Drawing.Size(75, 23);
            this.button_submit.TabIndex = 17;
            this.button_submit.Text = "Submit";
            this.button_submit.UseVisualStyleBackColor = true;
            this.button_submit.Click += new System.EventHandler(this.button_submit_Click);
            // 
            // button_browse
            // 
            this.button_browse.Location = new System.Drawing.Point(499, 59);
            this.button_browse.Name = "button_browse";
            this.button_browse.Size = new System.Drawing.Size(75, 23);
            this.button_browse.TabIndex = 18;
            this.button_browse.Text = "Browse";
            this.button_browse.UseVisualStyleBackColor = true;
            this.button_browse.Click += new System.EventHandler(this.button_browse_Click);
            // 
            // button_instructionSubmit
            // 
            this.button_instructionSubmit.Location = new System.Drawing.Point(499, 253);
            this.button_instructionSubmit.Name = "button_instructionSubmit";
            this.button_instructionSubmit.Size = new System.Drawing.Size(75, 23);
            this.button_instructionSubmit.TabIndex = 20;
            this.button_instructionSubmit.Text = "Submit";
            this.button_instructionSubmit.UseVisualStyleBackColor = true;
            this.button_instructionSubmit.Click += new System.EventHandler(this.button_instructionSubmit_Click);
            // 
            // textBox_loadScript
            // 
            this.textBox_loadScript.Location = new System.Drawing.Point(134, 61);
            this.textBox_loadScript.Name = "textBox_loadScript";
            this.textBox_loadScript.Size = new System.Drawing.Size(342, 20);
            this.textBox_loadScript.TabIndex = 21;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 258);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "Instruction";
            // 
            // textBox_instruction
            // 
            this.textBox_instruction.Location = new System.Drawing.Point(134, 255);
            this.textBox_instruction.Name = "textBox_instruction";
            this.textBox_instruction.Size = new System.Drawing.Size(342, 20);
            this.textBox_instruction.TabIndex = 23;
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "script";
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 308);
            this.Controls.Add(this.textBox_instruction);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_loadScript);
            this.Controls.Add(this.button_instructionSubmit);
            this.Controls.Add(this.button_browse);
            this.Controls.Add(this.button_submit);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_nextInstruction);
            this.Controls.Add(this.button_next);
            this.Controls.Add(this.button_runAll);
            this.Controls.Add(this.label1);
            this.Name = "GUI";
            this.Text = "GUI";
            this.Load += new System.EventHandler(this.GUI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_runAll;
        private System.Windows.Forms.Button button_next;
        private System.Windows.Forms.TextBox textBox_nextInstruction;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button_submit;
        private System.Windows.Forms.Button button_browse;
        private System.Windows.Forms.Button button_instructionSubmit;
        private System.Windows.Forms.TextBox textBox_loadScript;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_instruction;
        private System.Windows.Forms.OpenFileDialog openFileDialog;

    }
}