namespace GBCore
{
    internal class Gameboy
    {
        private readonly Memory _ram;        
        private readonly CPU _cpu;
        private readonly Graphics.PPU _ppu;

        public Gameboy(bool traceEnabled)
        {
            _ram = new Memory(0x10000);
            _cpu = new CPU(traceEnabled, _ram);
            _ppu = new Graphics.PPU(_ram);
        }

        public void Run(byte[] code, long maxInstr = 0)
        {
            _cpu.Load(code);

            long count = 0;
            long cpuCycles = 0;
            while (count < maxInstr || maxInstr == 0)
            {
                cpuCycles = _cpu.ExecuteNext();
                for (int i = 0; i < cpuCycles*16; i++) // TODO: Why is this multiplier needed to make the logo scroll?
                {
                    _ppu.Cycle();
                }
                count++;
            }            
        }
    }
}
