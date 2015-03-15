using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace A2a
{
    public partial class Form1 : Form
    {
        private IListaNomes lista;

        public Form1()
        {
            InitializeComponent();
            this.lista = new ListaNomes();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void adicionarButton_Click(object sender, EventArgs e)
        {
            lista.adicionar(textBox1.Text.ToString());
            textBox1.Clear();
        }

        private void imprimirButton_Click(object sender, EventArgs e)
        {
            label1.Text = lista.imprimir();
        }

        private void eliminarButton_Click(object sender, EventArgs e)
        {
            lista.limpar();
            label1.Text = null;
        }
    }
}
