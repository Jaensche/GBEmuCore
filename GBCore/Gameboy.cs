namespace GBCore
{
    internal class Gameboy
    {
        private readonly CPU _cpu;
        private readonly PPU _ppu;

        public byte[] _ram = new byte[0x10000];
        public byte[] _vram = new byte[0x10000];

        public Gameboy(bool traceEnabled)
        {
            _cpu = new CPU(traceEnabled, _ram);
            _ppu = new PPU();
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
