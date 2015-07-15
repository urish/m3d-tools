using System;
using System.IO.Ports;
using Micro3DSpooler.Spooler_Server;
using Micro3DSpooling.Sockets;
using System.Threading;
using RepetierHost.model;
using Micro3DSpooling.Common;
using System.Reflection;

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

        public void printFile(String comPort, String gcodeFile)
        {
            System.IO.StreamReader file = 
                new System.IO.StreamReader(gcodeFile);

            EmbeddedFirmwareInfo.AddEmbeddedFirmwareInfo("test", "test", 0, 0);

            var spooler = new SpoolerServer();
            try
            {
                SpoolerServer.log = new Logger(logFileName);
                var connector = new PrinterConnector(spooler, new  BroadcastReceiver());
                FirmwareConnection connection = (FirmwareConnection)connector.ConnectToPrinter(comPort);
                if (connection == null)
                {
                    Console.Error.WriteLine("Printer connection failed. Please verify that your printer is connected on {0}.", comPort);
                    return;
                }

                Thread.Sleep(1000); // TOOD replace with a better method of waiting for printer ready
                String serialNumber = connection.SerialNumber.ToString();
                connection.Shutdown();

                Console.WriteLine("Printing to {0}", serialNumber);
                var stats = spooler.PersistantStorage.History.GetStats(serialNumber);
                stats.GantryClipsRemoved = true;
                spooler.PersistantStorage.History.UpdateStats(serialNumber, stats);

                connection = (FirmwareConnection)connector.ConnectToPrinter(comPort);
                Thread.Sleep(1000); // TOOD replace with a better method of waiting for printer ready
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    if (line.Length > 0 && line[0] != ';')
                    {
                        connection.WriteManualCommand(line);
                    }
                    while (connection.IsWorking) {
                        Thread.Sleep(1);
                    }
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
