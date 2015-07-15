using System;
using System.IO.Ports;
using Micro3DSpooler.Spooler_Server;
using Micro3DSpooling.Sockets;
using System.Threading;
using RepetierHost.model;
using Micro3DSpooling.Common;
using System.Reflection;
using System.Collections.Generic;

namespace PrintGCode
{
    class BroadcastReceiver: IBroadcastServer
    {
        public void BroadcastMessage(string message)
        {
            Console.WriteLine("Spooler message: {0}", message);
        }
    }

    class MainClass
    {
        public const String logFileName = "print.log";

        public void injectPrintJob(FirmwareConnection connection, String gcodeFile) {
            JobParams jobParams = new JobParams();
            Object printerJob = connection.GetType().Assembly.CreateInstance("Micro3DSpooler.Spooler_Server.PrinterJob", false, BindingFlags.CreateInstance, null, new object[] {jobParams, "user"}, null, null);
            Type printerJobType = printerJob.GetType();
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            printerJob.GetType().GetField("gcodefilename", bindingFlags).SetValue(printerJob, gcodeFile);
            printerJob.GetType().InvokeMember("ProcessIncomingJob", BindingFlags.InvokeMethod | bindingFlags, null, printerJob, null);
            Object jobList = connection.GetType().GetField("jobslist", bindingFlags).GetValue(connection);
            jobList.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, jobList, new object[]{ printerJob });
        }

        public void printFile(String comPort, String gcodeFile)
        {
            EmbeddedFirmwareInfo.AddEmbeddedFirmwareInfo("test", "test", 0, 0);

            var spooler = new SpoolerServer();
            try
            {
                SpoolerServer.log = new Logger(logFileName);
                var connector = new PrinterConnector(spooler, new  BroadcastReceiver());

                // We open a first connection to obtain the serial number
                FirmwareConnection connection = (FirmwareConnection)connector.ConnectToPrinter(comPort);
                if (connection == null)
                {
                    Console.Error.WriteLine("Printer connection failed. Please verify that your printer is connected on {0}.", comPort);
                    return;
                }

                // Wait for the printer to come online
                while (connection.SerialNumber.ToString().Equals("00-00-00-00-00-000-000")) {
                    Thread.Sleep(100);
                }

                Console.WriteLine("Printing to {0}", connection.SerialNumber.ToString());
                Console.WriteLine("Starting to print {0}...", gcodeFile);
                injectPrintJob(connection, gcodeFile);
                connection.SetBedClear();
                while (connection.GetJobsCount() > 0) {
                    Console.Write("JobStatus: {0}, Completed: {1:0.00}%         \r", connection.GetJob(0).Status, 100 * connection.GetJob(0).PercentComplete);
                    Thread.Sleep(1000);
                }
            }
            finally
            {
                spooler.CloseConnections();
            }
        }


        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: Processor.exe <serialPort> <processedFile.gcode>");
            }
            else
            {
                new MainClass().printFile(args[0], args[1]);
            }
        }
    }
}
