using GBCore.Graphics;
using System.Diagnostics;
using System.Threading;

namespace GBCore
{
    internal class Gameboy
    {
        private readonly Memory _memory;      
        private readonly Timer _timer;  
        private readonly CPU _cpu;
        private readonly PPU _ppu;
        private readonly Cartridge _cart;
        private readonly Screen _screen;

        const double CLOCK_SPEED = 4194304.0; // Hz
        const double FRAME_RATE = 59.73;
        const long CYCLES_PER_FRAME = (int)(CLOCK_SPEED / FRAME_RATE); // 70224

        Stopwatch stopwatch = new Stopwatch();
        double targetFrameTime = 1000.0 / FRAME_RATE; // in milliseconds
        
        public Gameboy(bool traceEnabled, ushort progMemStart)
        {
            _memory = new Memory(0x10000);
            _timer = new Timer(_memory);
            _ppu = new PPU(_memory);
            _cart = new Cartridge();
            _memory._ppu = _ppu;
            _memory._timer = _timer;
            _memory._cart = _cart;

            _cpu = new CPU(traceEnabled, _memory, progMemStart);            
            _screen = new Screen();
        }

        public void Run(byte[] code, long maxInstr = 0)
        {
            try
            {     
                _cart.LoadCode(code);     
                _screen.Setup();                

                long cpuCycles = 0;
                long cyclesThisFrame = 0;
                while (true)
                {
                    cyclesThisFrame = 0;

                    while (cyclesThisFrame < CYCLES_PER_FRAME)
                    {
                        cpuCycles = _cpu.ExecuteNext();
                        for (int i = 0; i < cpuCycles; i++)
                        {
                            _ppu.Cycle();
                        }
                        _timer.TimerTick(cpuCycles);

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
                }
            }
            finally 
            { 
                _screen.CleanUp(); 
            }
        }
    }
}
