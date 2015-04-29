using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;


namespace PADIMapNoReduce
{
    public class Worker
    {
        static void Main(string[] args)
        {
            string serviceURL = args[0];
            string port = (serviceURL.Split(':')[2].Split('/'))[0];
            Console.WriteLine("PORT = " + port);
            TcpChannel channel = new TcpChannel(int.Parse(port));
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(WorkerServices), "W", WellKnownObjectMode.Singleton);

            // if it's a worker, inform the job tracker about joining the system.
            if (args.Length == 2)
            {
                string entryURL = args[1];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                string workerURL = args[0];
                jobTracker.notify(workerURL);
            }
            //if it's a job tracker, adds itself to the list of workers
            else
            {
                string entryURL = args[0];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                jobTracker.notify(entryURL);
            }
            System.Console.WriteLine("Press <enter> to terminate server...");
            System.Console.ReadLine();
        }
    }

    internal class WorkerServices : MarshalByRefObject, IWorker {
        List<string> workersURLs = new List<string>();
        string inputPath;
        int nSplits;
        string outputPath;
        string className;
         byte[] code;
         public static Thread[] mapThreads;


        //CHECK THIIIIIS
        IClient client = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:10001/C");
        List<int>[] splitsPerWorker;
        
        public void notify(string workerURL)
        {
            workersURLs.Add(workerURL);
        }

        public bool submit(string myInputPath, int numSplits, string myOutputPath, string myClassName, byte[] myCode) {
            inputPath = myInputPath;
            nSplits = numSplits;
            outputPath = myOutputPath;
            className = myClassName;
            code = myCode;
            mapThreads = new Thread[workersURLs.Count];
            //Console.WriteLine("RECEBI NSPLITS = " + nSplits);
            splitsPerWorker = splitFile();
            for (int i = 0; i < workersURLs.Count; i++)
            {
                Console.WriteLine(i + " - " + workersURLs[i]);
                }

            mapThreads[0] = Thread.CurrentThread;
            try
            {
                assignMapTask();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            doMapTask(splitsPerWorker[0], workersURLs[0], inputPath, outputPath, code, className, nSplits);
            return true;
        }

        /* Each worker is given a list of splits, which are distributed in round robin style. */
        public List<int>[] splitFile() {
            int nWorkers = workersURLs.Count;
            List<int>[] splitsPerWorker = new List<int>[nWorkers];

            for (int i = 0; i < nSplits; i++)
            {
                if (splitsPerWorker[i % nWorkers] == null)
                    splitsPerWorker[i % nWorkers] = new List<int>();
                splitsPerWorker[i % nWorkers].Add(i);
                Console.WriteLine("SPLIT " + i + "ASSIGNED TO " + i%nWorkers);
            }

            return splitsPerWorker;
        }

        public string getSplitContent(int split, string inputPath, int nSplits) {
            string content = null;
            
            try {
                content = client.getSplitContent(split, inputPath, nSplits);
            }
            catch (SocketException) {
                System.Console.WriteLine("Could not locate the client");
            }

            return content;
        }

        public void assignMapTask() {
            Console.WriteLine("N WORKERS = " + workersURLs.Count);
            for (int i = 1; i < workersURLs.Count; i++)
            {
                Console.WriteLine("WORKER URL = " + workersURLs[i]);
                try
                {
                    mapThreads[i] = new Thread(() => doTask(workersURLs[i], splitsPerWorker[i]));
                    mapThreads[i].Start();
                    Thread.Sleep(2);
                }
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION CAUGHT " + e.Message);
                }
            }
        }

        public void doTask(string workerURL, List<int> splits)
        {
            IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
            worker.doMapTask(splits, workerURL, inputPath, outputPath, code, className, nSplits);
        }


        public IList<KeyValuePair<string, string>> processSplit(string mySplitContent, byte[] code, string className)
        {
            if (mySplitContent != "")
            {
                Assembly assembly = Assembly.Load(code);
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsClass == true)
                    {
                        if (type.FullName.EndsWith("." + className))
                        {
                            object ClassObj = Activator.CreateInstance(type);
                            object[] args = new object[] { mySplitContent };
                            object resultObject = type.InvokeMember("Map", BindingFlags.Default | BindingFlags.InvokeMethod, null, ClassObj, args);

                            return (IList<KeyValuePair<string, string>>)resultObject;
                        }
                    }
                }
                throw (new System.Exception("could not invoke method"));
            }
            else
            {
                return null;
            }
        }


        public void doMapTask(List<int> splits, string workerURL,string inputPath,string outputPath, byte[] code, string className, int nSplits)
        {
            for (int i = 0; i < splits.Count; i++)
            {
                string mySplitContent = getSplitContent(splits[i], inputPath, nSplits);
                IList<KeyValuePair<string, string>> result = processSplit(mySplitContent, code, className);
                try
                {
                    client.sendResult(result, outputPath, splits[i] + ".out");
                    Console.WriteLine("WORKER " + workerURL + " FINISHED " + splits[i]);
                }
                catch (SocketException)
                {
                    System.Console.WriteLine("Could not locate the client...");
                }
            }
        }

        public void getStatus() { }
        public void slowWorker(int milisseconds, int workerID) {
            Console.WriteLine("ESTOU A FAZER SLEEP NO WORKER" + workerID);
            Console.WriteLine("NO PORTO" + workersURLs[workerID - 1]);
            int workerURLindex = workerID - 1;
            TimeSpan t=  DateTime.Now.TimeOfDay.Add(new TimeSpan(0,0, milisseconds/1000));
            Console.WriteLine("Antes do sleep");
            while (DateTime.Now.TimeOfDay.CompareTo(t) == -1) { Console.Write(".");}
            Console.WriteLine("Depois do slow");    
        }

        public Thread[] getMapThreads() {
            return mapThreads;

        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void freezeWorker() { }
        public void unfreezeWorker() { }
        public void freezeJobTracker() { }
        public void unfreezeJobTracker() { }
    }
}