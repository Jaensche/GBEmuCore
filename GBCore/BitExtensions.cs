namespace GBCore
{
    public static class BitExtensions
    {
        public static void SetBit(ref this byte value, int position, bool one)
        {
            if(one)
            {
                value = (byte)(value | (byte)(1 << position));
            }
            else
            {
                value = (byte)(value & ~(byte)(1 << position));
            }
        }

        public static bool BitSet(this byte value, int position)
        {
            // Return whether bit at position is set to 1.
            return (value & (1 << position)) != 0;
        }       

        public static int Bit(this byte value, int position)
        {
            return (value & (1 << position))>>position;
        }
    }
}