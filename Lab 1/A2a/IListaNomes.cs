using System;
using System.Collections;
using System.Text;

namespace A2a
{
    interface IListaNomes
    {
        void adicionar(String nome);
        String imprimir();
        void limpar();
    }

    public class ListaNomes : IListaNomes
    {
        private ArrayList listaNomes = new ArrayList();

        public void adicionar(String nome)
        {
            listaNomes.Add(nome);
        }

        public String imprimir()
        {
            String nomes = null;
            foreach (String nome in listaNomes)
            {
                nomes = nomes + ' ' + nome;
            }
            return nomes;
        }

        public void limpar()
        {
            listaNomes.Clear();
        }
    }
}