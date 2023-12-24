using System.IO;
using System;

namespace GBCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool traceEnabled = false;
            if(args.Length > 0)
            {
                traceEnabled = true;
            }

            CPU cpu = new CPU(traceEnabled);

            cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\individual\\04-op r,imm.gb"));

            //cpu.Load(new byte[] { 0x00, 0x01, 0xBB, 0xAA, 0xC5, 0xF1 });
            //cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\cpu_instrs.gb"));

            long count = 0;
            while (count < 1262766)
            {
                cpu.Cycle();
                count++;
                //Console.ReadKey();

                if(count == 1069205)
                {
                }
            }
        }
    }
}
