namespace GBCore.Graphics
{
    public class Sprite
    {
        public byte Y { get; set; }
        public byte X { get; set; }
        public byte TileNumber { get; set; }
        public byte Flags { get; set; }

        public Sprite(byte y, byte x, byte tileNumber, byte flags)
        {
            Y = y;
            X = x;
            TileNumber = tileNumber;
            Flags = flags;
        }
    }
}