using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;


namespace PADIMapNoReduce {

    public class Client
    {
        static void Main(string[] args)
        {
            int port = int.Parse(args[7]);
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(ClientServices), "C", WellKnownObjectMode.Singleton);
            
            string jobTrackerURL = args[1];
            string inputPath = args[2];
            string outputPath = args[3];
            int nSplits = int.Parse(args[4]);
            string mapperName = args[5];
            string dllPath = args[6];
            submit(jobTrackerURL, inputPath, outputPath, nSplits, mapperName, dllPath, port);
        }

        private static void submit(string jobTrackerURL, string inputPath, string outputPath, int nSplits, string mapperName, string dllPath, int port)
        {
            IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
            byte[] code = File.ReadAllBytes(dllPath);
            try
            {
                string[] files = Directory.GetFiles(outputPath, "*.out");
                foreach (string f in files)
                    File.Delete(f);
                jobTracker.submit(inputPath, outputPath, nSplits, mapperName, code, port);
            }
            catch (DirectoryNotFoundException)
            {
                jobTracker.submit(inputPath, outputPath, nSplits, mapperName, code, port);
            }
            catch (SocketException)
            {
                Console.WriteLine("Could not locate server");
            }
            Console.ReadLine();
        }
    }

    internal class ClientServices : MarshalByRefObject, IClient {
        Dictionary<string, int> nFileLines = new Dictionary<string, int>();

        public string getSplitContent(int splitNumber, string inputPath, int nSplits) {            
            int nLines;
            if (nFileLines.ContainsKey(inputPath))
                nLines = nFileLines[inputPath];
            else
            {
                nLines = File.ReadAllLines(@inputPath).Length;
                try
                {
                    nFileLines.Add(inputPath, nLines);
                }
                catch (ArgumentException)
                { /* Sometimes, due to multithreaded processing, a thread tries to add
                   * an entry do nFileLines but other thread in the meanwhile already
                   * did it, so it says 'An item with the same key has already been added.'  */
                }
            }

            int nLinesPerSplit = nLines / nSplits;
            int remainingLines = nLines % nSplits;
            int start = splitNumber * nLinesPerSplit;
            using (var sr = new StreamReader(inputPath))
            {
                int linesToRead;
                string[] linesContent = File.ReadAllLines(@inputPath);
                if (splitNumber == (nSplits - 1))
                    linesToRead = nLinesPerSplit + remainingLines;
                else
                    linesToRead = nLinesPerSplit;
                string[] mySplitContent = new string[linesToRead];
                Array.Copy(linesContent, start, mySplitContent, 0, linesToRead);
                Console.WriteLine("Split " + splitNumber + " has content = " + string.Join(" ", mySplitContent));
                return string.Join(" ", mySplitContent);
            }
        }

        public void sendResult(IList<KeyValuePair<string, string>> result, string outputPath, string splitIdentifier) {
            if (result != null)
            {
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
                foreach (KeyValuePair<string, string> p in result)
                    System.IO.File.AppendAllText(outputPath + splitIdentifier, p.Key + " " + p.Value + Environment.NewLine);
            }
        }
    }
}