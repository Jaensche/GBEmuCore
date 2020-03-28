using System.IO;
using System;

namespace GBCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CPU cpu = new CPU(10);

            //cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\individual\\06-ld r,r.gb"));
            cpu.Load(File.ReadAllBytes("R:\\DEV\\GBEmuCore\\blargg\\cpu_instrs\\individual\\01-special.gb"));


            while (true)
            {
                cpu.Cycle();
                //Console.ReadKey();
            }
        }
    }
}
