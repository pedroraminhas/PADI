﻿using System.Runtime.Remoting.Channels.Tcp;
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
            byte[] code = File.ReadAllBytes(dllPath);
            try
            {
                string[] files = Directory.GetFiles(outputPath, "*.out");
                foreach (string f in files)
                {
                    File.Delete(f);
                }
                Console.WriteLine(jobTracker.submit(inputPath, nSplits, outputPath, mapperName, code));
            }
            catch (SocketException)
            {
                Console.WriteLine("Could not locate server");
            }
            catch (DirectoryNotFoundException) {
                Console.WriteLine(jobTracker.submit(inputPath, nSplits, outputPath, mapperName, code));
            }
            Console.ReadLine();
        }
    }

    internal class ClientServices : MarshalByRefObject, IClient {
        Dictionary<string, int> nFileLines = new Dictionary<string, int>();

        public string getSplitContent(int splitNumber, string inputPath, int nSplits) {
            Console.WriteLine("CHEGUEI E QUERO O SPLIT " + splitNumber);
            
                int nLines;
                int linesToRead;

                if (nFileLines.ContainsKey(inputPath))
                {
                    nLines = nFileLines[inputPath];
                }
                else
                {
                    nLines = File.ReadAllLines(@inputPath).Length;
                    nFileLines.Add(inputPath, nLines);
                }

               /* int nLinesPerSplit;
                if (nLines % nSplits == 0)
                {
                    nLinesPerSplit = nLines / nSplits;
                }
                else
                {
                    nLinesPerSplit = nLines / nSplits + 1;
                }*/

                int nLinesPerSplit = nLines / nSplits;
                int remainingLines = nLines % nSplits;

                int start = splitNumber * nLinesPerSplit;
                Console.WriteLine("START = " + start);
                //int last = splitNumber * nLinesPerSplit + nLinesPerSplit - 1;

                using (var sr = new StreamReader(inputPath))
                {
                    string[] linesContent = File.ReadAllLines(@inputPath);

                    if (splitNumber == (nSplits - 1))
                    {
                        linesToRead = nLinesPerSplit + remainingLines;
                    }
                    else
                    {
                        linesToRead = nLinesPerSplit;
                    }

                   /* if (start + nLinesPerSplit > linesContent.Length)
                    {
                        linesToRead = linesContent.Length - start;
                    }
                    else
                    {
                        linesToRead = nLinesPerSplit;
                    }*/

                    Console.WriteLine("Linhas que ira ler " + linesToRead + "o split " + splitNumber);
                    Console.WriteLine("NSPLITS " + nSplits);

                    if (linesToRead != 0)
                    {
                        string[] mySplitContent = new string[linesToRead];
                        Array.Copy(linesContent, start, mySplitContent, 0, linesToRead);
                        Console.WriteLine("MY SPLIT = " + string.Join(" ", mySplitContent));
                        return string.Join(" ", mySplitContent);
                    }
                    else
                    {
                        return "";
                    }
                }
                
        }

        public void sendResult(IList<KeyValuePair<string, string>> result, string outputPath, string splitIdentifier) {
            if (result != null)
            {
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                foreach (KeyValuePair<string, string> p in result)
                {
                    System.IO.File.AppendAllText(outputPath + splitIdentifier, p.Key + " " + p.Value + Environment.NewLine);
                }
            }
        }
    }
}
