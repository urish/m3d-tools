using System;
using System.Configuration;
using System.Reflection;
using Micro3DSpooler.Spooler_Server;
using Micro3DSpooling.Common;
using System.Collections.Generic;
using System.IO;

namespace Processor
{

    class MainClass
    {
        string filamentType = "PLA";
        int printingTemperature = 215;
        float backlashX = 0;
        float backlashY = 0;
        float backlashSpeed = 0;
        float badOffset = 0;
        float badOffsetFrontLeft = 0;
        float badOffsetFrontRight = 0;
        float badOffsetBackLeft = 0;
        float badOffsetBackRight = 0;
        bool waveBonding = false;

        private Assembly spoolerAssembly;

        public MainClass()
        {
            spoolerAssembly = Assembly.LoadFrom("Micro3DSpooler.exe");

            // Load configuration from app.config
            var reader = new AppSettingsReader();
            filamentType = (String)reader.GetValue("filamentType", typeof(String));
            printingTemperature = (int)reader.GetValue("printingTemperature", typeof(int));
            backlashX = (float)reader.GetValue("backlashX", typeof(float));
            backlashY = (float)reader.GetValue("backlashY", typeof(float));
            backlashSpeed = (float)reader.GetValue("backlashSpeed", typeof(float));
            badOffset = (float)reader.GetValue("badOffset", typeof(float));
            badOffsetFrontLeft = (float)reader.GetValue("badOffsetFrontLeft", typeof(float));
            badOffsetBackLeft = (float)reader.GetValue("badOffsetBackLeft", typeof(float));
            badOffsetFrontRight = (float)reader.GetValue("badOffsetFrontRight", typeof(float));
            badOffsetBackRight = (float)reader.GetValue("badOffsetBackRight", typeof(float));
            waveBonding = (bool)reader.GetValue("waveBonding", typeof(bool));
        }

        private IPreprocessor createProcessor(String name)
        {
            return createProcessor(name, null);
        }

        private IPreprocessor createProcessor(String className, Object[] args)
        {
            return (IPreprocessor)spoolerAssembly.CreateInstance("Micro3DSpooler.Spooler_Server." + className,
                false, BindingFlags.CreateInstance, null, args, null, new object[] { });
        }

        private JobDetails getJobDetails()
        {
            var jobDetails = new JobDetails();
            jobDetails.ideal_temperature = printingTemperature;
            jobDetails.filament_type = FilamentProfile.StringToFilamentType(filamentType);
            return jobDetails;
        }

        private PrinterDetailsEX getPrinterDetails()
        {
            var printerDetails = new PrinterDetailsEX();
            printerDetails.BACKLASH_SPEED = backlashSpeed;
            printerDetails.BACKLASH_X = backlashX;
            printerDetails.BACKLASH_Y = backlashY;
            printerDetails.ENTIRE_Z_HEIGHT_OFFSET = badOffset;
            printerDetails.CORNER_HEIGHT_BACK_RIGHT_OFFSET = badOffsetBackRight;
            printerDetails.CORNER_HEIGHT_BACK_LEFT_OFFSET = badOffsetBackLeft;
            printerDetails.CORNER_HEIGHT_FRONT_LEFT_OFFSET = badOffsetFrontLeft;
            printerDetails.CORNER_HEIGHT_FRONT_RIGHT_OFFSET = badOffsetFrontRight;
            return printerDetails;
        }

        public bool RunProcessor(IPreprocessor processor, String inputFile, String outputFile)
        {
            var reader = new GCodeReader(inputFile);
            var writer = new GCodeWriter(outputFile);
            try
            {
                return processor.ProcessGCode(reader, writer, getPrinterDetails(), getJobDetails());
            }
            finally
            {
                reader.Close();
                writer.Close();
            }
        }

        public void transform(String inputFile, String outputFile)
        {
            var processors = new List<IPreprocessor>();

            // Prepare processors
            processors.Add(createProcessor("GCodeInitializationPreprocessor"));
            if (waveBonding)
            {
                processors.Add(createProcessor("BondingPreprocessor"));
            }
            processors.Add(createProcessor("ThermalBondingPreprocessor", new object[] { waveBonding }));
            processors.Add(createProcessor("BedCompensationPreprocessor"));
            processors.Add(createProcessor("BackLashPreprocessor"));
            processors.Add(createProcessor("SimpleFeedRateFixer"));

            Console.WriteLine("Processing file: {0}", inputFile);
            String intermediateInput = inputFile;
            String intermediateOutput = null;
            int step = 0;
            foreach (var processor in processors)
            {
                ++step;
                intermediateOutput = inputFile + "-step" + step + ".gcode";
                Console.WriteLine("-> {0}: {1}", processor.GetType().Name, RunProcessor(processor, intermediateInput, intermediateOutput));
                intermediateInput = intermediateOutput;
            }

            File.Copy(intermediateOutput, outputFile, true);
            Console.WriteLine("Wrote output to {0}", outputFile);
        }

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: Processor.exe <inputfile.gcode> <outputfile.gcode>");
            }
            else
            {
                new MainClass().transform(args[0], args[1]);
            }
        }
    }
}
