using System;

namespace GBCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            byte a = 255;
            byte b = 1;

            Console.WriteLine(a + b > 0xFF);
        }
    }
}
