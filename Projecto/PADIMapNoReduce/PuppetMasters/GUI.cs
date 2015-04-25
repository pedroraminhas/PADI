using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace PADIMapNoReduce
{
    public partial class GUI : Form
    {
        private string inputPath;
        private string instruction;
        private string nextInstruction;
        private static ArrayList instructions;
        private int index;
        private String errorNoScript = "Please load a script first!";
        private String errorNoInstruction = "Please insert an instruction first!";
        private static String errorNoPath = "Please choose a script first!";

        public GUI()
        {
            InitializeComponent();
        }

        private void GUI_Load(object sender, EventArgs e) { }
        
        private void button_browse_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                inputPath = openFileDialog.FileName;
                textBox_loadScript.Text = inputPath;
                textBox_loadScript.Update();
            }
        }

        public static void parseScript(string inputPath)
        {
            string line;
                System.IO.StreamReader file = new System.IO.StreamReader(inputPath);
                instructions = new ArrayList();
                while ((line = file.ReadLine()) != null)
                {
                    if (!line[0].Equals('%'))
                        instructions.Add(line);
                }
        }

        private void button_submit_Click(object sender, EventArgs e)
        {
            try{
                parseScript(inputPath);
                nextInstruction = (String)instructions[0];
                textBox_nextInstruction.Text = nextInstruction;
                index = 0;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(errorNoPath);
            }
        }

        private void button_runAll_Click(object sender, EventArgs e)
        {
            try
            {
                for (; index < instructions.Count; index++)
                {
                    PuppetMaster.splitInstruction((String)instructions[index]);
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(errorNoScript);
            }
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            try
            {
                if (index < instructions.Count)
                {
                    PuppetMaster.splitInstruction(nextInstruction);
                    index++;
                }
                if (index < instructions.Count)
                {
                    nextInstruction = (String)instructions[index];
                    textBox_nextInstruction.Text.Remove(0);
                    textBox_nextInstruction.Text = nextInstruction;
                }
            }
            catch (NullReferenceException) {
                MessageBox.Show(errorNoScript);
            }
        }

        private void textBox_nextInstruction_TextChanged(object sender, EventArgs e)
        {
            this.textBox_nextInstruction.Text = nextInstruction;
        }

        private void button_instructionSubmit_Click(object sender, EventArgs e)
        {
            try{
                if (!instruction.Equals("")){
                    instruction = textBox_instruction.Text.ToString();
                    PuppetMaster.splitInstruction(instruction);
                }
            }
            catch(NullReferenceException)
            {
                MessageBox.Show(errorNoInstruction);
            }
        }

    }
}
