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

            [Option('s', "start", Required = false, HelpText = "Address to start executing code from")]
            public ushort ProgMemStart { get; set; }

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
            ushort progMemStart = 0;

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
                    progMemStart = options.ProgMemStart;
                });

            byte[] nintendoLogo =
            {
                0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B,
                0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
                0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E,
                0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
                0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC,
                0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
            };
            byte[] code = new byte[0x10000]; // 32KB dummy ROM
            Array.Copy(nintendoLogo, 0, code, 0x0104, nintendoLogo.Length);

            byte[] boot = File.ReadAllBytes(rom);
            Array.Copy(boot, 0, code, 0x0000, boot.Length);

            Gameboy gameboy = new Gameboy(traceEnabled, progMemStart);
            gameboy.Run(code, maxInstr);

            if(writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
            }
        }

        private static StreamWriter ConsoleToFile()
        {
            FileStream fileStream;
            StreamWriter streamWriter;
            try
            {
                fileStream = new FileStream("out.txt", FileMode.Create, FileAccess.Write);
                streamWriter = new StreamWriter(fileStream);
                Console.SetOut(streamWriter);

                return streamWriter;
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
