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
        static string puppetMasterURL;
        static string[] puppetMastersURLs;
        static Dictionary<string, string> workers = new Dictionary<string, string>();
        static string[] splittedInstruction;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                puppetMasterPort= 20001;
                puppetMasterURL = "tcp://localhost:" + puppetMasterPort + "/PM";
                registerChannel(puppetMasterPort);
                puppetMastersURLs = startPuppetMasters();
                startGUI();
            }
            else
            {
                puppetMasterPort= Int32.Parse(args[0]);
                puppetMasterURL = "tcp://localhost:" + puppetMasterPort + "/PM";
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
            splittedInstruction = instruction.Split(' ');
            string command = splittedInstruction[0];
            switch (command)
            {
                case "WORKER":
                    startWorker(splittedInstruction);
                    break;
                case "SUBMIT":
                    submit();
                    break;
                case "WAIT":
                    wait();
                    break;
                case "STATUS":
                    getStatus();
                    break;
                case "SLOWW":
                    slowWorker();
                    break;
                case "FREEZEW":
                    freezeWorker();
                    break;
                case "UNFREEZEW":
                    unfreezeWorker();
                    break;
                case "FREEZEC":
                    freezeJobTracker();
                    break;
                case "UNFREEZEC":
                    unfreezeJobTracker();
                    break;
                default:
                    Console.WriteLine("BAD COMMAND - PLEASE CHECK YOUR SPELLING.");
                    break;
            }
        }

        
        /* If the PuppetMaster URL in the instruction is this PuppetMasters's URL, creates the worker;
         * Else, does a remote call to the wanted PuppetMaster URL so it creates the worker. */
        public static void startWorker(string[] workerInfo)
        {
            string targetPuppetMasterURL = workerInfo[2];
            string serviceURL = workerInfo[3];
            if (targetPuppetMasterURL.Equals(puppetMasterURL))
            {
                if (workerInfo.Length == 4)
                {
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL);
                    Console.WriteLine("INICIEI O JOB TRACKER " + serviceURL);
                }
                else
                {
                    string entryURL = workerInfo[4];
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL + " " + entryURL);
                    Console.WriteLine("INICIEI O WORKER " + serviceURL);
                }
                string workerID = workerInfo[1];
                workers.Add(workerID, serviceURL);
            }
            else
            {
                Console.WriteLine("VOU PEDIR AO " + targetPuppetMasterURL + "PARA CRIAR O " + serviceURL);
                IPuppetMasters puppetMaster = (IPuppetMasters)Activator.GetObject(typeof(IWorker), targetPuppetMasterURL);
                puppetMaster.startWorker(workerInfo);
            }
        }

        public static void submit()
        {            
            Process.Start(@"..\..\..\Client\bin\Debug\Client.exe", String.Join(" ", splittedInstruction));
        }

        public static void wait()
        {
            Thread.Sleep(int.Parse(splittedInstruction[1]) * 1000);
        }

        private static void getStatus()
        {
            foreach (KeyValuePair<string, string> entry in workers)
            {
                IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), entry.Value);
                target.getStatus();
            }
        }

        private static void slowWorker()
        {
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.slowWorker(int.Parse(splittedInstruction[2]) * 1000);
        }

        private static void freezeWorker()
        {
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.freezeWorker();
        }

        private static void unfreezeWorker()
        {
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.unfreezeWorker();
        }

        private static void freezeJobTracker()
        {
            IWorker target = (IWorker)Activator.GetObject(typeof(IWorker), workers[splittedInstruction[1]]);
            target.freezeJobTracker();
        }

        private static void unfreezeJobTracker()
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