using System.IO;
using System;

namespace GBCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CPU cpu = new CPU();

            cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\individual\\06-ld r,r.gb"));

            //cpu.Load(new byte[] { 0x00, 0x01, 0xBB, 0xAA, 0xC5, 0xF1 });
            //cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\cpu_instrs.gb"));

            while (true)
            {
                cpu.Cycle();
                //Console.ReadKey();
            }
        }
    }
}
