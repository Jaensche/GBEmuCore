using GBCore;
using NUnit.Framework;
using System;
using System.Reflection.Emit;

namespace GBCoreTest
{
    [TestFixture]
    public class CPUTests
    {
        [Test]
        public void SequenceTest()
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{ 
                0x01, 0x12, 0x34, // LD BC 0x3412
                0x03, // INC BC
                0x04, // INC B
                0x05, // DEC B
                0x06, 0xAA // LD B, d8
            });
            cpu.PC = 0;

            cpu.Cycle();
            Assert.That(cpu.BC, Is.EqualTo(0x3412));

            cpu.Cycle();
            Assert.That(cpu.BC, Is.EqualTo(0x3413));

            cpu.Cycle();
            Assert.That(cpu.BC, Is.EqualTo(0x3513));
            Assert.That(cpu.IsSet(Flags.Z), Is.False);
            Assert.That(cpu.IsSet(Flags.N), Is.False);
            Assert.That(cpu.IsSet(Flags.H), Is.False);

            cpu.Cycle();
            Assert.That(cpu.BC, Is.EqualTo(0x3413));
            Assert.That(cpu.IsSet(Flags.Z), Is.False);
            Assert.That(cpu.IsSet(Flags.N), Is.True);
            Assert.That(cpu.IsSet(Flags.H), Is.False);

            cpu.Cycle();
            Assert.That(cpu.REG[CPU.B], Is.EqualTo(0xAA));
            Assert.That(cpu.IsSet(Flags.Z), Is.False);
            Assert.That(cpu.IsSet(Flags.N), Is.True);
            Assert.That(cpu.IsSet(Flags.H), Is.False);
        }

        [TestCase(0x01, CPU.B, CPU.C)]
        [TestCase(0x11, CPU.D, CPU.E)]
        [TestCase(0x21, CPU.H, CPU.L)]
        public void LD_d16(byte opcode, byte regHigh, byte regLow)
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{
                opcode, 0xBC, 0xAB
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;

            cpu.Cycle();
            Assert.That(cpu.REG[regHigh], Is.EqualTo(0xAB));
            Assert.That(cpu.REG[regLow], Is.EqualTo(0xBC));
            Assert.That(cpu.IsSet(Flags.Z), Is.False);
            Assert.That(cpu.IsSet(Flags.N), Is.False);
            Assert.That(cpu.IsSet(Flags.H), Is.False);
            Assert.That(cpu.IsSet(Flags.C), Is.False);
        }

        [Test]
        public void LD_SP_d16()
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{
                0x31, 0xBC, 0xAB
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;

            cpu.Cycle();
            Assert.That(cpu.SP, Is.EqualTo(0xABBC));
            Assert.That(cpu.IsSet(Flags.Z), Is.False);
            Assert.That(cpu.IsSet(Flags.N), Is.False);
            Assert.That(cpu.IsSet(Flags.H), Is.False);
            Assert.That(cpu.IsSet(Flags.C), Is.False);
        }

        [Test]
        public void INC_HalfCarry() 
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{
                0x00,
                0x04, // INC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x00;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.EqualTo(0));

            cpu.Load(new byte[]{
                0x00,
                0x04, // INC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x0F;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.GreaterThan(1));

            cpu.Load(new byte[]{
                0x00,
                0x04, // INC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0xFF;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.GreaterThan(1));

            cpu.Load(new byte[]{
                0x00,
                0x04, // INC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x2F;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.GreaterThan(1));
        }

        [Test]
        public void DEC_HalfCarry()
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{
                0x00,
                0x05, // DEC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x00;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.GreaterThan(1));

            cpu.Load(new byte[]{
                0x00,
                0x05, // DEC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x01;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.EqualTo(0));

            cpu.Load(new byte[]{
                0x00,
                0x05, // DEC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0xFF;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.EqualTo(0));

            cpu.Load(new byte[]{
                0x00,
                0x05, // DEC B
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
            cpu.REG[CPU.B] = 0x2F;

            cpu.Cycle();
            cpu.Cycle();
            Assert.That(cpu.REG[CPU.F] & (byte)Flags.H, Is.EqualTo(0));
        }        

        public static void LD_ADDR_A()
        {
            CPU cpu = new CPU();

            cpu.Load(new byte[]{
                0x02, 0xAB, 0xBC
            });
            cpu.PC = 0;
            cpu.REG[CPU.F] = 0x00;
        }
    }
}