using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;


namespace PADIMapNoReduce {
    public class Client {
        private static string jobTrackerURL;
        private static string inputPath;
        private static string outputPath;
        private static int nSplits;
        private static string mapperName;
        private static string dllPath;

        static void Main(string[] args) {
            int port = 10001;
            jobTrackerURL = args[1];
            inputPath = args[2];
            outputPath = args[3];
            nSplits = int.Parse(args[4]);
            mapperName = args[5];
            dllPath = args[6];
            
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(ClientServices), "C", WellKnownObjectMode.Singleton);

            submit();
        }

        private static void submit()
        {
            IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
            try
            {
                byte[] code = File.ReadAllBytes(dllPath);
                Console.WriteLine(jobTracker.submit(inputPath, nSplits, outputPath, mapperName, code));
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }
            Console.ReadLine();
        }
    }

    internal class ClientServices : MarshalByRefObject, IClient {
        public string getSplitContent(int splitNumber, string inputPath) {
            using (var sr = new StreamReader(inputPath)) {
                for (int i = 0; i < splitNumber; i++)
                    sr.ReadLine();
                return sr.ReadLine();
            }
        }

        public void sendResult(IList<KeyValuePair<string, string>> result, string outputPath) {
            foreach (KeyValuePair<string, string> p in result) {
                System.IO.File.AppendAllText(outputPath, p.Key + " " + p.Value + Environment.NewLine);
            }
        }
    }
}
