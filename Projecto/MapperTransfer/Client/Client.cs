using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;


namespace PADIMapNoReduce {
    class Client {
        static void Main(string[] args) {
            string mapperName = args[0];
            string inputPath = @"..\..\..\Files\test.txt";
            string outputPath = @"..\..\..\Results\";
            int nSplits = getNumberOfSplits(inputPath);

            int port = 10001;
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(ReturnContentServices), "C", WellKnownObjectMode.Singleton);
            
            IWorker mt = (IWorker)Activator.GetObject(typeof(IWorker), "tcp://localhost:30001/W");
            try {
                byte[] code = File.ReadAllBytes(args[1]);
                Console.WriteLine(mt.Submit(inputPath, nSplits, outputPath, mapperName, code));
            } catch (SocketException) {
                System.Console.WriteLine("Could not locate server");
            }
            Console.ReadLine();
        }

        public static int getNumberOfSplits(String inputPath)
        {
            var lineCount = File.ReadAllLines(inputPath).Length;
            return lineCount;
        }
    }


    internal class ReturnContentServices : MarshalByRefObject, IClient
    {
        public string getContent(List<int> splits, string inputPath)
        {
            string content = "";

            for (int line = 0; line < splits.Count; line++)
            {
                using (var sr = new StreamReader(inputPath))
                {
                    for (int i = 1; i < line; i++)
                        sr.ReadLine();
                    content = content + sr.ReadLine();
                    if (line != splits.Count - 1)
                    {
                        content += " ";
                    }
                }
            }
            return content;
        }
    }


}
