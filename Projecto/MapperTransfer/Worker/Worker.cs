using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace PADIMapNoReduce
{
    /// <summary>
    /// Program class is a container for application entry point Main
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point Main
        /// </summary>
        /// <param name="args">No required arguments</param>
        static void Main(string[] args)
        {
            int port = 30001;
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(WorkerServices), "W", WellKnownObjectMode.Singleton);
            System.Console.WriteLine("Press <enter> to terminate server...");
            System.Console.ReadLine();
        }
    }

    internal class WorkerServices : MarshalByRefObject, IWorker
    {
        int nWorkers = 1;
        public bool Submit(string inputPath, int nSplits, string outputPath, string className, byte[] code)
        {
            List<int>[] splitsPerWorker = splitFile(nSplits);
            //sendLimitsToToWorkers(workersLimits);
            string mySplitContent = getMyShare(splitsPerWorker, inputPath);
            Console.WriteLine(mySplitContent);
            IList<KeyValuePair<string, string>> result = doMyMapTask(className, code, mySplitContent);
            //sendResult(result, outputPath);
            Console.WriteLine("Map call result was: ");
            foreach (KeyValuePair<string, string> p in result)
            {
                Console.WriteLine("key: " + p.Key + ", value: " + p.Value);
            }
            return true;
        }

        public List<int>[] splitFile(int nSplits) {
            List<int>[] splitsPerWorker = new List<int>[nWorkers];

            for (int i = 0; i < nSplits; i++)
            {
                if (splitsPerWorker[i % nWorkers] == null)
                    splitsPerWorker[i % nWorkers] = new List<int>();
                splitsPerWorker[i % nWorkers].Add(i);
            }

            return splitsPerWorker;
        }

        public string getMyShare(List<int>[] splitsPerWorker, string inputPath)
        {
            IClient rcs = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:10001/C");
            string content = null;

            try
            {
                content = rcs.getContent(splitsPerWorker[0], inputPath);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            return content;
        }

        public IList<KeyValuePair<string, string>> doMyMapTask(String className, byte[] code, String mySplitContent)
        {
            Assembly assembly = Assembly.Load(code);
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + className))
                    {
                        object ClassObj = Activator.CreateInstance(type);
                        object[] args = new object[] {mySplitContent};
                        object resultObject = type.InvokeMember("Map", BindingFlags.Default | BindingFlags.InvokeMethod, null, ClassObj, args);

                        return (IList<KeyValuePair<string, string>>)resultObject;
                    }
                }
            }
            throw (new System.Exception("could not invoke method"));
        }



    }
}