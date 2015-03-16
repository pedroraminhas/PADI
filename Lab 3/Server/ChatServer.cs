using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace RemotingSample
{

    class ChatServer
    {
        static void Main(string[] args)
        {

            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, true);
            String input;
           RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(MyRemoteObject),
                "MyRemoteObjectName",
                WellKnownObjectMode.Singleton);
           MyRemoteObject obj = (MyRemoteObject)Activator.GetObject(
               typeof(MyRemoteObject),
               "tcp://localhost:8086/MyRemoteObjectName");


            System.Console.WriteLine("Comandos:");
            System.Console.WriteLine("1- Registar Cliente");
            System.Console.WriteLine("2- Enviar Mensagem");

            while (true)
            {
                input = System.Console.ReadLine();
                switch (input)
                {
                    case "1":
                        Console.WriteLine("Escreva o nome do Cliente");
                        String clientName = System.Console.ReadLine();
                        obj.AddUser(clientName);
                        break;
                    case "2":
                        Console.WriteLine("Escreva a  mensagem");
                        String message = System.Console.ReadLine();
                        obj.SendMessage(message);
                        obj.existMessage = true;
                        break;
                    default:
                        Console.WriteLine("Opação Inválida");
                        break;
                }
            }
        }
    }
}