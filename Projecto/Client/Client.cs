using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

namespace PADIMapNoReduce
{

    class Client
    {

        static MyRemoteJobTracker jobTracker;

        static private MyRemoteJobTracker Init(String EntryURL)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            MyRemoteJobTracker obj = (MyRemoteJobTracker)Activator.GetObject(
                typeof(MyRemoteJobTracker),
                "tcp://" + EntryURL);
            return obj;
        }

        static void Submit(String inputFilePath, int nSplits, String outputPath)
        {
            jobTracker.Submit(inputFilePath, nSplits, outputPath);
        }


        static void Main()
        {
            String EntryURL = "localhost:8086/MyRemoteObjectName";
            jobTracker = Init(EntryURL);

            try
            {
                String inputPath = @"C:\Users\Ana\Desktop\test.txt";
                int nSplits = 5;
                String outputPath = "output";
                Submit(inputPath, nSplits, outputPath);
                System.Console.WriteLine("wazap?");

            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Console.ReadLine();
        }
    }
}