using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace AsynchCalcPi {
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class Form1 : System.Windows.Forms.Form {
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button _calcButton;
    private System.Windows.Forms.NumericUpDown _digits;
    private System.Windows.Forms.TextBox _pi;
    private System.Windows.Forms.ProgressBar _piProgress;
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.Container components = null;

    public Form1() {
        InitializeComponent();
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose( bool disposing ) {
        if( disposing ) {
        if (components != null) {
            components.Dispose();
        }
        }
        base.Dispose( disposing );
    }

		#region Windows Form Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            this.panel1 = new System.Windows.Forms.Panel();
            this._calcButton = new System.Windows.Forms.Button();
            this._digits = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this._pi = new System.Windows.Forms.TextBox();
            this._piProgress = new System.Windows.Forms.ProgressBar();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._digits)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this._calcButton);
            this.panel1.Controls.Add(this._digits);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(232, 40);
            this.panel1.TabIndex = 0;
            // 
            // _calcButton
            // 
            this._calcButton.Location = new System.Drawing.Point(144, 8);
            this._calcButton.Name = "_calcButton";
            this._calcButton.Size = new System.Drawing.Size(75, 23);
            this._calcButton.TabIndex = 2;
            this._calcButton.Text = "Calc";
            this._calcButton.Click += new System.EventHandler(this._calcButton_Click);
            // 
            // _digits
            // 
            this._digits.Location = new System.Drawing.Point(80, 8);
            this._digits.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this._digits.Name = "_digits";
            this._digits.Size = new System.Drawing.Size(56, 20);
            this._digits.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Digits of Pi";
            // 
            // _pi
            // 
            this._pi.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pi.Location = new System.Drawing.Point(0, 40);
            this._pi.Multiline = true;
            this._pi.Name = "_pi";
            this._pi.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._pi.Size = new System.Drawing.Size(232, 103);
            this._pi.TabIndex = 1;
            this._pi.Text = "3";
            this._pi.TextChanged += new System.EventHandler(this._pi_TextChanged);
            // 
            // _piProgress
            // 
            this._piProgress.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._piProgress.Location = new System.Drawing.Point(0, 143);
            this._piProgress.Maximum = 1;
            this._piProgress.Name = "_piProgress";
            this._piProgress.Size = new System.Drawing.Size(232, 23);
            this._piProgress.TabIndex = 2;
            // 
            // Form1
            // 
            this.AcceptButton = this._calcButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(232, 166);
            this.Controls.Add(this._pi);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._piProgress);
            this.Name = "Form1";
            this.Text = "Digits of Pi";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._digits)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }
		#endregion

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main() {
        Application.Run(new Form1());
    }

    enum CalcState {
        Pending,
        Calculating,
        Canceled,
    }
    CalcState _state = CalcState.Pending;

    class ShowProgressArgs : EventArgs
    {
        public string Pi;
        public int TotalDigits;
        public int DigitsSoFar;
        public bool Cancel;
        public ShowProgressArgs(
        string pi, int totalDigits, int digitsSoFar)
        {
            this.Pi = pi;
            this.TotalDigits = totalDigits;
            this.DigitsSoFar = digitsSoFar;
        }
    }
    delegate void ShowProgressHandler(object sender, ShowProgressArgs e);

    // to pass arguments correctly
    delegate void ShowProgressDelegate(string pi, int totalDigits, int digitsSoFar, out bool _state);

    void ShowProgress(string pi, int totalDigits, int digitsSoFar, out bool cancel)
    {
        // Make sure we're on the right thread
        if (_pi.InvokeRequired == false) {
            _pi.Text = pi;
            _piProgress.Maximum = totalDigits;
            _piProgress.Value = digitsSoFar;

            // Check for Cancel
            cancel = (_state == CalcState.Canceled);
            // Check for completion
            if (cancel || (digitsSoFar == totalDigits))
            {
                _state = CalcState.Pending;
                _calcButton.Text = "Calc";
                _calcButton.Enabled = true;
            }
        }
        else {
            ShowProgressHandler showProgress = new ShowProgressHandler(AsynchCalcPiForm_ShowProgress);
            object sender = System.Threading.Thread.CurrentThread;
            ShowProgressArgs
            e = new ShowProgressArgs(pi, totalDigits, digitsSoFar);
            Invoke(showProgress, new object[] { sender, e });
            cancel = e.Cancel;
        }
    }

    void AsynchCalcPiForm_ShowProgress(object sender, ShowProgressArgs e) {
        ShowProgress(e.Pi, e.TotalDigits, e.DigitsSoFar, out e.Cancel);
    }

    void CalcPi(int digits) {
        bool cancel = false;
        StringBuilder pi = new StringBuilder("3", digits + 2);
        
        // Show progress
        ShowProgress(pi.ToString(), digits, 0, out cancel);

        if( digits > 0 ) {
            pi.Append(".");

            for( int i = 0; i < digits; i += 9 ) {
                int nineDigits = NineDigitsOfPi.StartingAt(i+1);
                int digitCount = Math.Min(digits - i, 9);
                string ds = string.Format("{0:D9}", nineDigits);
                pi.Append(ds.Substring(0, digitCount));

                // Show progress
                ShowProgress(pi.ToString(), digits, i + digitCount, out cancel);
                if (cancel) break;
            }
        }
    }

    delegate void CalcPiDelegate(int digits);

    private void _calcButton_Click(object sender, System.EventArgs e) {
        // Calc button does double duty as Cancel button
        switch (_state)
        {
            // Start a new calculation
            case CalcState.Pending:
                // Allow canceling
                _state = CalcState.Calculating;
                _calcButton.Text = "Cancel";
                // Asynch delegate method
                CalcPiDelegate calcPi = new CalcPiDelegate(CalcPi);
                calcPi.BeginInvoke((int)_digits.Value, null, null);
                break;

            // Cancel a running calculation
            case CalcState.Calculating:
                _state = CalcState.Canceled;
                _calcButton.Enabled = false;
                break;
            // Shouldn't be able to press Calc button while it's canceling
            case CalcState.Canceled:
                Debug.Assert(false);
                break;
        }
    }

    private void _pi_TextChanged(object sender, EventArgs e) {}
    }
}














