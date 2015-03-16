using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;

namespace RemotingSample{

    class Client
    {


        static void Main()
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
                typeof(MyRemoteObject),
                "tcp://localhost:8086/MyRemoteObjectName");
            while (true)
            {

                try
                {
                    Console.WriteLine("Escreva a mensagem que pretende enviar");
                    Console.WriteLine(obj.SendMessage(System.Console.ReadLine()));
                    obj.existMessage = false;

                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate server");
                }

                Console.ReadLine();
            }
        }
    }
}