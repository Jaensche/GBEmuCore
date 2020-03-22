using GBCore;
using NUnit.Framework;

namespace GBCoreTest
{
    public class CPUTests
    {
        CPU cpu;

        [SetUp]
        public void Setup()
        {
            cpu = new CPU(500);
        }
            
        [TestCase(0b10100101, true, CPU.Direction.Right, ExpectedResult = 0b11010010, TestName = "RRC")]
        [TestCase(0b10100101, true, CPU.Direction.Left, ExpectedResult = 0b01001011, TestName = "RLC")]
        [TestCase(0b10100101, false, CPU.Direction.Left, ExpectedResult = 0b01001010, TestName = "RL C0")]
        [TestCase(0b10100101, false, CPU.Direction.Right, ExpectedResult = 0b01010010, TestName = "RR C0")]
        public byte Rotate_Carry0_CorrectResult(byte input, bool carry, CPU.Direction direction)
        {
            cpu.FlagC = false;
            return cpu.Rot(input, carry, direction);
        }

        [TestCase(0b10100101, false, CPU.Direction.Left, ExpectedResult = 0b01001011, TestName = "RL C1")]
        [TestCase(0b10100101, false, CPU.Direction.Right, ExpectedResult = 0b11010010, TestName = "RR C1")]
        public byte Rotate_Carry1_CorrectResult(byte input, bool carry, CPU.Direction direction)
        {
            cpu.FlagC = true;
            return cpu.Rot(input, carry, direction);
        }

        [TestCase(0b11111110, true, CPU.Direction.Right, ExpectedResult = false, TestName = "RRC -> C0")]
        [TestCase(0b00000001, true, CPU.Direction.Right, ExpectedResult = true, TestName = "RRC -> C1")]
        [TestCase(0b01111111, true, CPU.Direction.Left, ExpectedResult = false, TestName = "RLC -> C0")]
        [TestCase(0b10000000, true, CPU.Direction.Left, ExpectedResult = true, TestName = "RLC -> C1")]
        [TestCase(0b01111111, false, CPU.Direction.Left, ExpectedResult = false, TestName = "RL -> C0")]
        [TestCase(0b10000000, false, CPU.Direction.Left, ExpectedResult = true, TestName = "RL -> C1")]
        [TestCase(0b11111110, false, CPU.Direction.Right, ExpectedResult = false, TestName = "RR -> C0")]
        [TestCase(0b00000001, false, CPU.Direction.Right, ExpectedResult = true, TestName = "RR -> C1")]
        public bool Rotate_CorrectCarry(byte input, bool carry, CPU.Direction direction)
        {
            cpu.FlagC = false;
            cpu.Rot(input, carry, direction);
            return cpu.FlagC;
        }
    }
}