using System;

namespace GBCore
{
    public class Memory
    {
        public byte[] _ram;

        public Memory(int size) 
        {
            _ram = new byte[size];
        }

        public byte Read(ushort addr)
        {
            byte data = _ram[addr];

            return data;
        }

        byte SB;
        byte SC;

        public void Write(ushort addr, byte data)
        {
            //DIV - Divider Register - Always set to 0x00
            if (addr == 0xFF04)
            {
                data = 0x00;
            }

            if (addr == 0xFF42)
            {

            }

            if (addr == 0xFF01) // SB
            {
                SB = data;
            }

            if (addr == 0xFF02) // SC
            {
                SC = data;
            
                if ((SC & 0x80) != 0)
                {
                    Console.Write((char)SB);
                    SC &= 0x7F;
                }
            }        

            _ram[addr] = data;
        }
    }
}
