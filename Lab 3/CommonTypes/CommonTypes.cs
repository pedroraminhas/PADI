using System;
using System.Collections;
using System.Collections.Generic;

namespace RemotingSample
{
    public class MyRemoteObject : MarshalByRefObject
    {

        static private ArrayList users = new ArrayList();
        Dictionary<string, string> messages =   new Dictionary<string, string>();



        public void AddUser(String name)
        {
            users.Add(name);
        }

        public void SendMessage(String clientName, String message)
        {

            if (users.Contains(clientName))
            {
                messages.Add(clientName,message);
            }
            else
                Console.WriteLine("Não está autenticado no sistema");
        }

        public int returnChat(int line)
        {
            int i = 0;
            foreach (var key in messages.Keys)
            {
                if (i > line)

                    Console.WriteLine(key, ":", messages[key]);
            }
            return line;
        }
    }
}