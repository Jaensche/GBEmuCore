using System;

namespace GBCore
{
    public class Timer
    {
        private readonly Memory _memory;

        private const ushort TIMA = 0xFF05;
        private const ushort TMA = 0xFF06;
        private const ushort TAC = 0xFF07;

        public Timer(Memory memory)
        {
            _memory = memory;
        }

        private ushort internalDivider = 0;

        public byte DivValue 
        { 
            get
            {
                return (byte)(internalDivider >> 8);
            }
        }

        private bool lastTimerBit;

        public void TimerTick(long cycles)
        {
            for (int i = 0; i < cycles; i++)
            {
                internalDivider++;

                byte tac = _memory.Read(TAC);
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
                    byte tima = _memory.Read(TIMA);
                    tima++;

                    if (tima == 0x00) // overflow (was 0xFF before increment)
                    {
                        // Load TMA and request interrupt
                        tima = _memory.Read(TMA);
                        byte irq = _memory.Read(CPU.IRQ_FLAGS);
                        _memory.Write(CPU.IRQ_FLAGS, (byte)(irq | (byte)IrqFlags.Timer));
                    }

                    _memory.Write(TIMA, tima);
                }

                lastTimerBit = currentTimerBit;
            }
        }

        public void Reset()
        {
            internalDivider = 0;
        }
    }
}
