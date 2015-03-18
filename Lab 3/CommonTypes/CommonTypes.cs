using System;
using System.Collections;
using System.Collections.Generic;

namespace RemotingSample
{
    public class MyRemoteObject : MarshalByRefObject
    {

        static public ArrayList messages = new ArrayList();
        static public ArrayList senders = new ArrayList();
        static private ArrayList users = new ArrayList();

      //  Dictionary<string, string> messages =   new Dictionary<string, string>();



        public void AddUser(String name)
        {
            users.Add(name);
        }

        public void SendMessage(String clientName, String message)
        {

            if (users.Contains(clientName))
            {
                messages.Add(message);
                senders.Add(clientName);
            }
            else
                Console.WriteLine("Não está autenticado no sistema");
        }

        public ArrayList getMessages() {
            return messages;
        }
        public ArrayList getSenders()
        {
            return senders;
        }
    }
}