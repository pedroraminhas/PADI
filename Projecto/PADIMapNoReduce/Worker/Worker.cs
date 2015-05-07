using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;


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
            if (args.Length == 3)
            {
                string entryURL = args[1];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                string workerURL = args[0];
                string jobTrackerURL = args[2];
                jobTracker.notify(workerURL, jobTrackerURL);
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                worker.setURLs(workerURL, jobTrackerURL);
            }
            //if it's a job tracker, adds itself to the list of workers
            else
            {
                string entryURL = args[0];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                jobTracker.notify(entryURL, entryURL);
            }
            System.Console.WriteLine("Press <enter> to terminate server...");
            System.Console.ReadLine();
        }
    }

    internal class WorkerServices : MarshalByRefObject, IWorker {
        private string myURL;
        private string jobTrackerURL;
        private string WORKER_IDLE = "IDLE";
        private string WORKER_UNAVAILABLE = "UNAVAILABLE";
        private bool isFrozen = false;
        Thread myThread;
        List<string> workersURLs = new List<string>();
        Dictionary<string, string> workersStatus = new Dictionary<string, string>();
        string inputPath;
        int nSplits;
        string outputPath;
        string className;
        byte[] code;
        
        public void notify(string workerURL, string jobTrackerURL)
        {
            workersURLs.Add(workerURL);
            workersStatus.Add(workerURL, WORKER_IDLE);
            this.myURL = workerURL;
            this.jobTrackerURL = jobTrackerURL;
        }

        public void setURLs(string myURL, string jobTrackerURL)
        {
            this.myURL = myURL;
            this.jobTrackerURL = jobTrackerURL;
        }

        public bool submit(string myInputPath, int numSplits, string myOutputPath, string myClassName, byte[] myCode, int clientPort) {
            myThread = Thread.CurrentThread;
            inputPath = myInputPath;
            nSplits = numSplits;
            outputPath = myOutputPath;
            className = myClassName;
            code = myCode;
            Dictionary<string, List<int>> splitsPerWorker = splitFile();
            assignMapTask(splitsPerWorker, clientPort);
            return true;
        }

        /* Each worker is given a list of splits, which are distributed in round robin style. */
        public Dictionary<string, List<int>> splitFile() {
            Dictionary<string, List<int>> splitsPerWorker = new Dictionary<string, List<int>>();
            List<string> availableWorkers = getAvailableWorkers();
            int nAvailableWorkers = availableWorkers.Count;
            for (int i = 0; i < nSplits; i++)
            {
                string workerURL = availableWorkers[i%nAvailableWorkers];
                if (!splitsPerWorker.ContainsKey(workerURL))
                    splitsPerWorker.Add(workerURL, new List<int>());
                splitsPerWorker[workerURL].Add(i);
                Console.WriteLine("SPLIT " + i + " ASSIGNED TO " + workerURL);
            }
            return splitsPerWorker;
        }

        public string getSplitContent(int split, string inputPath, int nSplits, int clientPort) {
            string content = null;
            try {
                IClient client = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:" + clientPort + "/C");
                content = client.getSplitContent(split, inputPath, nSplits);
            }
            catch (SocketException) {
                System.Console.WriteLine("Could not locate the client");
            }
            return content;
        }

        public List<string> getAvailableWorkers()
        {
            List<String> availableWorkers = new List<string>();
            foreach (KeyValuePair<string, string> entry in workersStatus)
            {
                if (entry.Value.Equals(this.WORKER_IDLE))
                {
                    availableWorkers.Add(entry.Key);
                }
            }
            return availableWorkers;
        }

        public void assignMapTask(Dictionary<string, List<int>> splitsPerWorker, int clientPort)
        {
            List<string> availableWorkers = getAvailableWorkers();
            for (int i = 0; i < availableWorkers.Count; i++)
            {
                if (availableWorkers[i].Equals(myURL))
                {
                    new Thread(() => doMapTask(splitsPerWorker[myURL], myURL, inputPath, outputPath, code, className, nSplits, clientPort)).Start();
                }
                else
                {
                    new Thread(() => doTask(availableWorkers[i], splitsPerWorker[availableWorkers[i]], clientPort)).Start();
                    Thread.Sleep(1);
                }
            }
        }

        public Dictionary<string, List<int>> giveFailedSplits(List<int> splits)
        {
            Dictionary<string, List<int>> splitsPerWorker = new Dictionary<string, List<int>>();
            List<string> availableWorkers = getAvailableWorkers();
            int nAvailableWorkers = availableWorkers.Count;
            for (int i = 0; i < splits.Count; i++)
            {
                string workerURL = availableWorkers[i % nAvailableWorkers];
                if (!splitsPerWorker.ContainsKey(workerURL))
                    splitsPerWorker.Add(workerURL, new List<int>());

                splitsPerWorker[workerURL].Add(splits[i]);
                Console.WriteLine("SPLIT " + splits[i] + " REASSIGNED TO " + workerURL);
            }
            return splitsPerWorker;
        }

        public void doTask(string workerURL, List<int> splits, int clientPort)
        {
            try
            {
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                worker.doMapTask(splits, workerURL, inputPath, outputPath, code, className, nSplits, clientPort);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("WORKER " + workerURL + " IS NOT AVAILABLE: REDISTRIBUTING SPLITS...");
                workersStatus[workerURL] = WORKER_UNAVAILABLE;
                Dictionary<string, List<int>> remainingSplits = giveFailedSplits(splits);
                assignMapTask(remainingSplits, clientPort);
            }
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

        public void doMapTask(List<int> splits, string workerURL,string inputPath,string outputPath, byte[] code, string className, int nSplits, int clientPort)
        {
            if (!isFrozen)
            {
                myThread = Thread.CurrentThread;
                for (int i = 0; i < splits.Count; i++)
                {
                    string mySplitContent = getSplitContent(splits[i], inputPath, nSplits, clientPort);
                    IList<KeyValuePair<string, string>> result = processSplit(mySplitContent, code, className);
                    try
                    {
                        IClient client = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:" + clientPort + "/C");
                        client.sendResult(result, outputPath, splits[i] + ".out");
                        Console.WriteLine("WORKER " + workerURL + " FINISHED " + splits[i]);
                    }
                    catch (SocketException)
                    {
                        System.Console.WriteLine("Could not locate the client...");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void getStatus() { }

        public void slowWorker(int seconds) {
            TimeSpan t = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 0, Convert.ToInt32(seconds)));
            bool canPass = false;

            while (!canPass)
            {
                try
                {
                    if (myThread.IsAlive)
                        canPass = true;
                }
                catch (NullReferenceException){ }
            }

            try
            {
                myThread.Suspend();
                while (DateTime.Now.TimeOfDay <= t) { }
                myThread.Resume();
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public void freezeWorker()
        {
            try
            {
                Console.WriteLine("FREEZE!");
                isFrozen = true;
                myThread.Suspend();
            }
            catch (NullReferenceException)
            {
                // If it reaches this point, it means there is no job being done and we don't suspend it.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void unfreezeWorker()
        {
            try
            {
                Console.WriteLine("UNFREEZE!");
                isFrozen = false;
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                jobTracker.notifyIsAvailable(myURL);
                myThread.Resume();
            }
            catch (NullReferenceException)
            {
                // If it reaches this point, it means there was no job being done and we don't resume it.
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void notifyIsAvailable(string workerURL)
        {
            workersStatus[workerURL] = WORKER_IDLE;
        }

        public void freezeJobTracker() { }
        public void unfreezeJobTracker() { }
    }
}