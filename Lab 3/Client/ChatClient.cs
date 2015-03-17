using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Threading;

namespace RemotingSample{

    class Client
    {
        String clientName;
        static int lineViewed;




        static void Main()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

           MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
                typeof(MyRemoteObject),
                "tcp://localhost:8086/MyRemoteObjectName");
            Console.WriteLine("Escreva o seu nome de utilizador");
            String clientName = System.Console.ReadLine();

            ThreadStart ts = new ThreadStart(returnChat);
            Thread t = new Thread(ts);
            t.Start();

            while (true)
            {

                try
                {
                    Console.WriteLine("Escreva a mensagem que pretende enviar");
                    String message = System.Console.ReadLine();
                    obj.SendMessage(clientName, message);
                    lineViewed++;

                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate server");
                }

                Console.ReadLine();
            }
        }
        public static void returnChat()
        {
            MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
                  typeof(MyRemoteObject),
                  "tcp://localhost:8086/MyRemoteObjectName");
            while (true)
            {
                Console.WriteLine(obj.returnChat(lineViewed));
                lineViewed = obj.returnChat(lineViewed);
            }
        }
    }
}