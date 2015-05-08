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
            TcpChannel channel = new TcpChannel(int.Parse(port));
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(WorkerServices), "W", WellKnownObjectMode.Singleton);

            // if it's a worker, inform the job tracker about joining the system.
            if (args.Length == 2)
            {
                string entryURL = args[1];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                jobTracker.notify(serviceURL);
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), serviceURL);
                worker.setURLs(serviceURL, entryURL);
            }
            //if it's a job tracker, adds itself to the list of workers
            else
            {
                string entryURL = args[0];
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), entryURL);
                jobTracker.notify(entryURL);
                jobTracker.setURLs(entryURL, entryURL);
            }
            Console.WriteLine("Running @ " + port);
            Console.ReadLine();
        }
    }

    internal class WorkerServices : MarshalByRefObject, IWorker {
        private string myURL;
        private string jobTrackerURL = "NOT SET";
        Dictionary<string, string> workersStatus = new Dictionary<string, string>();
        private const string WORKER_IDLE = "IDLE";
        private const string WORKER_UNAVAILABLE = "UNAVAILABLE";
        private Dictionary<string, Dictionary<int, string>> tasksStatus = new Dictionary<string, Dictionary<int, string>>();
        private const string PHASE_START = "READY TO START";
        private const string PHASE_SPLIT = "TRANSFERING SPLIT CONTENT";
        private const string PHASE_PROCESSING = "WAITING TO BE PROCESSED";
        private const string PHASE_SEND = "TRANSFERING MAP RESULTS";
        private const string PHASE_CONCLUDED = "CONCLUDED";
        private bool isFrozen = false;
        Thread myThread;
        
        public void notify(string workerURL)
        {
            workersStatus.Add(workerURL, WORKER_IDLE);
        }

        public void setURLs(string myURL, string jobTrackerURL)
        {
            this.myURL = myURL;
            this.jobTrackerURL = jobTrackerURL;
        }

        public void submit(string inputPath, string outputPath, int nSplits, string className, byte[] code, int clientPort) {
            myThread = Thread.CurrentThread;
            Dictionary<string, List<int>> splitsPerWorker = splitFile(nSplits);
            assignMapTask(inputPath, outputPath, nSplits, className, code, splitsPerWorker, clientPort);
        }

        /* Each worker is given a list of splits, which are distributed in round robin style. */
        public Dictionary<string, List<int>> splitFile(int nSplits)
        {
            Dictionary<string, List<int>> splitsPerWorker = new Dictionary<string, List<int>>();
            List<string> availableWorkers = getAvailableWorkers();
            int nAvailableWorkers = availableWorkers.Count;
            for (int i = 0; i < nSplits; i++)
            {
                string workerURL = availableWorkers[i%nAvailableWorkers];
                if (!splitsPerWorker.ContainsKey(workerURL))
                    splitsPerWorker.Add(workerURL, new List<int>());
                splitsPerWorker[workerURL].Add(i);
                Console.WriteLine("Split " + i + " assigned to " + workerURL);
            }
            return splitsPerWorker;
        }

        public void assignMapTask(string inputPath, string outputPath, int nSplits, string className, byte[] code, Dictionary<string, List<int>> splitsPerWorker, int clientPort)
        {
            List<string> availableWorkers = getAvailableWorkers();
            for (int i = 0; i < availableWorkers.Count; i++)
            {
                if (availableWorkers[i].Equals(myURL))
                {
                    new Thread(() => doMapTask(splitsPerWorker[myURL], myURL, inputPath, outputPath, code, className, nSplits, clientPort)).Start();
                    Thread.Sleep(1);
                }
                else
                {
                    new Thread(() => doTask(inputPath, outputPath, nSplits, className, code, availableWorkers[i], splitsPerWorker[availableWorkers[i]], clientPort)).Start();
                    Thread.Sleep(1);
                }
            }
        }

        public List<string> getAvailableWorkers()
        {
            List<String> availableWorkers = new List<string>();
            foreach (KeyValuePair<string, string> entry in workersStatus)
            {
                if (entry.Value.Equals(WORKER_IDLE))
                {
                    availableWorkers.Add(entry.Key);
                }
            }
            return availableWorkers;
        }

        public string getSplitContent(int split, string inputPath, int nSplits, int clientPort)
        {
            tasksStatus[inputPath][split] = PHASE_SPLIT;
            string content = null;
            try
            {
                IClient client = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:" + clientPort + "/C");
                content = client.getSplitContent(split, inputPath, nSplits);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate the client");
            }
            return content;
        }

        public void doTask(string inputPath, string outputPath, int nSplits, string className, byte[] code, string workerURL, List<int> splits, int clientPort)
        {
            try
            {
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                worker.doMapTask(splits, workerURL, inputPath, outputPath, code, className, nSplits, clientPort);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Worker " + workerURL + " is not available: redistributing splits...");
                workersStatus[workerURL] = WORKER_UNAVAILABLE;
                Dictionary<string, List<int>> remainingSplits = giveFailedSplits(splits);
                assignMapTask(inputPath, outputPath, nSplits, className, code, remainingSplits, clientPort);
            }
        }

        public void doMapTask(List<int> splits, string workerURL, string inputPath, string outputPath, byte[] code, string className, int nSplits, int clientPort)
        {
            try
            {
                tasksStatus.Add(inputPath, new Dictionary<int, string>());
                foreach (int split in splits)
                {
                    tasksStatus[inputPath].Add(split, PHASE_START);
                }

                if (!isFrozen)
                {
                    myThread = Thread.CurrentThread;
                    for (int i = 0; i < splits.Count; i++)
                    {
                        string mySplitContent = getSplitContent(splits[i], inputPath, nSplits, clientPort);
                        IList<KeyValuePair<string, string>> result = processSplit(inputPath, splits[i], mySplitContent, code, className);
                        try
                        {
                            tasksStatus[inputPath][splits[i]] = PHASE_SEND;
                            IClient client = (IClient)Activator.GetObject(typeof(IClient), "tcp://localhost:" + clientPort + "/C");
                            client.sendResult(result, outputPath, splits[i] + ".out");
                            Console.WriteLine("WORKER " + workerURL + " FINISHED " + splits[i]);
                            tasksStatus[inputPath][splits[i]] = PHASE_CONCLUDED;
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
            catch (Exception e) //PARA TIRAR QUANDO PARAR DE SE ESBARDALHAR
            {
                Console.WriteLine(e.Message);
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
        
        public IList<KeyValuePair<string, string>> processSplit(string inputPath, int split, string mySplitContent, byte[] code, string className)
        {
            tasksStatus[inputPath][split] = PHASE_PROCESSING;
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

        public void printSystemStatus(bool toJobTracker)    //InvalidOperationException se for chamado quando estão a ser alterados workers. É preciso fazer isto: http://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
        {
            if (toJobTracker)
            {
                Console.WriteLine("\n\nSYSTEM STATE\n");
                Console.WriteLine("JOB TRACKER: " + jobTrackerURL);
                Console.WriteLine("NUMBER OF REGISTERED WORKERS: " + workersStatus.Count);
                List<string> failedWorkers = new List<string>();
                foreach (KeyValuePair<string, string> entry in workersStatus)
                {
                    Console.WriteLine("\t" + entry.Key);
                    if (entry.Value.Equals(WORKER_UNAVAILABLE))
                        failedWorkers.Add(entry.Key);
                }
                Console.WriteLine("NUMBER OF FAILED WORKERS: " + failedWorkers.Count);
                foreach (string worker in failedWorkers)
                    Console.WriteLine("\t" + worker);
                foreach (KeyValuePair<string, string> entry in workersStatus)
                {
                    string workerURL = entry.Key;
                    if (!workerURL.Equals(myURL))
                    {
                        IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                        worker.printJobsStatus();
                    }
                    else
                        printJobsStatus();
                }
            }
        }

        public void printJobsStatus()
        {
            foreach (KeyValuePair<string, Dictionary<int, string>> job in tasksStatus)
            {
                string inputPath = job.Key;
                Console.WriteLine("\nJOB: " + inputPath);
                foreach (KeyValuePair<int, string> split in tasksStatus[inputPath])
                    Console.WriteLine("SPLIT: " + split.Key + "\tSTATUS: " + split.Value);
            }
        }

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
                catch (NullReferenceException) { }
            }
            try
            {
                myThread.Suspend();
                while (DateTime.Now.TimeOfDay <= t) { }
                myThread.Resume();
            }
            catch (Exception e) //VER QUAL A EXCEPÇÃO QUE LANÇA
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void freezeWorker()
        {
            try
            {
                Console.WriteLine("FREEZE!");
                isFrozen = true;
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                jobTracker.notifyAvailability(myURL, WORKER_UNAVAILABLE);
                myThread.Suspend();
            }
            catch (NullReferenceException) // VER MELHOR ISTO
            {
                // If it reaches this point, it means there is no job being done and we don't suspend it.
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Job tracker URL isn't known yet...");
            }
            catch (ThreadStateException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void unfreezeWorker()
        {
            if (isFrozen)
            {
                try
                {
                    Console.WriteLine("UNFREEZE!");
                    isFrozen = false;
                    IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                    jobTracker.notifyAvailability(myURL, WORKER_IDLE);
                    myThread.Resume();
                }
                catch (ThreadStateException)
                { /* If it reaches this point, it means there was no job being done and we can't resume the worker thread. */ }
                catch (Exception e) //VER QUAL A EXCEPÇÃO QUE LANÇA
                {
                    Console.WriteLine(e.GetType());
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            else
                Console.WriteLine("This worker wasn't frozen!");
        }

        public void notifyAvailability(string workerURL, string state)
        {
            workersStatus[workerURL] = state;
        }
        
        public void freezeJobTracker() { }
        public void unfreezeJobTracker() { }
    }
}