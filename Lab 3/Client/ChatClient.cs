using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Threading;

namespace RemotingSample{

    class ChatClient
    {
        String clientName;
        static int lineViewed=0;




        static void Main()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

           MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
                typeof(MyRemoteObject),
                "tcp://localhost:8086/MyRemoteObjectName");
            Console.WriteLine("Escreva o seu nome de utilizador");

            ThreadStart ts = new ThreadStart(returnChat);
            Thread t = new Thread(ts);
            t.Start();
            while (Console.KeyAvailable == false)
                Thread.Sleep(1000);
            String clientName = System.Console.ReadLine();


            while (true)
            {
           

                try
                {
                    Console.WriteLine("Escreva a mensagem que pretende enviar");
                    while (Console.KeyAvailable == false)
                        Thread.Sleep(1000);
                    String message = System.Console.ReadLine();
                    obj.SendMessage(clientName, message);
                    lineViewed++;

                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate server");
                }

            }
        }
        public static void returnChat()
        {
            MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
                  typeof(MyRemoteObject),
                  "tcp://localhost:8086/MyRemoteObjectName");
            while (true)
            {
                if (obj.getMessages().Count > lineViewed)
                {
                    for (int i = lineViewed; i < obj.getMessages().Count; i++)
                    {
                        System.Console.WriteLine(obj.getSenders()[i]);
                        System.Console.WriteLine(":");
                        System.Console.WriteLine(obj.getMessages()[i]);
                    }

                    lineViewed = obj.getMessages().Count;
                }
            }
        }
    }
}