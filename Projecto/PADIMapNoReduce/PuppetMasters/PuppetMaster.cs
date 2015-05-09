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
        static int puppetMasterPort;
        static string myURL;
        //static string[] puppetMastersURLs;
        static string jobTrackerURL;
        static string previousJobTrackerURL;
        static Dictionary<string, string> workers = new Dictionary<string, string>();
        private static Dictionary<string, bool> workersStatus = new Dictionary<string, bool>();     // false if frozen

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
                    wait(splittedInstruction);
                    break;
                case "STATUS":
                    new Thread(() => getStatus()).Start();
                    Thread.Sleep(1);
                    break;
                case "SLOWW":
                    new Thread(() => slowWorker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "FREEZEW":
                    new Thread(() => freezeWorker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "UNFREEZEW":
                    new Thread(() => unfreezeWorker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "FREEZEC":
                    new Thread(() => freezeJobTracker(splittedInstruction)).Start();
                    Thread.Sleep(1);
                    break;
                case "UNFREEZEC":
                    new Thread(() => unfreezeJobTracker(splittedInstruction)).Start();
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
                workers.Add(workerID, serviceURL);
                workersStatus.Add(serviceURL, true);
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

        public static void wait(string[] splittedInstruction)
        {
            Console.WriteLine("Waiting " + splittedInstruction[1] + " seconds...");
            Thread.Sleep(int.Parse(splittedInstruction[1]) * 1000);
        }

        private static void getStatus()
        {
            Console.WriteLine("Printing system's status...");
            try
            {
                IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                jobTracker.printSystemStatus(true);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Impossible to print the status: the system isn't up yet.");
            }
        }

        private static void slowWorker(string[] splittedInstruction)
        {
            try
            {
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
                target.slowWorker(Convert.ToInt32(splittedInstruction[2]));
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void freezeWorker(string[] splittedInstruction)
        {
            try
            {
                Console.WriteLine("Freezing worker " + splittedInstruction[1] + "...");
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
                target.freezeWorker();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no worker with ID = " + splittedInstruction[1] + "!");
            }
        }

        private static void unfreezeWorker(string[] splittedInstruction)
        {
            try
            {
                Console.WriteLine("Unfreezing worker " + splittedInstruction[1] + "...");
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
                target.unfreezeWorker();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no worker with ID = " + splittedInstruction[1] + "!");
            }
        }

        private static void freezeJobTracker(string[] splittedInstruction)
        {
            try
            {
                string freezingURL = workers[splittedInstruction[1]];
                Console.WriteLine("Freezing job tracker " + freezingURL + "...");
                if (freezingURL.Equals(jobTrackerURL))
                {
                    workersStatus[freezingURL] = false;
                    previousJobTrackerURL = jobTrackerURL;
                    IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
                    jobTracker.freezeJobTracker();
                    jobTrackerURL = getNewJobTracker();
                    notifyNewJobTracker(jobTrackerURL);
                }
                else
                    Console.WriteLine("The ID provided doesn't correspond to the job tracker's URL.");
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no job tracker with ID = " + splittedInstruction[1] + "!");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Impossible to freeze the job tracker: there isn't one assigned yet.");
            }
        }

        private static string getNewJobTracker()
        {
            foreach (KeyValuePair<string, bool> worker in workersStatus)
            {
                if (worker.Value.Equals(true))
                {
                    Console.WriteLine("VOU MANDAR O " + worker.Key);
                    return worker.Key;
                }
            }
            return null;
        }

        private static void notifyNewJobTracker(string jobTrackerURL)
        {
            foreach (KeyValuePair<string, string> worker in workers)
            {
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), worker.Value);
                target.notifyNewJobTracker(jobTrackerURL);
            }
        }

        private static void unfreezeJobTracker(string[] splittedInstruction)
        {
            try
            {
                Console.WriteLine("Unfreezing job tracker " + workers[splittedInstruction[1]] + "...");
                if (workers[splittedInstruction[1]].Equals(previousJobTrackerURL))
                {
                    IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), previousJobTrackerURL);
                    jobTracker.unfreezeJobTracker();
                }
                else
                    Console.WriteLine("The ID provided doesn't correspond to the job tracker's URL!");
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("There is no job tracker with ID = " + splittedInstruction[1] + "!");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Impossible to unfreeze the job tracker: there isn't one assigned yet!");
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