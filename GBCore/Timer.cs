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

        public const ushort IRQ_FLAGS = 0xFF0F;

        public Timer(Memory ram)
        {
            _ram = ram;
        }

        private ushort internalDivider = 0;

        // Previous state of the bit we watch for rising/falling edges
        private bool lastTimerBit;

        public void TimerTick(long cycles)
        {
            // Each CPU instruction calls this with the number of cycles it took
            for (int i = 0; i < cycles; i++)
            {
                internalDivider++;

                // Update visible DIV register (upper 8 bits of divider)
                _ram.Write(DIV, (byte)(internalDivider >> 8));

                byte tac = _ram.Read(TAC);
                bool timerEnable = (tac & 0b00000100) != 0;
                int clockSelect = tac & 0b11;

                // Determine which bit of the divider the timer listens to
                int bitIndex = clockSelect switch
                {
                    0b00 => 9,
                    0b01 => 3,
                    0b10 => 5,
                    0b11 => 7,
                    _ => 9
                };

                bool currentTimerBit = ((internalDivider >> bitIndex) & 1) != 0;

                // On falling edge of this bit, increment TIMA (only if enabled)
                if (timerEnable && lastTimerBit && !currentTimerBit)
                {
                    byte tima = _ram.Read(TIMA);
                    tima++;

                    if (tima == 0x00) // overflow (was 0xFF before increment)
                    {
                        // Load TMA and request interrupt
                        tima = _ram.Read(TMA);
                        byte irq = _ram.Read(IRQ_FLAGS);
                        _ram.Write(IRQ_FLAGS, (byte)(irq | (byte)IrqFlags.Timer));
                    }

                    _ram.Write(TIMA, tima);
                }

                lastTimerBit = currentTimerBit;
            }
        }
    }
}
