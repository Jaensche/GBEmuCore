using GBCore.Graphics;
using System;

namespace GBCore
{
    public class Memory
    {
        public byte[] _memory;
        public PPU _ppu;
        public Timer _timer;
        public Cartridge _cart;

        public Input _input;

        public Memory(int size) 
        {
            _memory = new byte[size];
        }

        public byte Read(ushort addr)
        {
            if (addr == 0xFF04)
            {
                return _timer.DivValue;                
            }

            // reroute VRAM reads to PPU
            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                return _ppu.ReadVram(addr);
            }

            // reroute Rom reads to Cartride
            if (addr >= 0x0000 && addr <= 0x7FFF)
            {
                return _cart.Read(addr);    
            }

            // reroute to Input
            if (addr == 0xFF00)
            {
                return _input.Read();
            }

            return _memory[addr];
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

            // reroute VRAM writes to PPU
            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
               _ppu.WriteVram(addr, data);
               return;
            }

            // reroute Rom writes to Cartride
            if (addr >= 0x0000 && addr <= 0x7FFF)
            {
                _cart.Write(addr, data);
                return;
            }

            // reroute to Input
            if (addr == 0xFF00)
            {
                _input.Write(data);
            }

            _memory[addr] = data;
        }
    }
}
