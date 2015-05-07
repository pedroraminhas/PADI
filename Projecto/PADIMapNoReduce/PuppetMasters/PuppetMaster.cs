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
        static Dictionary<string, string> workers = new Dictionary<string, string>();
        

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
                    break;
                case "SUBMIT":
                    new Thread(() => submit(splittedInstruction)).Start();
                    break;
                case "WAIT":
                    wait(splittedInstruction);
                    break;
                case "STATUS":
                    new Thread(() => getStatus()).Start();
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
                    break;
                case "UNFREEZEC":
                    new Thread(() => unfreezeJobTracker(splittedInstruction)).Start();
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
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL + " " + entryURL + " " + jobTrackerURL);
                }
                workers.Add(splittedInstruction[1], serviceURL);
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
            Console.WriteLine("Sending a job from the client port " + clientPort + "...");
            Process.Start(@"..\..\..\Client\bin\Debug\Client.exe", String.Join(" ", splittedInstruction) + " " + clientPort);
        }

        public static void wait(string[] splittedInstruction)
        {
            Console.WriteLine("Waiting " + splittedInstruction[1] + " seconds...");
            Thread.Sleep(int.Parse(splittedInstruction[1]) * 1000);
        }

        private static void getStatus()
        {
            Console.WriteLine("Printing system's status...");
            IWorker jobTracker = (IWorker)Activator.GetObject(typeof(IWorker), jobTrackerURL);
            jobTracker.getStatus();
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
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.freezeJobTracker();
        }

        private static void unfreezeJobTracker(string[] splittedInstruction)
        {
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.unfreezeJobTracker();
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