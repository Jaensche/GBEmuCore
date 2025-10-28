namespace GBCore
{
    public class Memory
    {
        public byte[] _ram;

        public Memory(int size) 
        {
            _ram = new byte[size];
        }

        public byte Read(ushort addr)
        {
            byte data = _ram[addr];

            return data;
        }

        public void Write(ushort addr, byte data)
        {
            //DIV - Divider Register - Always set to 0x00
            if (addr == 0xFF04)
            {
                data = 0x00;
            }

            if(addr == 0xFF42)
            {

            }

            _ram[addr] = data;
        }
    }
}
