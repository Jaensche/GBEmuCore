using GBCore.Graphics;
using System.Diagnostics;
using System.Threading;

namespace GBCore
{
    internal class Gameboy
    {
        private readonly Memory _ram;        
        private readonly CPU _cpu;
        private readonly PPU _ppu;
        private readonly Screen _screen;

        const double CLOCK_SPEED = 4194304.0; // Hz
        const double FRAME_RATE = 59.73;
        const long CYCLES_PER_FRAME = (int)(CLOCK_SPEED / FRAME_RATE); // 70224

        Stopwatch stopwatch = new Stopwatch();
        double targetFrameTime = 1000.0 / FRAME_RATE; // in milliseconds

        public Gameboy(bool traceEnabled, ushort progMemStart)
        {
            _ram = new Memory(0x10000);
            _cpu = new CPU(traceEnabled, _ram, progMemStart);
            _ppu = new PPU(_ram);
            _screen = new Screen();
        }

        public void Run(byte[] code, long maxInstr = 0)
        {
            try
            {                
                _screen.Setup();
                _cpu.Load(code);

                long count = 0;
                long cpuCycles = 0;
                long cyclesThisFrame = 0;
                while (count < maxInstr || maxInstr == 0)
                {
                    cyclesThisFrame = 0;

                    while (cyclesThisFrame < CYCLES_PER_FRAME)
                    {
                        cpuCycles = _cpu.ExecuteNext();
                        for (int i = 0; i < cpuCycles; i++)
                        {
                            _ppu.Cycle();
                        }

                        cyclesThisFrame += cpuCycles;
                    }

                   _screen.Render(_ppu.ScreenBuffer);

                    if (_screen.PollEvents() == -1)
                    {
                        break;
                    }

                    double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                    if (elapsedMs < targetFrameTime)
                    {
                        int delay = (int)(targetFrameTime - elapsedMs);
                        if (delay > 0)
                            Thread.Sleep(delay);
                    }

                    count++;
                }
            }
            finally 
            { 
                _screen.CleanUp(); 
            }
        }
    }
}
