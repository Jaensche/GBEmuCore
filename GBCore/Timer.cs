using System;

namespace GBCore
{
    internal class Timer
    {
        private readonly Memory _ram;

        private const ushort DIV = 0xFF04;
        private const ushort TIMA = 0xFF05;
        private const ushort TMA = 0xFF06;
        private const ushort TAC = 0xFF07;
        int timerDivider = 0;

        public const ushort IRQ_FLAGS = 0xFF0F;

        public Timer(Memory ram)
        {
            _ram = ram;
        }

        public void TimerTick(long ticks)
        {
            //for (long i = 0; i < ticks; i++)
            {
                timerDivider++;

                byte tac = _ram.Read(TAC);
                byte inputClockSelect = (byte)(tac & 0b00000011);
                bool timerEnable = (tac & 0b00000100) > 0;

                if (timerEnable)
                {
                    byte tima = _ram.Read(TIMA);

                    if (tima == 0xFF)
                    {
                        // Reset TIMA and raise timer interrupt
                        byte irqFlags = _ram.Read(IRQ_FLAGS);
                        _ram.Write(IRQ_FLAGS, (byte)((byte)irqFlags | (byte)IrqFlags.Timer));
                        _ram.Write(TIMA, _ram.Read(TMA));
                    }
                    else if (timerDivider % 64 == 0 && inputClockSelect == 0b00) // 1024
                    {
                        _ram.Write(TIMA, (byte)(tima + 1));
                    }
                    else if (timerDivider % 32 == 0 && inputClockSelect == 0b11) // 256
                    {
                        _ram.Write(TIMA, (byte)(tima + 1));
                    }
                    else if (timerDivider % 16 == 0 && inputClockSelect == 0b10) // 64
                    {
                        _ram.Write(TIMA, (byte)(tima + 1));
                    }
                    else if (inputClockSelect == 0b01) // 16
                    {
                        _ram.Write(TIMA, (byte)(tima + 1));
                    }
                }

                // 16
                _ram.Write(DIV, (byte)(_ram.Read(DIV) + 1));

                if (timerDivider >= 64)
                {
                    timerDivider = 0;
                }
            }
        }
    }
}
