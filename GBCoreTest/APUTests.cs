using GBCore;
using NUnit.Framework;

namespace GBCoreTest
{
    public class APUTests
    {
        [TestCase(0b10100101, true, APU.Direction.Right, ExpectedResult = 0b11010010, TestName = "RRC")]
        [TestCase(0b10100101, true, APU.Direction.Left, ExpectedResult = 0b01001011, TestName = "RLC")]
        [TestCase(0b10100101, false, APU.Direction.Left, ExpectedResult = 0b01001010, TestName = "RL C0")]
        [TestCase(0b10100101, false, APU.Direction.Right, ExpectedResult = 0b01010010, TestName = "RR C0")]
        public byte Rotate_Carry0_CorrectResult(byte input, bool carry, APU.Direction direction)
        {
            byte flags = 0;
            return APU.ROT(input, ref flags, carry, direction);
        }

        [TestCase(0b10100101, false, APU.Direction.Left, ExpectedResult = 0b01001011, TestName = "RL C1")]
        [TestCase(0b10100101, false, APU.Direction.Right, ExpectedResult = 0b11010010, TestName = "RR C1")]
        public byte Rotate_Carry1_CorrectResult(byte input, bool carry, APU.Direction direction)
        {
            byte flags = 0;
            flags |= (byte)Flags.C;

            return APU.ROT(input, ref flags, carry, direction);
        }

        [TestCase(0b11111110, true, APU.Direction.Right, ExpectedResult = 0, TestName = "RRC -> C0")]
        [TestCase(0b00000001, true, APU.Direction.Right, ExpectedResult = (byte)Flags.C, TestName = "RRC -> C1")]
        [TestCase(0b01111111, true, APU.Direction.Left, ExpectedResult = 0, TestName = "RLC -> C0")]
        [TestCase(0b11000000, true, APU.Direction.Left, ExpectedResult = (byte)Flags.C, TestName = "RLC -> C1")]
        [TestCase(0b01111111, false, APU.Direction.Left, ExpectedResult = 0, TestName = "RL -> C0")]
        [TestCase(0b11000000, false, APU.Direction.Left, ExpectedResult = (byte)Flags.C, TestName = "RL -> C1")]
        [TestCase(0b11111110, false, APU.Direction.Right, ExpectedResult = 0, TestName = "RR -> C0")]
        [TestCase(0b00000011, false, APU.Direction.Right, ExpectedResult = (byte)Flags.C, TestName = "RR -> C1")]
        public byte Rotate_CorrectCarry(byte input, bool carry, APU.Direction direction)
        {
            byte flags = 0;

            APU.ROT(input, ref flags, carry, direction);
            
            return flags;
        }

        [TestCase(0b11110000, ExpectedResult = 0b00001111, TestName = "SWAP")]
        [TestCase(0b00001111, ExpectedResult = 0b11110000, TestName = "SWAP")]
        [TestCase(0b00111100, ExpectedResult = 0b11000011, TestName = "SWAP")]
        public byte Swap(byte input)
        {
            return APU.SWP(input);
        }
    }
}
