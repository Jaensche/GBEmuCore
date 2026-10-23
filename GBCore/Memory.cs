using GBCore.Graphics;
using System;

namespace GBCore
{
    public class Memory
    {
        public byte[] _memory;
        public PPU _ppu;
        public Timer _timer;

        public Memory(int size) 
        {
            _memory = new byte[size];
        }

        public byte Read(ushort addr)
        {
            // block reads by PPU
            //if (addr >= 0x8000 && addr <= 0x9FFF)
            //{
            //    if (!ppu.IsVramAccessible)
            //    {
            //        return 0xFF;
            //    }
            //}

            if (addr == 0xFF04)
            {
                return _timer.DivValue;                
            }

            byte data = _memory[addr];

            return data;
        }

        byte SB;
        byte SC;
        

        public void Write(ushort addr, byte data)
        {
            //DIV - Divider Register - Reset Timer on all writes
            if (addr == 0xFF04)
            {
                _timer.Reset();

                //nothing to write to memory
                return;
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

            // block writes by PPU
            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                if (!_ppu.IsVramAccessible)
                {
                    return;
                }
            }

            _memory[addr] = data;
        }
    }
}
