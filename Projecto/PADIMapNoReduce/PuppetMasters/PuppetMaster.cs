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
        static int thisPuppetMasterPort = 20001;
        static string thisPuppetMasterURL = "tcp://localhost:" + thisPuppetMasterPort + "/PM";
        static Dictionary<string, string> workers = new Dictionary<string, string>();
        static string[] splittedInstruction;


        [STAThread]
        static void Main(string[] args)
        {
            startGUI();

            TcpChannel channel = new TcpChannel(thisPuppetMasterPort);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(PuppetMasterServices), "PM", WellKnownObjectMode.Singleton);

            //Console.ReadLine();
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
            string puppetMasterURL = workerInfo[2];
            if (puppetMasterURL.Equals(thisPuppetMasterURL))
            {
                string serviceURL = workerInfo[3];
                if (workerInfo.Length == 4)
                {
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL);
                }
                else
                {
                    string entryURL = workerInfo[4];
                    Process.Start(@"..\..\..\Worker\bin\Debug\Server.exe", serviceURL + " " + entryURL);
                }
                string workerID = workerInfo[1];
                workers.Add(workerID, serviceURL);
            }
            else
            {
                IPuppetMasters puppetMaster = (IPuppetMasters)Activator.GetObject(typeof(IWorker), puppetMasterURL);
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