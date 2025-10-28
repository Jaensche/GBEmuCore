using GBCore.Graphics;

namespace GBCore
{
    internal class Gameboy
    {
        private readonly Memory _ram;        
        private readonly CPU _cpu;
        private readonly PPU _ppu;
        private readonly Screen _screen;

        public Gameboy(bool traceEnabled)
        {
            _ram = new Memory(0x10000);
            _cpu = new CPU(traceEnabled, _ram);
            _ppu = new PPU(_ram);
            _screen = new Screen();
        }

        public void Run(byte[] code, long maxInstr = 0)
        {
            // TODO: Slow down to the correct clock speed

            try
            {                
                _screen.Setup();
                _cpu.Load(code);

                long count = 0;
                long cpuCycles = 0;
                while (count < maxInstr || maxInstr == 0)
                {
                    cpuCycles = _cpu.ExecuteNext();
                    for (int i = 0; i < cpuCycles * 64; i++)
                    {
                        _ppu.Cycle();
                    }

                    if (_ppu.readyToRender)
                    {
                        _screen.Render(_ppu.ScreenBuffer);
                        _ppu.ScreenBuffer = new int[160, 144];

                        if (_screen.PollEvents() == -1)
                        {
                            break;
                        }
                        _ppu.readyToRender = false;
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
