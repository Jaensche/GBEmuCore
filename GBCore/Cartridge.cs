namespace GBCore
{
    public class Cartridge
    {
        byte[] _rom;

        public Cartridge()
        {
            _rom = new byte[0x8000];
        }

        public byte Read(ushort addr)
        {
            return _rom[addr];
        }

        public void Write(ushort addr, byte data)
        {
            return;
        }

        public void LoadCode(byte[] code)
        {          
            _rom = code;
        }
    } 
}