﻿using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GbCoreTest")]
namespace GBCore
{
    /*
     *  0000 	3FFF 	16KB ROM bank 00 	From cartridge, usually a fixed bank
        4000 	7FFF 	16KB ROM Bank 01~NN 	From cartridge, switchable bank via MBC (if any)
        8000 	9FFF 	8KB Video _ram (V_ram) 	Only bank 0 in Non-CGB mode

        Switchable bank 0/1 in CGB mode
        A000 	BFFF 	8KB External _ram 	In cartridge, switchable bank if any
        C000 	CFFF 	4KB Work _ram (W_ram) bank 0 	
        D000 	DFFF 	4KB Work _ram (W_ram) bank 1~N 	Only bank 1 in Non-CGB mode

        Switchable bank 1~7 in CGB mode
        E000 	FDFF 	Mirror of C000~DDFF (ECHO _ram) 	Typically not used
        FE00 	FE9F 	Sprite attribute table (OAM) 	
        FEA0 	FEFF 	Not Usable 	
        FF00 	FF7F 	I/O Registers 	
        FF80 	FFFE 	High _ram (H_ram) 	
        FFFF 	FFFF 	Interrupts Enable Register (IE) 	
        */

    /*
     *  16bit Hi   Lo   Name/Function
         AF    A    -    Accumulator & Flags
         BC    B    C    BC
         DE    D    E    DE
         HL    H    L    HL
         SP    -    -    Stack Pointer
         PC    -    -    Prog_ram Counter/Pointer
         */

    public enum Flags : byte
    {
        Z = 128,
        N = 64,
        H = 32,
        C = 16
    }

    public enum IrqFlags : byte
    {
        VBlank = 1,
        LcdStat = 2,
        Timer = 4,
        Serial = 8,
        Joypad = 16
    }    

    public class CPU
    {
        private const ushort PROGMEMSTART = 0x100;

        public const int B = 0;
        public const int C = 1;
        public const int D = 2;
        public const int E = 3;
        public const int H = 4;
        public const int L = 5;
        public const int F = 6;
        public const int A = 7;
        public readonly byte[] REG = new byte[8];

        private Memory _ram;
        private Timer _timer;

        public const ushort IRQ_FLAGS = 0xFF0F;
        public const ushort IRQ_ENABLE = 0xFF0F;               

        public ushort PC; // Program Counter

        public bool IME; // Interrupt Master Enable

        public ushort SP; // Stack Pointer

        public bool RedrawFlag;

        public long _cycleCount;

        public bool _traceEnabled;

        public bool _haltState;

        public CPU(bool traceEnabled, Memory ram)
        {
            _traceEnabled = traceEnabled;
            _ram = ram;
            _timer = new Timer(ram);
            Reset();
        }

        public ushort AF
        {
            get
            {
                return (ushort)((REG[A] << 8) + REG[F]);
            }
            set
            {
                REG[F] = (byte)(value & 0x00FF);
                REG[A] = (byte)((value & 0xFF00) >> 8);
            }
        }

        public ushort BC
        {
            get
            {
                return (ushort)((REG[B] << 8) + REG[C]);
            }
            set
            {
                REG[C] = (byte)(value & 0x00FF);
                REG[B] = (byte)((value & 0xFF00) >> 8);
            }
        }

        public ushort DE
        {
            get
            {
                return (ushort)((REG[D] << 8) + REG[E]);
            }
            set
            {
                REG[E] = (byte)(value & 0x00FF);
                REG[D] = (byte)((value & 0xFF00) >> 8);
            }
        }

        public ushort HL
        {
            get
            {
                return (ushort)((REG[H] << 8) + REG[L]);
            }
            set
            {
                REG[L] = (byte)(value & 0x00FF);
                REG[H] = (byte)((value & 0xFF00) >> 8);
            }
        }        

        public void Reset()
        {
            PC = PROGMEMSTART;

            AF = 0x01B0;
            BC = 0x0013;
            DE = 0x00D8;
            HL = 0x014D;
            SP = 0xFFFE;

            IME = true;

            _ram.Write(0xFF05, 0x00);
            _ram.Write(0xFF06, 0x00);
            _ram.Write(0xFF07, 0x00);
            _ram.Write(0xFF10, 0x80);
            _ram.Write(0xFF11, 0xBF);
            _ram.Write(0xFF12, 0xF3);
            _ram.Write(0xFF14, 0xBF);
            _ram.Write(0xFF16, 0x3F);
            _ram.Write(0xFF17, 0x00);
            _ram.Write(0xFF19, 0xBF);
            _ram.Write(0xFF1A, 0x7F);
            _ram.Write(0xFF1B, 0xFF);
            _ram.Write(0xFF1C, 0x9F);
            _ram.Write(0xFF1E, 0xBF);
            _ram.Write(0xFF20, 0xFF);
            _ram.Write(0xFF21, 0x00);
            _ram.Write(0xFF22, 0x00);
            _ram.Write(0xFF23, 0xBF);
            _ram.Write(0xFF24, 0x77);
            _ram.Write(0xFF25, 0xF3);
            _ram.Write(0xFF26, 0xF1);
            _ram.Write(0xFF40, 0x91);
            _ram.Write(0xFF42, 0x00);
            _ram.Write(0xFF43, 0x00);
            _ram.Write(0xFF45, 0x00);
            _ram.Write(0xFF47, 0xFC);
            _ram.Write(0xFF48, 0xFF);
            _ram.Write(0xFF49, 0xFF);
            _ram.Write(0xFF4A, 0x00);
            _ram.Write(0xFF4B, 0x00);

            _ram.Write(0xFFFF, (byte)(_ram.Read(0xFFFF) | (byte)IrqFlags.Joypad));
            _ram.Write(0xFFFF, (byte)(_ram.Read(0xFFFF) | (byte)IrqFlags.LcdStat));
            _ram.Write(0xFFFF, (byte)(_ram.Read(0xFFFF) | (byte)IrqFlags.Serial));
            _ram.Write(0xFFFF, (byte)(_ram.Read(0xFFFF) | (byte)IrqFlags.Timer));
            _ram.Write(0xFFFF, (byte)(_ram.Read(0xFFFF) | (byte)IrqFlags.VBlank));

            // Hack for initial FY
            _ram.Write(0xFF44, 0x90);

            _cycleCount = 0;
            RedrawFlag = false;
        }

        private void CheckInterrupt(IrqFlags flag)
        {
            var irqFlags = (IrqFlags)_ram.Read(IRQ_FLAGS);
            var irqEnable = (IrqFlags)_ram.Read(IRQ_ENABLE);

            if (!IME && irqFlags.HasFlag(flag) && irqEnable.HasFlag(flag))
            {
                if (_haltState)
                {
                    _haltState = false;
                }
            }

            if (IME && irqFlags.HasFlag(flag) && irqEnable.HasFlag(flag))
            {
                if (_haltState)
                {
                    _haltState = false;
                }

                IME = false;
                Push16(PC);

                // Reset flag
                _ram.Write(IRQ_FLAGS, (byte)((byte)irqFlags & (byte)~flag));

                switch (flag)
                {
                    case IrqFlags.VBlank:
                        PC = 0x0040;
                        break;

                    case IrqFlags.LcdStat:
                        PC = 0x0048;
                        break;

                    case IrqFlags.Timer:
                        PC = 0x0050;
                        break;

                    case IrqFlags.Serial:
                        PC = 0x0058;
                        break;

                    case IrqFlags.Joypad:
                        PC = 0x0060;
                        break;
                }
            }            
        }

        private void IRQ()
        {
            CheckInterrupt(IrqFlags.VBlank);
            CheckInterrupt(IrqFlags.LcdStat);
            CheckInterrupt(IrqFlags.Timer);
            CheckInterrupt(IrqFlags.Serial);
            CheckInterrupt(IrqFlags.Joypad);
        }

        public void Cycle()
        {
            // reset redraw
            RedrawFlag = false;

            // Load Opcode
            byte opcode = _ram.Read(PC);

            if (_traceEnabled)
            {
                Trace();
            }

            long oldCycleCount = _cycleCount;

            // Decode Opcode
            // Execute Opcode
            DecodeExecute(opcode);

            _timer.TimerTick(_cycleCount - oldCycleCount);

            // handle interrupts
            IRQ();
        }

        private void Trace()
        {    
            Console.Write("A: {0:X2} ", REG[A]);
            Console.Write("F: {0:X2} ", REG[F]);
            Console.Write("B: {0:X2} ", REG[B]);
            Console.Write("C: {0:X2} ", REG[C]);
            Console.Write("D: {0:X2} ", REG[D]);
            Console.Write("E: {0:X2} ", REG[E]);
            Console.Write("H: {0:X2} ", REG[H]);
            Console.Write("L: {0:X2} ", REG[L]);
            Console.Write("SP: {0:X4} ", SP);
            Console.Write("PC: 00:{0:X4} ", PC);
            Console.Write("({0:X2} {1:X2} {2:X2} {3:X2})", _ram.Read(PC), _ram.Read((ushort)(PC + 1)), _ram.Read((ushort)(PC + 2)), _ram.Read((ushort)(PC + 3)));
            Console.WriteLine(string.Empty);
        }

        public void Load(byte[] programCode)
        {
            for (int i = 0; i < programCode.Length; i++)
            {
                _ram.Write((ushort)i, programCode[i]);
            }
        }

        private void DecodeExecute(byte opcode)
        {
            switch (opcode)
            {
                /********************************
                 * Misc / Control 
                 ********************************/

                //NOP
                case 0x00:
                    if (!_haltState)
                    {
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // STOP
                case 0x10:
                    Environment.Exit(1);
                    break;

                // HALT
                case 0x76:
                    if (!_haltState)
                    {
                        _haltState = true;
                        PC++;
                    }
                    break;

                // DI
                case 0xF3:
                    IME = false;
                    _cycleCount += 4;
                    PC++;
                    break;

                // EI
                case 0xFB:
                    IME = true;
                    _cycleCount += 4;
                    PC++;
                    break;

                // PREFIX CB
                case 0xCB:
                    PC++;
                    DecodeExectueCB(_ram.Read(PC));
                    break;

                /********************************
                 * Load / Store 
                 ********************************/

                // LD Rx, Ry
                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                case 0x44:
                case 0x45:
                case 0x47:
                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                case 0x4C:
                case 0x4D:
                case 0x4F:
                case 0x50:
                case 0x51:
                case 0x52:
                case 0x53:
                case 0x54:
                case 0x55:
                case 0x57:
                case 0x58:
                case 0x59:
                case 0x5A:
                case 0x5B:
                case 0x5C:
                case 0x5D:
                case 0x5F:
                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x67:
                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6F:
                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                case 0x7C:
                case 0x7D:
                case 0x7F:
                    LR_RxRy(opcode);
                    break;

                // LD Rx, n
                case 0x06:
                case 0x16:
                case 0x26:
                case 0x0E:
                case 0x1E:
                case 0x2E:
                case 0x3E:
                    LR_Rx_n(opcode);
                    break;

                // LD Rx, (HL)
                case 0x46:
                case 0x56:
                case 0x66:
                case 0x4E:
                case 0x5E:
                case 0x6E:
                case 0x7E:
                    LR_Rx_HL(opcode);
                    break;

                // LD (HL), Rx
                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                case 0x74:
                case 0x75:
                case 0x77:
                    LD_HL_Rx(opcode);
                    break;

                // LD (HL), n
                case 0x36:
                    LD_HL_n();
                    break;

                // LD A, (BC)
                case 0x0A:
                    {
                        REG[A] = _ram.Read(BC);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (DE)
                case 0x1A:
                    {
                        REG[A] = _ram.Read(DE);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (BC), A
                case 0x02:
                    {
                        _ram.Write(BC, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (DE), A
                case 0x12:
                    {
                        _ram.Write(DE, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (nn)
                case 0xFA:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        REG[A] = _ram.Read(addr);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD HL, SP+r8
                case 0xF8:
                    {
                        sbyte op = (sbyte)_ram.Read(++PC);

                        SetFlag(Flags.N, false);
                        SetFlag(Flags.Z, false);
                        SetFlag(Flags.H, IsPlusHalfCarryLowByte(SP, (byte)op));
                        SetFlag(Flags.C, IsPlusCarry((byte)(SP & 0x00FF), (byte)op));

                        HL = (ushort)(SP + op);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD (nn), A
                case 0xEA:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        _ram.Write(addr, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (C)
                case 0xF2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        REG[A] = _ram.Read(addr);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH (C), A
                case 0xE2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        _ram.Write(addr, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (n)
                case 0xF0:
                    {
                        ushort addr = (ushort)(0xFF00 + _ram.Read(++PC));
                        REG[A] = _ram.Read(addr);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LDH (n), A
                case 0xE0:
                    {
                        ushort addr = (ushort)(0xFF00 + _ram.Read(++PC));
                        _ram.Write(addr, REG[A]);

                        if (REG[A] == 0xC0)
                        {
                        }

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD A, (HL-)
                case 0x3A:
                    {
                        REG[A] = _ram.Read(HL--);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL-), A
                case 0x32:
                    {
                        _ram.Write(HL--, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (HL++)
                case 0x2A:
                    {
                        REG[A] = _ram.Read(HL++);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL++), A
                case 0x22:
                    {
                        _ram.Write(HL++, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD BC, nn
                case 0x01:
                    {
                        BC = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD DE, nn
                case 0x11:
                    {
                        DE = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD HL, nn
                case 0x21:
                    {
                        HL = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD SP, nn
                case 0x31:
                    {
                        SP = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD(nn), SP
                case 0x08:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        _ram.Write(addr, (byte)(SP & 0x00FF));
                        _ram.Write((ushort)(addr + 1), (byte)((SP & 0xFF00) >> 8));
                        PC++;
                        _cycleCount += 20;
                    }
                    break;

                // LD SP, HL
                case 0xF9:
                    {
                        SP = HL;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                /********************************
                 * Stack
                 ********************************/

                // PUSH Rx
                case 0xC5:
                case 0xD5:
                case 0xE5:
                    {
                        byte x = (byte)((opcode & 0b00110000) >> 3);

                        Push16((ushort)((REG[x] << 8) + REG[x + 1]));
                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // PUSH AF
                case 0xF5:
                    {
                        Push16((ushort)((REG[A] << 8) + REG[F]));
                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // POP Rx
                case 0xC1:
                case 0xD1:
                case 0xE1:
                    {
                        byte x = (byte)((opcode & 0b00110000) >> 3);

                        REG[x + 1] = Pop8();
                        REG[x] = Pop8();

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // POP AF
                case 0xF1:
                    {
                        REG[F] = (byte)(Pop8() & 0xF0);
                        REG[A] = Pop8();

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                /********************************
                 * Arithmetic Logic
                 ********************************/

                // INC Rx
                case 0x04:
                case 0x14:
                case 0x24:
                case 0x0C:
                case 0x1C:
                case 0x2C:
                case 0x3C:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[x], 1));
                        REG[x] += 1;
                        SetFlag(Flags.Z, REG[x] == 0);
                        SetFlag(Flags.N, false);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // DEC Rx
                case 0x05:
                case 0x15:
                case 0x25:
                case 0x0D:
                case 0x1D:
                case 0x2D:
                case 0x3D:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[x], (byte)(REG[x] - 1)));
                        REG[x] -= 1;
                        SetFlag(Flags.Z, (REG[x] == 0));
                        SetFlag(Flags.N, true);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // INC BC
                case 0x03:
                    {
                        BC += 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // INC DE
                case 0x13:
                    {
                        DE += 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // INC HL
                case 0x23:
                    {
                        HL += 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // INC SP
                case 0x33:
                    {
                        SP += 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // DEC BC
                case 0x0B:
                    {
                        BC -= 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // DEC DE
                case 0x1B:
                    {
                        DE -= 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // DEC HL
                case 0x2B:
                    {
                        HL -= 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // DEC SP
                case 0x3B:
                    {
                        SP -= 1;
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // INC (HL)
                case 0x34:
                    {
                        byte currentHL = _ram.Read(HL);
                        SetFlag(Flags.H, IsPlusHalfCarry(currentHL, 1));
                        byte result = (byte)(currentHL + 1);
                        _ram.Write(HL, result);

                        SetFlag(Flags.Z, result == 0);
                        SetFlag(Flags.N, false);

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // DEC (HL)
                case 0x35:
                    {
                        byte currentHL = _ram.Read(HL);
                        byte result = (byte)(currentHL - 1);
                        _ram.Write(HL, result);

                        SetFlag(Flags.Z, result == 0);
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarryLowByte(currentHL, (ushort)1));

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // CPL
                case 0x2F:
                    {
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, true);

                        REG[A] = (byte)~REG[A];

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SCF
                case 0x37:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, true);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // DAA
                case 0x27:
                    {
                        // note: assumes a is a uint8_t and wraps from 0xff to 0
                        if (!IsSet(Flags.N))
                        {  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
                            if (IsSet(Flags.C) || REG[A] > 0x99)
                            {
                                REG[A] += 0x60; SetFlag(Flags.C, true);
                            }
                            if (IsSet(Flags.H) || (REG[A] & 0x0F) > 0x09)
                            {
                                REG[A] += 0x06;
                            }
                        }
                        else
                        {  // after a subtraction, only adjust if (half-)carry occurred
                            if (IsSet(Flags.C))
                            {
                                REG[A] -= 0x60;
                            }
                            if (IsSet(Flags.H))
                            {
                                REG[A] -= 0x06;
                            }
                        }
                        // these flags are always updated
                        SetFlag(Flags.Z, (REG[A] == 0)); // the usual z flag
                        SetFlag(Flags.H, false); // h flag is always cleared

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD A, r8
                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x87:
                    {
                        int reg = opcode & 0b00000111;
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[A], REG[reg]));
                        SetFlag(Flags.C, REG[A] + REG[reg] > 0xFF);

                        REG[A] += REG[reg];
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD A, d8
                case 0xC6:
                    {
                        byte value = _ram.Read(++PC);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[A], value));
                        SetFlag(Flags.C, REG[A] + value > 0xFF);

                        REG[A] += value;
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD SP, r8
                case 0xE8:
                    {
                        sbyte value = (sbyte)_ram.Read(++PC);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarryLowByte(SP, (ushort)value));
                        SetFlag(Flags.C, IsPlusCarry((byte)(SP & 0x00FF), (byte)value));

                        SP += (ushort)value;
                        SetFlag(Flags.Z, false);

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // ADC A, r8
                case 0x88:
                case 0x89:
                case 0x8A:
                case 0x8B:
                case 0x8C:
                case 0x8D:
                case 0x8F:
                    {
                        int reg = opcode & 0b00000111;
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        byte val = REG[reg];
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[A], val, carry));
                        SetFlag(Flags.C, (REG[A] + val + carry) > 0xFF);

                        REG[A] += (byte)(val + carry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADC A, d8
                case 0xCE:
                    {
                        byte val = _ram.Read(++PC);
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, false);

                        bool halfcarry = IsPlusHalfCarry(REG[A], val, carry);
                        SetFlag(Flags.H, halfcarry);

                        bool newCarry = (REG[A] + val + carry) > 0xFF;
                        REG[A] += (byte)(val + carry);
                        SetFlag(Flags.C, newCarry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADC A, (HL)
                case 0x8E:
                    {
                        byte val = _ram.Read(HL);
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[A], val, carry));
                        SetFlag(Flags.C, (REG[A] + val + carry) > 0xFF);

                        REG[A] += (byte)(val + carry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // ADD A, (HL)
                case 0x86:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, IsPlusHalfCarry(REG[A], _ram.Read(HL)));
                        SetFlag(Flags.C, REG[A] + _ram.Read(HL) > 0xFF);

                        REG[A] += _ram.Read(HL);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, r8
                case 0x90:
                case 0x91:
                case 0x92:
                case 0x93:
                case 0x94:
                case 0x95:
                case 0x97:
                    {
                        int reg = opcode & 0b00000111;
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], REG[reg]));
                        SetFlag(Flags.C, REG[A] - REG[reg] < 0);

                        REG[A] -= REG[reg];
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, d8
                case 0xD6:
                    {
                        byte value = _ram.Read(++PC);
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], value));
                        SetFlag(Flags.C, REG[A] - value < 0);

                        REG[A] -= value;
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, r8
                case 0x98:
                case 0x99:
                case 0x9A:
                case 0x9B:
                case 0x9C:
                case 0x9D:
                case 0x9F:
                    {
                        int reg = opcode & 0b00000111;
                        byte val = REG[reg];
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val, carry));
                        SetFlag(Flags.C, (REG[A] - val - carry) < 0);

                        REG[A] = (byte)(REG[A] - val - carry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, d8
                case 0xDE:
                    {
                        byte val = _ram.Read(++PC);
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val, carry));
                        SetFlag(Flags.C, (REG[A] - val - carry) < 0);

                        REG[A] = (byte)(REG[A] - val - carry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, (HL)
                case 0x9E:
                    {
                        byte val = _ram.Read(HL);
                        byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val, carry));
                        SetFlag(Flags.C, (REG[A] - val - carry) < 0);

                        REG[A] = (byte)(REG[A] - val - carry);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, (HL)
                case 0x96:
                    {
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], _ram.Read(HL)));
                        SetFlag(Flags.C, REG[A] - _ram.Read(HL) < 0);

                        REG[A] -= _ram.Read(HL);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, r8
                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                case 0xA7:
                    {
                        int reg = opcode & 0b00000111;
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, true);
                        SetFlag(Flags.C, false);

                        REG[A] &= REG[reg];
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, d8
                case 0xE6:
                    {
                        byte value = _ram.Read(++PC);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, true);
                        SetFlag(Flags.C, false);

                        REG[A] &= value;
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, (HL)
                case 0xA6:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, true);
                        SetFlag(Flags.C, false);

                        REG[A] &= _ram.Read(HL);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // OR A, r8
                case 0xB0:
                case 0xB1:
                case 0xB2:
                case 0xB3:
                case 0xB4:
                case 0xB5:
                case 0xB7:
                    {
                        int reg = opcode & 0b00000111;
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] |= REG[reg];
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // OR A, d8
                case 0xF6:
                    {
                        byte value = _ram.Read(++PC);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] |= value;
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // OR A, (HL)
                case 0xB6:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] |= _ram.Read(HL);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, r8
                case 0xA8:
                case 0xA9:
                case 0xAA:
                case 0xAB:
                case 0xAC:
                case 0xAD:
                case 0xAF:
                    {
                        int reg = opcode & 0b00000111;
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] ^= REG[reg];
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, r8
                case 0xEE:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] ^= _ram.Read(++PC);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, (HL)
                case 0xAE:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);
                        SetFlag(Flags.C, false);

                        REG[A] ^= _ram.Read(HL);
                        SetFlag(Flags.Z, REG[A] == 0);

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // CP A, r8 
                case 0xB8:
                case 0xB9:
                case 0xBA:
                case 0xBB:
                case 0xBC:
                case 0xBD:
                case 0xBF:
                    {
                        int reg = (opcode & 0b00000111);
                        byte val = (byte)(REG[reg]); //+ (IsSet(Flags.C) ? 1 : 0));
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val));
                        SetFlag(Flags.C, val > REG[A]);

                        SetFlag(Flags.Z, REG[A] == val);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // CP A, d8
                case 0xFE:
                    {
                        //byte val = (byte)(_ram.ReadMem(++PC) + (IsSet(Flags.C) ? 1 : 0));
                        byte val = _ram.Read(++PC);
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val));
                        SetFlag(Flags.C, REG[A] - val < 0);

                        SetFlag(Flags.Z, (REG[A] - val) == 0);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // CP A, (HL)
                case 0xBE:
                    {
                        byte val = _ram.Read(HL);
                        //byte carry = IsSet(Flags.C) ? (byte)1 : (byte)0;
                        SetFlag(Flags.N, true);
                        SetFlag(Flags.H, IsMinusHalfCarry(REG[A], val));
                        SetFlag(Flags.C, (REG[A] - val) < 0);

                        SetFlag(Flags.Z, (REG[A] - val) == 0);

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // CCF
                case 0x3F:
                    {
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);

                        SetFlag(Flags.C, !((Flags)REG[F]).HasFlag(Flags.C));

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD HL, BC
                case 0x09:
                    Add_HL(BC);
                    break;

                // ADD HL, BC
                case 0x19:
                    Add_HL(DE);
                    break;

                // ADD HL, HL
                case 0x29:
                    Add_HL(HL);
                    break;

                // ADD HL, SP
                case 0x39:
                    Add_HL(SP);
                    break;

                /********************************
                 * Rotation Shift Bit
                 ********************************/

                // RLC A
                case 0x07:
                    {
                        SetFlag(Flags.Z, false);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);

                        SetFlag(Flags.C, (REG[A] & 0b10000000) > 0);

                        REG[A] = (byte)((REG[A] << 1) | (REG[A] >> 7));

                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // RRC A
                case 0x0F:
                    {
                        SetFlag(Flags.Z, false);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);

                        SetFlag(Flags.C, (REG[A] & 0b00000001) > 0);

                        REG[A] = (byte)((REG[A] >> 1) | (REG[A] << 7));

                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // RL A
                case 0x17:
                    {
                        SetFlag(Flags.Z, false);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);

                        bool cAfter = (REG[A] & 0b10000000) > 0;

                        REG[A] = (byte)(REG[A] << 1);
                        REG[A] += (byte)(IsSet(Flags.C) ? 1 : 0);
                        SetFlag(Flags.C, cAfter);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // RR A
                case 0x1F:
                    {
                        SetFlag(Flags.Z, false);
                        SetFlag(Flags.N, false);
                        SetFlag(Flags.H, false);

                        bool cAfter = (REG[A] & 0b00000001) > 0;

                        REG[A] = (byte)(REG[A] >> 1);
                        REG[A] += (byte)(IsSet(Flags.C) ? 0b10000000 : 0);
                        SetFlag(Flags.C, cAfter);

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                /********************************
                 * Jumps Calls
                 ********************************/

                // JR NZ, d8
                case 0x20:
                    {
                        sbyte offset = (sbyte)_ram.Read(++PC);
                        if (!IsSet(Flags.Z))
                        {
                            PC++;
                            PC += (ushort)offset;
                            _cycleCount += 12;
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // JR NC, d8
                case 0x30:
                    {
                        sbyte offset = (sbyte)_ram.Read(++PC);
                        if (!IsSet(Flags.C))
                        {
                            PC++;
                            PC += (ushort)offset;
                            _cycleCount += 12;
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // JR Z, d8
                case 0x28:
                    {
                        sbyte offset = (sbyte)_ram.Read(++PC);
                        if (IsSet(Flags.Z))
                        {
                            PC++;
                            PC += (ushort)offset;
                            _cycleCount += 12;
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // JR C, d8
                case 0x38:
                    {
                        sbyte offset = (sbyte)_ram.Read(++PC);
                        if (IsSet(Flags.C))
                        {
                            PC++;
                            PC += (ushort)offset;
                            _cycleCount += 12;
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // JR d8
                case 0x18:
                    {
                        sbyte offset = (sbyte)_ram.Read(++PC);
                        PC++;
                        PC += (ushort)offset;
                        _cycleCount += 12;
                    }
                    break;

                // RET NZ
                case 0xC0:
                    {
                        if (!IsSet(Flags.Z))
                        {
                            Ret();
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // RET NC
                case 0xD0:
                    {
                        if (!IsSet(Flags.C))
                        {
                            Ret();
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // RET Z
                case 0xC8:
                    {
                        if (IsSet(Flags.Z))
                        {
                            Ret();
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // RET C
                case 0xD8:
                    {
                        if (IsSet(Flags.C))
                        {
                            Ret();
                        }
                        else
                        {
                            _cycleCount += 8;
                            PC++;
                        }
                    }
                    break;

                // JP a16
                case 0xc3:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC = addr;
                        _cycleCount += 16;
                    }
                    break;

                // JP HL
                case 0xE9:
                    {
                        PC = HL;
                        _cycleCount += 4;
                    }
                    break;

                // JP NZ a16
                case 0xC2:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (!IsSet(Flags.Z))
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                        else
                        {
                            PC++;
                        }
                    }
                    break;

                // JP NZ a16
                case 0xD2:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (!IsSet(Flags.C))
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                        else
                        {
                            PC++;
                        }
                    }
                    break;

                // JP Z a16
                case 0xCA:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (IsSet(Flags.Z))
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                        else
                        {
                            PC++;
                        }
                    }
                    break;

                // JP C a16
                case 0xDA:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (IsSet(Flags.C))
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                        else
                        {
                            PC++;
                        }
                    }
                    break;

                // CALL a16
                case 0xCD:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        PC++;
                        Push16(PC);
                        PC = addr;
                        _cycleCount += 24;
                    }
                    break;

                // CALL Z 16
                case 0xCC:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (IsSet(Flags.Z))
                        {
                            PC++;
                            Push16(PC);
                            PC = addr;
                            _cycleCount += 24;
                        }
                        else
                        {
                            PC++;
                            _cycleCount += 12;
                        }
                    }
                    break;

                // CALL C 16
                case 0xDC:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (IsSet(Flags.C))
                        {
                            PC++;
                            Push16(PC);
                            PC = addr;
                            _cycleCount += 24;
                        }
                        else
                        {
                            PC++;
                            _cycleCount += 12;
                        }
                    }
                    break;

                // CALL NZ 16
                case 0xC4:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (!IsSet(Flags.Z))
                        {
                            PC++;
                            Push16(PC);
                            PC = addr;
                            _cycleCount += 24;
                        }
                        else
                        {
                            PC++;
                            _cycleCount += 12;
                        }
                    }
                    break;

                // CALL NC 16
                case 0xD4:
                    {
                        ushort addr = (ushort)(_ram.Read(++PC) + (_ram.Read(++PC) << 8));
                        if (!IsSet(Flags.C))
                        {
                            PC++;
                            Push16(PC);
                            PC = addr;
                            _cycleCount += 24;
                        }
                        else
                        {
                            PC++;
                            _cycleCount += 12;
                        }
                    }
                    break;

                // RST
                case 0xC7:
                case 0xD7:
                case 0xE7:
                case 0xF7:
                    {
                        byte addr = (byte)((opcode & 0xF0) - 0xC0);
                        Push16(++PC);
                        PC = addr;
                        _cycleCount += 16;
                    }
                    break;

                // RST
                case 0xCF:
                case 0xDF:
                case 0xEF:
                case 0xFF:
                    {
                        byte addr = (byte)((opcode & 0xF0) - 0xC0 + 8);
                        Push16(++PC);
                        PC = addr;
                        _cycleCount += 4;
                    }
                    break;

                // RETI
                case 0xD9:
                    {
                        ushort addr = Pop16();
                        PC = addr;
                        _cycleCount += 4;
                        IME = true;
                    }
                    break;

                // RET
                case 0xC9:
                    {
                        ushort addr = Pop16();
                        PC = addr;
                        _cycleCount += 4;
                    }
                    break;

                default:
                    {
                        throw new Exception(opcode.ToString());
                    }
            }
        }

        private void DecodeExectueCB(byte opcode)
        {
            switch (opcode)
            {

                case 0x00: case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x07:
                    RLC_r(opcode); // RLC r
                    break;
                case 0x08: case 0x09: case 0x0A: case 0x0B: case 0x0C: case 0x0D: case 0x0F:
                    RRC_r(opcode); // RRC r
                    break;
                case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x17:
                    RL_r(opcode); // RL r
                    break;
                case 0x18: case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1F:
                    RR_r(opcode); // RR r
                    break;
                case 0x06:
                    RLC_HL(); // RLC (HL)
                    break;
                // RRC (HL)
                case 0x0E:
                    RRC_HL();
                    break;
                // RL (HL)
                case 0x16:
                    RL_HL();
                    break;
                // RR (HL)
                case 0x1E:
                    RR_HL();
                    break;
                case 0x20: case 0x21: case 0x22: case 0x23: case 0x24: case 0x25: case 0x27:
                    SLA_r(opcode);
                    break;
                case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C: case 0x2D: case 0x2F:
                    SRA_r(opcode);
                    break;
                case 0x26:
                    SLA_HL(); // SLA (HL)
                    break;
                case 0x2E:
                    SRA_HL();  // SRA_HL
                    break;
                case 0x30: case 0x31: case 0x32: case 0x33: case 0x34: case 0x35: case 0x37:
                    SWAP_r(opcode); break;
                case 0x36: // SWAP (HL)
                    SWAP_HL(opcode); break;
                case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3F:
                    SRL_r(opcode); break;
                case 0x3E:
                    SRL_HL(); break; // SRL (HL)
                case 0x40: case 0x41: case 0x42: case 0x43: case 0x44: case 0x45: case 0x47:
                case 0x48: case 0x49: case 0x4A: case 0x4B: case 0x4C: case 0x4D: case 0x4F:
                case 0x50: case 0x51: case 0x52: case 0x53: case 0x54: case 0x55: case 0x57:
                case 0x58: case 0x59: case 0x5A: case 0x5B: case 0x5C: case 0x5D: case 0x5F:
                case 0x60: case 0x61: case 0x62: case 0x63: case 0x64: case 0x65: case 0x67:
                case 0x68: case 0x69: case 0x6A: case 0x6B: case 0x6C: case 0x6D: case 0x6F:
                case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75: case 0x77:
                case 0x78: case 0x79: case 0x7A: case 0x7B: case 0x7C: case 0x7D: case 0x7F:
                    BIT_x_r8(opcode); // BIT X r8
                    break;
                case 0x46: case 0x4E: case 0x56: case 0x5E: case 0x66: case 0x6E: case 0x76: case 0x7E:
                    BIT_X_HL(opcode); // BIT X (HL)
                    break;
                case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: case 0x87:
                case 0x88: case 0x89: case 0x8A: case 0x8B: case 0x8C: case 0x8D: case 0x8F:
                case 0x90: case 0x91: case 0x92: case 0x93: case 0x94: case 0x95: case 0x97:
                case 0x98: case 0x99: case 0x9A: case 0x9B: case 0x9C: case 0x9D: case 0x9F:
                case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: case 0xA7:
                case 0xA8: case 0xA9: case 0xAA: case 0xAB: case 0xAC: case 0xAD: case 0xAF:
                case 0xB0: case 0xB1: case 0xB2: case 0xB3: case 0xB4: case 0xB5: case 0xB7:
                case 0xB8: case 0xB9: case 0xBA: case 0xBB: case 0xBC: case 0xBD: case 0xBF:
                    RES_X_r8(opcode); // RES X r8
                    break;
                case 0x86: case 0x8E: case 0x96: case 0x9E: case 0xA6: case 0xAE: case 0xB6: case 0xBE:
                    RES_X_HL(opcode); // RES X (HL)
                    break;
                case 0xC0: case 0xC1: case 0xC2: case 0xC3: case 0xC4: case 0xC5: case 0xC7:
                case 0xC8: case 0xC9: case 0xCA: case 0xCB: case 0xCC: case 0xCD: case 0xCF:
                case 0xD0: case 0xD1: case 0xD2: case 0xD3: case 0xD4: case 0xD5: case 0xD7:
                case 0xD8: case 0xD9: case 0xDA: case 0xDB: case 0xDC: case 0xDD: case 0xDF:
                case 0xE0: case 0xE1: case 0xE2: case 0xE3: case 0xE4: case 0xE5: case 0xE7:
                case 0xE8: case 0xE9: case 0xEA: case 0xEB: case 0xEC: case 0xED: case 0xEF:
                case 0xF0: case 0xF1: case 0xF2: case 0xF3: case 0xF4: case 0xF5: case 0xF7:
                case 0xF8: case 0xF9: case 0xFA: case 0xFB: case 0xFC: case 0xFD: case 0xFF:
                    SET_X_r8(opcode); // SET X r8
                    break;
                case 0xC6: case 0xCE: case 0xD6: case 0xDE: case 0xE6: case 0xEE: case 0xF6: case 0xFE:
                    SET_X_HL(opcode); // SET X (HL)
                    break;
                default:
                    throw new NotImplementedException(opcode.ToString("X2"));
            }
        }

        private void SetFlag(Flags flag, bool input)
        {
            if (input)
            {
                REG[F] |= (byte)flag;
            }
            else
            {
                REG[F] &= (byte)~flag;
            }
        }

        public bool IsSet(Flags flag)
        {
            return ((Flags)REG[F]).HasFlag(flag);
        }

        private static bool IsPlusCarry(byte a, byte operand)
        {
            return (a + operand) > byte.MaxValue;
        }

        private static bool IsPlusHalfCarry(byte a, byte operand)
        {
            return ((a & 0x0F) + (operand & 0x0F) & 0x10) > 0;
        }

        private static bool IsPlusHalfCarry(byte a, byte operand, byte carry)
        {
            return (((a & 0x0F) + (operand & 0x0F) + (carry & 0xf)) & 0x10) > 0;
        }

        private static bool IsMinusHalfCarry(byte a, byte operand)
        {
            return ((a & 0x0F) - (operand & 0x0F) & 0x10) > 0;
        }

        private static bool IsMinusHalfCarry(byte a, byte operand, byte carry)
        {
            return (((a & 0x0F) - (operand & 0x0F) - (carry & 0x0F)) & 0x10) > 0;
        }

        private static bool IsPlusHalfCarryLowByte(ushort a, ushort operand)
        {
            byte lowA = (byte)(a & 0xFF);
            byte lowOp = (byte)(operand & 0xFF);
            return IsPlusHalfCarry(lowA, lowOp);
        }

        private static bool IsPlusHalfCarryHighByte(ushort a, ushort operand)
        {
            byte lowA = (byte)(a & 0xFF);
            byte lowOp = (byte)(operand & 0xFF);
            byte lowCarry = IsPlusCarry(lowA, lowOp) ? (byte)1 : (byte)0;
            byte highA = (byte)((a & 0xFF00) >> 8);
            byte highOp = (byte)((operand & 0xFF00) >> 8);
            return IsPlusHalfCarry(highA, highOp, lowCarry);
        }

        private static bool IsMinusHalfCarryLowByte(ushort a, ushort operand)
        {
            byte lowA = (byte)(a & 0xFF);
            byte lowOp = (byte)(operand & 0xFF);
            return IsMinusHalfCarry(lowA, lowOp);
        }

        private void SWAP_HL(byte opcode)
        {
            HL = APU.SWP(HL);            
        }

        private void RLC_r(byte opcode)
        {
            int reg = opcode & 0b00001111;

            REG[reg] = APU.ROT(REG[reg], ref REG[F], true, APU.Direction.Left);

            PC++;
            _cycleCount += 8;
        }

        private void RRC_r(byte opcode)
        {
            int reg = (opcode & 0b00001111) - 8;

            REG[reg] = APU.ROT(REG[reg], ref REG[F], true, APU.Direction.Right);

            PC++;
            _cycleCount += 8;
        }

        private void RL_r(byte opcode)
        {
            int reg = opcode & 0b00001111;

            REG[reg] = APU.ROT(REG[reg], ref REG[F], false, APU.Direction.Left);

            PC++;
            _cycleCount += 8;
        }

        private void RR_r(byte opcode)
        {
            int reg = (opcode & 0b00001111) - 8;

            REG[reg] = APU.ROT(REG[reg], ref REG[F], false, APU.Direction.Right);

            PC++;
            _cycleCount += 8;
        }

        private void RLC_HL()
        {
            _ram.Write(HL, APU.ROT(_ram.Read(HL), ref REG[F], true, APU.Direction.Left));

            PC++;
            _cycleCount += 16;
        }

        private void RRC_HL()
        {
            _ram.Write(HL, APU.ROT(_ram.Read(HL), ref REG[F], true, APU.Direction.Right));

            PC++;
            _cycleCount += 16;
        }

        private void RL_HL()
        {
            _ram.Write(HL, APU.ROT(_ram.Read(HL), ref REG[F], false, APU.Direction.Left));

            PC++;
            _cycleCount += 16;
        }

        private void RR_HL()
        {
            byte result = APU.ROT(_ram.Read(HL), ref REG[F], false, APU.Direction.Right);
            _ram.Write(HL, result);

            PC++;
            _cycleCount += 16;
        }

        private void SET_X_HL(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);

            _ram.Write(HL, (byte)(_ram.Read(HL) | (0b00000001 << x)));

            PC++;
            _cycleCount += 8;
        }

        private void SET_X_r8(byte opcode)
        {
            int reg = opcode & 0b00000111;
            byte x = (byte)((opcode & 0b00111000) >> 3);

            REG[reg] |= (byte)(0b00000001 << x);

            PC++;
            _cycleCount += 8;
        }

        private void RES_X_HL(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);

            _ram.Write(HL, (byte)(_ram.Read(HL) & (0b11111110 << x)));

            PC++;
            _cycleCount += 8;
        }

        private void RES_X_r8(byte opcode) // Reset bit X to 0
        {
            int reg = opcode & 0b00000111;
            byte x = (byte)((opcode & 0b00111000) >> 3);

            REG[reg] = (byte)(REG[reg] & ~(byte)(0b00000001 << x));

            PC++;
            _cycleCount += 8;
        }

        private void BIT_X_HL(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);

            SetFlag(Flags.Z, (_ram.Read(HL) & (0b00000001 << x)) == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, true);

            PC++;
            _cycleCount += 8;
        }

        private void BIT_x_r8(byte opcode)
        {
            int reg = opcode & 0b00000111;
            byte x = (byte)((opcode & 0b00111000) >> 3);

            SetFlag(Flags.Z, (REG[reg] & (0b00000001 << x)) == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, true);

            PC++;
            _cycleCount += 8;
        }

        private void SRL_HL()
        {
            byte result = (byte)(_ram.Read(HL) >> 1);
            SetFlag(Flags.C, (_ram.Read(HL) & 0x01) > 0);
            _ram.Write(HL, result);

            SetFlag(Flags.Z, result == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 16;
        }

        private void SRL_r(byte opcode)
        {
            int reg = (opcode & 0b00001111) - 8;

            SetFlag(Flags.C, (REG[reg] & 0b00000001) > 0);

            REG[reg] = (byte)(REG[reg] >> 1 & 0x7F);

            SetFlag(Flags.Z, REG[reg] == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 8;
        }

        private void SWAP_r(byte opcode)
        {
            int reg = opcode & 0b00000111;

            REG[reg] = APU.SWP(REG[reg]);

            SetFlag(Flags.Z, REG[reg] == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);
            SetFlag(Flags.C, false);

            PC++;
            _cycleCount += 8;
        }

        private void SRA_HL()
        {
            byte result = (byte)(_ram.Read(HL) >> 1);
            SetFlag(Flags.C, ((_ram.Read(HL) & 0x01) > 0));
            _ram.Write(HL, result);

            SetFlag(Flags.Z, result == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 16;
        }

        private void SLA_HL()
        {
            byte result = (byte)(_ram.Read(HL) << 1);
            SetFlag(Flags.C, (_ram.Read(HL) & 0b10000000) > 0);
            _ram.Write(HL, result);

            SetFlag(Flags.Z, result == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 16;
        }

        private void SRA_r(byte opcode)
        {
            int reg = opcode & 0b00000111;

            SetFlag(Flags.C, (REG[reg] & 0b00000001) > 0);
            REG[reg] = (byte)((REG[reg] & (1 << 7)) | (REG[reg] >> 1));

            SetFlag(Flags.Z, REG[reg] == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 8;
        }

        private void SLA_r(byte opcode)
        {
            int reg = opcode & 0b00000111;

            SetFlag(Flags.C, (REG[reg] & 0b10000000) > 0);
            REG[reg] = (byte)(REG[reg] << 1);

            SetFlag(Flags.Z, REG[reg] == 0);
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, false);

            PC++;
            _cycleCount += 8;
        }        

        private void LD_HL_n()
        {
            _ram.Write(HL, _ram.Read(++PC));
            PC++;
            _cycleCount += 12;
        }

        private void LD_HL_Rx(byte opcode)
        {
            byte x = (byte)(opcode & 0b00000111);
            _ram.Write(HL, REG[x]);
            PC++;
            _cycleCount += 8;
        }

        private void LR_Rx_HL(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);
            REG[x] = _ram.Read(HL);
            PC++;
            _cycleCount += 8;
        }

        private void LR_Rx_n(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);
            REG[x] = _ram.Read(++PC);
            PC++;
            _cycleCount += 8;
        }

        private void LR_RxRy(byte opcode)
        {
            byte x = (byte)((opcode & 0b00111000) >> 3);
            byte y = (byte)(opcode & 0b00000111);
            REG[x] = REG[y];
            PC++;
            _cycleCount += 4;
        }

        private void Ret()
        {
            PC = (ushort)((_ram.Read((ushort)(SP + 1)) << 8) + _ram.Read(SP));
            SP += 2;
            _cycleCount += 16;
        }

        private void Add_HL(ushort regVal)
        {
            SetFlag(Flags.N, false);
            SetFlag(Flags.H, IsPlusHalfCarryHighByte(HL, regVal));
            SetFlag(Flags.C, HL + regVal > ushort.MaxValue);
            HL += regVal;

            PC++;
            _cycleCount += 8;
        }

        public void Push16(ushort value)
        {
            // upper byte first
            Push8((byte)(value >> 8)); 
            Push8((byte)(value & 0xFF));
        }

        public void Push8(byte value)
        {
            _ram.Write(--SP, value);
        }

        public ushort Pop16()
        {
            // lower byte first
            ushort value = (ushort)(Pop8() + (Pop8() << 8));
            return value;
        }

        public byte Pop8()
        {
            return _ram.Read(SP++);
        }
    }
}
