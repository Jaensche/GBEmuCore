namespace GBCore
{
    public static class BitExtensions
    {
        public static void SetBitTo1(ref this byte value, int position)
        {
            // Set a bit at position to 1.
            value = (byte)(value | (byte)(1 << position));
        }

        public static void SetBitTo0(ref this byte value, int position)
        {
            // Set a bit at position to 0.
            value = (byte)(value & ~(byte)(1 << position));
        }

        public static bool IsBitSetTo1(this byte value, int position)
        {
            // Return whether bit at position is set to 1.
            return (value & (1 << position)) != 0;
        }

        public static bool IsBitSetTo0(this byte value, int position)
        {
            // If not 1, bit is 0.
            return !IsBitSetTo1(value, position);
        }
    }
}