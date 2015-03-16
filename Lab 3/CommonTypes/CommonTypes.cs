using System;
using System.Collections;

namespace RemotingSample
{
	public class MyRemoteObject : MarshalByRefObject  {

        static private ArrayList users = new ArrayList();
        public Boolean existMessage = false;
        public String messagePending;

        public void AddUser(String name)
        {
            users.Add(name);
        }
    public string SendMessage(String message) {
        System.Console.Write(message);
        messagePending = message;
        
         return message;
		}
  }
}