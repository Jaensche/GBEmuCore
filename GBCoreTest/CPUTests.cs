using GBCore;
using NUnit.Framework;

namespace GBCoreTest
{
    [TestFixture]
    public class CPUTests
    {
        [Test]
        public void Bla()
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{ 
                0x01, 0x12, 0x34, // LD BC 0x3412
                0x03, // INC BC
                0x04, // INC B
                0x05, // DEC B
            });
            cpu.PC = 0;

            cpu.Cycle();
            Assert.AreEqual(0x3412, cpu.BC);

            cpu.Cycle();
            Assert.AreEqual(0x3413, cpu.BC);

            cpu.Cycle();
            Assert.AreEqual(0x3513, cpu.BC);
            Assert.AreEqual(false, cpu.IsSet(Flags.Z));
            Assert.AreEqual(false, cpu.IsSet(Flags.N));
            Assert.AreEqual(false, cpu.IsSet(Flags.H));

            cpu.Cycle();
            Assert.AreEqual(0x3413, cpu.BC);
            Assert.AreEqual(false, cpu.IsSet(Flags.Z));
            Assert.AreEqual(true, cpu.IsSet(Flags.N));
            Assert.AreEqual(false, cpu.IsSet(Flags.H));
        }
    }
}