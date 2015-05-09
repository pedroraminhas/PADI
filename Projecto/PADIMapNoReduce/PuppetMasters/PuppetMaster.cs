using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using PADIMapNoReduce;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

namespace PADIMapNoReduce
{
    static class PuppetMaster
    {
        private static int puppetMasterPort;
        private static string myURL;
        //static string[] puppetMastersURLs;
        private static string jobTrackerURL;
        // The list contains the URL at 0 and status at 1 (status can be AVAILABLE or UNAVAILABLE)
        private static Dictionary<string, string[]> workersStatus = new Dictionary<string, string[]>();
        private const string AVAILABLE = "AVAILABLE";
        private const string UNAVAILABLE = "UNAVAILABLE";

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                puppetMasterPort= 20001;
                myURL = "tcp://localhost:" + puppetMasterPort + "/PM";
                registerChannel(puppetMasterPort);
                //puppetMastersURLs = startPuppetMasters();
                startGUI();
            }
            else
            {
                puppetMasterPort= Int32.Parse(args[0]);
                myURL = "tcp://localhost:" + puppetMasterPort + "/PM";
                registerChannel(puppetMasterPort);
                Console.WriteLine("PORT = " + Int32.Parse(args[0]));
                Console.ReadLine();
            }
        }

        private static void registerChannel(int port)
        {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMasterServices), "PM", WellKnownObjectMode.Singleton);
        }

        private static string[] startPuppetMasters()
        {
            string[] ports = System.IO.File.ReadAllLines(@"..\..\PMconfig.txt");
            foreach (string port in ports)
            {
                Process.Start(@"PuppetMasters.exe", port);
            }
            return ports;
        }

        public static void startGUI()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GUI());
        }

        public static void splitInstruction(string instruction)
        {
            string[] splittedInstruction = instruction.Split(' ');
            string command = splittedInstruction[0];
            switch (command)
            {
                case "WORKER":
                    new Thread(() => startWorker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "SUBMIT":
                    new Thread(() => submit(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "WAIT":
                    wait(splittedInstruction[1]);
                    break;
                case "STATUS":
                    new Thread(() => getStatus()).Start();
                    Thread.Sleep(1);
                    break;
                case "SLOWW":
                    new Thread(() => slowWorker(splittedInstruction[1], splittedInstruction[2])).Start();
                    Thread.Sleep(1);
                    break;
                case "FREEZEW":
                    new Thread(() => freezeWorker(splittedInstruction[1])).Start();
                    Thread.Sleep(1);
                    break;
                case "UNFREEZEW":
                    new Thread(() => unfreezeWorker(splittedInstruction[1])).Start();
                    Thread.Sleep(1);
                    break;
                case "FREEZEC":
                    new Thread(() => freezeJobTracker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "UNFREEZEC":
                    new Thread(() => unfreezeJobTracker(splittedInstruction[1])).Start();
                    Thread.Sleep(1);
                    break;
                default:
                    Console.WriteLine("BAD COMMAND - PLEASE CHECK YOUR SPELLING.");
                    break;
            }
        }

        /* If the PuppetMaster URL in the instruction is this PuppetMasters's URL, creates the worker;
         * Else, does a remote call to the wanted PuppetMaster URL so it creates the worker. */
        public static void startWorker(string[] splittedInstruction)
        {
            string workerID = splittedInstruction[1];
            string targetPuppetMasterURL = splittedInstruction[2];
            string serviceURL = splittedInstruction[3];
            if (targetPuppetMasterURL.Equals(myURL))
            {
                if (splittedInstruction.Length == 4)
                {
                    jobTrackerURL = serviceURL;
                    Console.WriteLine("Starting the job tracker at " + serviceURL + "...");
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL);
                }
                else
                {
                    string entryURL = splittedInstruction[4];
                    Console.WriteLine("Starting a worker at " + serviceURL + "...");
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL + " " + entryURL);
                }
                workersStatus.Add(workerID, new string[] {serviceURL, AVAILABLE});
            }
            else
            {
                Console.WriteLine("Asking the puppet master at " + targetPuppetMasterURL + " to create a worker at " + serviceURL + "...");
                IPuppetMasters puppetMaster = (IPuppetMasters)Activator.GetObject(typeof(IWorker), targetPuppetMasterURL);
                puppetMaster.startWorker(splittedInstruction);
            }
        }

        public static void submit(string[] splittedInstruction)
        {
            Random r = new Random();
            int clientPort = r.Next(10001, 19999);
            Console.WriteLine("Sending a job from client port = " + clientPort + "...");
            string[] parameters = { jobTrackerURL, splittedInstruction[2], splittedInstruction[3], splittedInstruction[4], splittedInstruction[5], splittedInstruction[6], clientPort.ToString() };
            Process.Start(@"..\..\..\Client\bin\Debug\Client.exe", String.Join(" ", parameters));
        }

        public static void wait(string seconds)
        {
            Console.WriteLine("Waiting " + seconds + " seconds...");
            Thread.Sleep(int.Parse(seconds) * 1000);
        }

        private static void getStatus()
        {
            Console.WriteLine("Printing system's status...");
            Console.WriteLine("\n\n --- SYSTEM STATE: ABOUT THE SYSTEM ---\n");
            if (jobTrackerURL != null)
                Console.WriteLine("JOB TRACKER: " + jobTrackerURL + "\n");
            else
                Console.WriteLine("JOB TRACKER: NOT SET\n");
            Console.WriteLine("JOB TRACKER ASPECT OF WORKERS");
            foreach (KeyValuePair<string, string[]> worker in workersStatus)
            {
                if (worker.Value[1].Equals(AVAILABLE))
                    Console.WriteLine("\t~ " + worker.Value[0] + " - AVAILABLE");
                else
                    Console.WriteLine("\t~ " + worker.Value[0] + " - UNAVAILABLE");
            }
            Console.WriteLine("\n");

            try
            {
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                jobTracker.printSystemStatus();
            }
            catch (ArgumentNullException) { }
        }

        private static void slowWorker(string workerID, string seconds)
        {
            try
            {
                string workerURL = workersStatus[workerID][0];
                Console.WriteLine("Slowing worker at " + workerURL + " for " + seconds + " seconds...");
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                target.slowWorker(int.Parse(seconds));
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no worker with ID = " + workerID + "!");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void freezeWorker(string workerID)
        {
            try
            {
                string workerURL = workersStatus[workerID][0];
                Console.WriteLine("Freezing worker at " +  workerURL + "...");
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                target.freezeWorker();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("\tThere is no worker with ID = " + workerID + "!");
            }
        }

        private static void unfreezeWorker(string workerID)
        {
            string workerURL = workersStatus[workerID][0];
            try
            {
                Console.WriteLine("Unfreezing worker at " + workerURL + "...");
                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerURL);
                worker.unfreezeWorker();
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no worker with ID = " + workerURL + "!");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void freezeJobTracker(string[] splittedInstruction)
        {
            try
            {
                string freezingID = splittedInstruction[1];
                string freezingURL = workersStatus[freezingID][0];
                Console.WriteLine("Freezing job tracker at " + freezingURL + "...");
                if (freezingURL.Equals(jobTrackerURL))
                {
                    workersStatus[freezingID][1] = UNAVAILABLE;
                    IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                    jobTracker.freezeJobTracker();
                    jobTrackerURL = getNewJobTracker();
                    notifyNewJobTracker(jobTrackerURL);
                }
                else
                    Console.WriteLine("\tID " + splittedInstruction[1] + " doesn't correspond to the job tracker's URL!");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("\tImpossible to freeze the job tracker: there isn't one assigned yet.");
            }
        }

        private static string getNewJobTracker()
        {
            foreach (KeyValuePair<string, string[]> worker in workersStatus)
                if (worker.Value[1].Equals(AVAILABLE))
                    return worker.Value[0];
            return null;
        }

        private static void notifyNewJobTracker(string jobTrackerURL)
        {
            if (jobTrackerURL != null)
                foreach (KeyValuePair<string, string[]> worker in workersStatus)
                {
                    Console.WriteLine("\tNotifying " + worker.Value + " about the new job tracker...");
                    IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), worker.Value[0]);
                    target.notifyNewJobTracker(jobTrackerURL);
                }
            else
                Console.WriteLine("There are no available job trackers!");
        }

        private static void unfreezeJobTracker(string unfreezingID)
        {
            try
            {
                string unfreezingURL = workersStatus[unfreezingID][0];
                Console.WriteLine("Unfreezing job tracker " + unfreezingURL + "...");
                workersStatus[unfreezingID][1] = AVAILABLE;
                IWorker frozenJobTracker = (IWorker)Activator.GetObject(typeof(IWorker), unfreezingURL);
                frozenJobTracker.unfreezeJobTracker();
                if (jobTrackerURL == null)
                {
                    jobTrackerURL = unfreezingURL;
                    notifyNewJobTracker(jobTrackerURL);
                }
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("\tThere is no previous job tracker with ID = " + unfreezingID + "!");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("\tImpossible to unfreeze the job tracker: there isn't one assigned yet!");
            }
        }
    }
        
    internal class PuppetMasterServices : MarshalByRefObject, IPuppetMasters
    {
        public void startWorker(string[] parameters)
        {
            PuppetMaster.startWorker(parameters);
        }
    }    
}