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
        }

        public static void Main(string[] args)
        {
            bool traceEnabled = false;
            string rom = string.Empty;
            long maxInstr = 0;

            var bla = Parser.Default
                .ParseArguments<Options>(args);

            Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    if (options.Verbose)
                    {
                        traceEnabled = true;
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

                if(count == 1239515)
                {
                }
            }
        }
    }
}
