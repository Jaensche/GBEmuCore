using System.IO;
using System;
using CommandLine;
using System.Collections.Generic;

namespace GBCore
{
    public class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose")]
            public bool Verbose { get; set; }

            [Option('r', "rom", Required = true, HelpText = "Rom file to run")]
            public string romFile { get; set; }
        }

        public static void Main(string[] args)
        {
            bool traceEnabled = false;
            string rom = string.Empty;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.Verbose)
                       {
                           traceEnabled = true;
                       }
                       rom = o.romFile;
                   });

            CPU cpu = new CPU(traceEnabled);

            cpu.Load(File.ReadAllBytes(rom));

            long count = 0;
            while (count < 1763388)
            {
                cpu.Cycle();
                count++;

                if(count == 1233675)
                {
                }
            }
        }
    }
}
