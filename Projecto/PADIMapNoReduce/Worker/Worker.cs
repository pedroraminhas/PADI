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
    /*
     * Class Worker represents a worker in the system which performs jobs
     * submitted by the client
     */ 
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

    /*
     * Internal Class WorkerServices represents a remote object of class worker
     */ 
    internal class WorkerServices : MarshalByRefObject, IWorker {
        private string myURL;
        private string jobTrackerURL = "NOT SET";
        Dictionary<string, string> workersStatus = new Dictionary<string, string>();
        private const string WORKER_IDLE = "IDLE";
        private const string WORKER_UNAVAILABLE = "UNAVAILABLE";
        private Dictionary<string, Dictionary<int, string>> tasksStatus;
        private const string PHASE_START = "READY TO START";
        private const string PHASE_SPLIT = "TRANSFERING SPLIT CONTENT";
        private const string PHASE_PROCESSING = "WAITING TO BE PROCESSED";
        private const string PHASE_SEND = "TRANSFERING MAP RESULTS";
        private const string PHASE_CONCLUDED = "CONCLUDED";
        private bool isFrozen = false;
        private bool jobTrackerIsFrozen = false;
        Thread myThread;
        
        //change the status of the worker with workerURL to IDLE
        public void notify(string workerURL)
        {
            workersStatus.Add(workerURL, WORKER_IDLE);
        }

        //Sets the url of the worker and the url of the jobTracker
        public void setURLs(string myURL, string jobTrackerURL)
        {
            this.myURL = myURL;
            this.jobTrackerURL = jobTrackerURL;
        }

        // Split a file and assigns a job (can be one or more splits) to the workers
        public void submit(string inputPath, string outputPath, int nSplits, string className, byte[] code, int clientPort) {
            if (!jobTrackerIsFrozen)
            {
                myThread = Thread.CurrentThread;
                Dictionary<string, List<int>> splitsPerWorker = splitFile(nSplits);
                assignMapTask(inputPath, outputPath, nSplits, className, code, splitsPerWorker, clientPort);
            }
            else
                throw new InvalidOperationException();
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

        //Creates a thread that assigns a job to the worker
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

        //Gets a list of available workers
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

        // Asks the client for the content of the split
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

        /* Calls a method in the worker to execute the work, if the worker is not available re-assign
        *  the job to the other available workers
        */  
        public void doTask(string inputPath, string outputPath, int nSplits, string className, byte[] code, string workerURL, List<int> splits, int clientPort)
        {
            try
            {
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                worker.doMapTask(splits, workerURL, inputPath, outputPath, code, className, nSplits, clientPort);
            }
            //if the worker is not available, re-assign the job to the other workers
            catch (InvalidOperationException)
            {
                Console.WriteLine("Worker " + workerURL + " is not available: redistributing splits...");
                workersStatus[workerURL] = WORKER_UNAVAILABLE;
                Dictionary<string, List<int>> remainingSplits = giveFailedSplits(splits);
                assignMapTask(inputPath, outputPath, nSplits, className, code, remainingSplits, clientPort);
            }
        }

        /*
         * Asks the client for the assigned split and performs the work
         */
        public void doMapTask(List<int> splits, string workerURL, string inputPath, string outputPath, byte[] code, string className, int nSplits, int clientPort)
        {
            try
            {
                tasksStatus = new Dictionary<string, Dictionary<int, string>>();
                tasksStatus.Add(inputPath, new Dictionary<int, string>());
                foreach (int split in splits)
                    tasksStatus[inputPath].Add(split, PHASE_START);

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
                            Console.WriteLine("I just " + workerURL + " finished " + splits[i]);
                            tasksStatus[inputPath][splits[i]] = PHASE_CONCLUDED;
                        }
                        catch (SocketException)
                        {
                            System.Console.WriteLine("Could not locate the client...");
                        }
                    }
                }
                else
                    throw new InvalidOperationException();
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        //Re-assigns the failed splits to the other available workers
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
        
        //Gets the dll and invokes the function to process the split
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
                throw new Exception("Could not invoke method!");
            }
            else
            {
                return null;
            }
        }

        /*
         * Prints the status of the system
         * Number of registered workers, Number of failed workers, status of each worker and the phase 
         * of the job
         */ 
        public void printSystemStatus()    
        {
            try
            {
                Console.WriteLine("\n\n --- SYSTEM STATE: ABOUT THE JOB TRACKER ---\n");
                Console.WriteLine("NUMBER OF REGISTERED WORKERS: " + workersStatus.Count);
                List<string> failedWorkers = new List<string>();
                foreach (KeyValuePair<string, string> entry in workersStatus)
                {
                    Console.WriteLine("\t~ " + entry.Key);
                    if (entry.Value.Equals(WORKER_UNAVAILABLE))
                        failedWorkers.Add(entry.Key);
                }
                Console.WriteLine("NUMBER OF FAILED WORKERS: " + failedWorkers.Count);
                foreach (string worker in failedWorkers)
                    Console.WriteLine("\t~ " + worker);
                Console.WriteLine("\n");
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
            catch (Exception e)
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        //Prints the status of the job
        public void printJobsStatus()
        {
            if (tasksStatus != null)
            {
                foreach (KeyValuePair<string, Dictionary<int, string>> job in tasksStatus)
                {
                    Console.WriteLine(" --- SYSTEM STATE: ABOUT JOBS ---\n");
                    string inputPath = job.Key;
                    Console.WriteLine("JOB: " + inputPath);
                    foreach (KeyValuePair<int, string> split in tasksStatus[inputPath])
                        Console.WriteLine("SPLIT " + split.Key + ": " + split.Value);
                    Console.WriteLine("\n");
                }
            }
        }

        //Slows worker the given seconds
        public void slowWorker(int seconds) {
            TimeSpan t = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 0, Convert.ToInt32(seconds)));
            bool canPass = false;

            while (!canPass)
            {
                try
                {
                    if (myThread.IsAlive)  //Slows the main thread only if it is active, because the thread that executes 
                        canPass = true;    //this method is diferent that the thread that is executing the job
                }
                catch (NullReferenceException) { }
            }
            try
            {
                myThread.Suspend();
                while (DateTime.Now.TimeOfDay <= t) { }
                myThread.Resume();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        //Freezes the worker
        public void freezeWorker()
        {
            try
            {
                Console.WriteLine("FREEZE!");
                isFrozen = true;
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                jobTracker.notifyAvailability(myURL, WORKER_UNAVAILABLE);
                myThread.Suspend();         //Suspend the thread that is executing the job
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Job tracker URL isn't known yet...");
            }
            catch (ThreadStateException)
            { }
        }

        //Unfreezes the worker
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
                    myThread.Resume();      //Resumes the thread that is executing the job
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

        //Change the state of the worker
        public void notifyAvailability(string workerURL, string state)
        {
            workersStatus[workerURL] = state;
        }
        
        //Freezes the job tracker
        public void freezeJobTracker()
        {
            Console.WriteLine("FREEZE!");
            jobTrackerIsFrozen = true;
        }

        //Gets the status of the workers from the failed job tracker and sets the job tracker url to the url of the new one
        public void notifyNewJobTracker(string newJobTrackerURL)
        {
            if (newJobTrackerURL.Equals(myURL))
            {
                IWorker previousJobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                workersStatus = previousJobTracker.getWorkersStatus();
            }
            jobTrackerURL = newJobTrackerURL;
        }

        //Get worker status
        public Dictionary<string, string> getWorkersStatus()
        {
            return workersStatus;
        }

        //unfreeze the job tracker
        public void unfreezeJobTracker()
        {
            Console.WriteLine("UNFREEZE!");
            jobTrackerIsFrozen = false;
        }
    }
}