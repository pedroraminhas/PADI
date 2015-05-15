using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System;
using System.Net.Sockets;
using System.IO;


namespace PADIMapNoReduce {
    class Client {
        static void Main(string[] args) {
            string mapperName = args[0];
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
            IWorker mt = (IWorker)Activator.GetObject(
                typeof(IWorker),
                "tcp://localhost:10000/Worker");
            try {
                byte[] code = File.ReadAllBytes(args[1]);
                Console.WriteLine(mt.SendMapper(code, mapperName));
            } catch (SocketException) {
                System.Console.WriteLine("Could not locate server");
            }
            Console.ReadLine();
        }
    }
}
