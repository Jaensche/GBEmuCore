using System;
using System.IO;
using CommandLine;

namespace GBCore
{
    public class Program
    {
        public class Options
        {
            [Option('r', "rom", Required = true, HelpText = "Rom file to run")]
            public string RomFile { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose")]
            public bool Verbose { get; set; }            

            [Option('i', "instr", Required = false, HelpText = "Maximum instruction count to execute")]
            public long MaxInstructions { get; set; }

            [Option('f', "file", Required = false, HelpText = "Write ouput to out.txt")]
            public bool WritToFile { get; set; }
        }

        public static void Main(string[] args)
        {
            bool traceEnabled = false;
            string rom = string.Empty;
            long maxInstr = 0;

            var bla = Parser.Default
                .ParseArguments<Options>(args);

            StreamWriter writer = null;
            Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    if (options.Verbose)
                    {
                        traceEnabled = true;
                    }
                    if (options.WritToFile)
                    {
                        writer = ConsoleToFile();
                    }
                    maxInstr = options.MaxInstructions;
                    rom = options.RomFile;
                });

            CPU cpu = new CPU(traceEnabled);

            cpu.Load(File.ReadAllBytes(rom));

            long count = 0;
            while (count < maxInstr)
            {
                cpu.Cycle();
                count++;

                if(count == 1249858)
                {
                }
            }

            if(writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
            }
        }

        private static StreamWriter ConsoleToFile()
        {
            FileStream ostrm;
            StreamWriter writer;
            try
            {
                ostrm = new FileStream("out.txt", FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(ostrm);
                Console.SetOut(writer);

                return writer;
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open file");
                Console.WriteLine(e.Message);
                return null;
            }                        
        }
    }
}
