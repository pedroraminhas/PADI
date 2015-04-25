using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PADIMapNoReduce
{
    public partial class GUI : Form
    {
        private string inputPath;
        private string instruction;
        //int workerID;
        public GUI()
        {
            InitializeComponent();
        }

        // INSTRUCTION SUBMIT
        private void button1_Click(object sender, EventArgs e)
        {
            PuppetMaster.splitInstruction(instruction);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

            //string input = loadBox.Text.ToString();
            //workerID = Convert.ToInt32(input);
        }
        // INSTRUCTION TEXTBOX
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            instruction = textbox_instruction.Text.ToString();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //Recebe o PuppetMasterURL
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            //Recebe o EntryURL
        }

        private void GUI_Load(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        // SCRIPT BROWSE
        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                inputPath = openFileDialog1.FileName;
                loadBox.Text = inputPath;
                loadBox.Update();
            }
        }

        // SCRIPT SUBMIT
        private void submitButton_Click(object sender, EventArgs e)
        {
            PuppetMaster.parseScript(inputPath);
        }
    }
}
