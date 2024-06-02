namespace GBCore
{
    internal class Gameboy
    {
        private readonly Memory _ram;        
        private readonly CPU _cpu;
        private readonly PPU _ppu;

        public Gameboy(bool traceEnabled)
        {
            _ram = new Memory(0x10000);
            _cpu = new CPU(traceEnabled, _ram);
            _ppu = new PPU(_ram);
        }

        public void Run(byte[] code, long maxInstr = 0)
        {
            _cpu.Load(code);

            long count = 0;
            while (count < maxInstr || maxInstr == 0)
            {
                _cpu.Cycle();
                count++;

                if (count == 152007)
                {
                }
            }            
        }
    }
}
