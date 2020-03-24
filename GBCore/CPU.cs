using System;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("GbCoreTest")]
namespace GBCore
{
    /*
     *  0000 	3FFF 	16KB ROM bank 00 	From cartridge, usually a fixed bank
        4000 	7FFF 	16KB ROM Bank 01~NN 	From cartridge, switchable bank via MBC (if any)
        8000 	9FFF 	8KB Video RAM (VRAM) 	Only bank 0 in Non-CGB mode

        Switchable bank 0/1 in CGB mode
        A000 	BFFF 	8KB External RAM 	In cartridge, switchable bank if any
        C000 	CFFF 	4KB Work RAM (WRAM) bank 0 	
        D000 	DFFF 	4KB Work RAM (WRAM) bank 1~N 	Only bank 1 in Non-CGB mode

        Switchable bank 1~7 in CGB mode
        E000 	FDFF 	Mirror of C000~DDFF (ECHO RAM) 	Typically not used
        FE00 	FE9F 	Sprite attribute table (OAM) 	
        FEA0 	FEFF 	Not Usable 	
        FF00 	FF7F 	I/O Registers 	
        FF80 	FFFE 	High RAM (HRAM) 	
        FFFF 	FFFF 	Interrupts Enable Register (IE) 	
        */

    /*
     *  16bit Hi   Lo   Name/Function
         AF    A    -    Accumulator & Flags
         BC    B    C    BC
         DE    D    E    DE
         HL    H    L    HL
         SP    -    -    Stack Pointer
         PC    -    -    Program Counter/Pointer
         */

    
    public class CPU
    {
        private const ushort PROGMEMSTART = 0x101;

        private const int B = 0;
        private const int C = 1;
        private const int D = 2;
        private const int E = 3;
        private const int H = 4;
        private const int L = 5;
        private const int F = 6;
        private const int A = 7;
        private readonly byte[] REG = new byte[8];

        public Flags flags = new Flags();

        public bool IsHalfCarry(byte a, byte b)
        {
            return (((a & 0x0F) + (b & 0x0F)) & 0x10) == 0x10;
        }

        public bool IsHalfCarry(ushort a, ushort b)
        {
            return (((a & 0x00FF) + (b & 0x00FF)) & 0x0100) == 0x0100;
        }

        private ushort AF
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

        private ushort BC
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

        private ushort DE
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
        private ushort HL
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

        public byte[] RAM = new byte[0xFFFF];
        public byte[] VRAM = new byte[0xFFFF];       

        public ushort I;
        public ushort PC;

        public byte IME;
        public bool IME_enable;

        public ushort[] Stack = new ushort[32];
        public ushort SP;
        
        public bool RedrawFlag;    
        
        private int _cyclesPer60Hz;

        private Random _rand;

        private long _cycleCount;

        public CPU(int cyclesPer60Hz)
        {           
            _cyclesPer60Hz = cyclesPer60Hz;

            Reset();
        }

        public byte ReadMem(ushort addr)
        {
            byte data = RAM[addr];
            Console.WriteLine($"{addr:X4} -> {data:X2}");

            return data;   
        }

        public void WriteMem(ushort addr, byte data)
        {
            RAM[addr] = data;

            Console.WriteLine($"{addr:X4} <- {data:X2}");

            if (addr == 0xFF02 && data == 0x81)
            {
                Console.WriteLine(ReadMem(0xFF01));
            }
        }

        public void Reset()
        {
            PC = PROGMEMSTART;     // Program counter starts at 0x200
            I = 0;          // Reset index register
            //SP = 0xFFFE;         // Reset stack pointer
            //HL = 0x014D;
            //DE = 0x00D8;
            //BC = 0x0013;
            //AF = 0x01B0;
            //IME = 0;

            //RAM[0xFF05] = 0x00;
            //RAM[0xFF06] = 0x00;
            //RAM[0xFF07] = 0x00;
            //RAM[0xFF10] = 0x80;
            //RAM[0xFF11] = 0xBF;
            //RAM[0xFF12] = 0xF3;
            //RAM[0xFF14] = 0xBF;
            //RAM[0xFF16] = 0x3F;
            //RAM[0xFF17] = 0x00;
            //RAM[0xFF19] = 0xBF;
            //RAM[0xFF1A] = 0x7F;
            //RAM[0xFF1B] = 0xFF;
            //RAM[0xFF1C] = 0x9F;
            //RAM[0xFF1E] = 0xBF;
            //RAM[0xFF20] = 0xFF;
            //RAM[0xFF21] = 0x00;
            //RAM[0xFF22] = 0x00;
            //RAM[0xFF23] = 0xBF;
            //RAM[0xFF24] = 0x77;
            //RAM[0xFF25] = 0xF3;
            //RAM[0xFF26] = 0xF1;
            //RAM[0xFF40] = 0x91;
            //RAM[0xFF42] = 0x00;
            //RAM[0xFF43] = 0x00;
            //RAM[0xFF45] = 0x00;
            //RAM[0xFF47] = 0xFC;
            //RAM[0xFF48] = 0xFF;
            //RAM[0xFF49] = 0xFF;
            //RAM[0xFF4A] = 0x00;
            //RAM[0xFF4B] = 0x00;

            // Clear registers V0-VF
            for (int i = 0; i < REG.Length; i++)
            {
                REG[i] = 0;
            }

            _cycleCount = 0;
            _rand = new Random();
            RedrawFlag = false;
        }

        public bool Cycle()
        {
            // reset redraw
            RedrawFlag = false;

            // Load Opcode
            byte opcode = RAM[PC];

            Console.Write("{0:X4}, ", PC);            
            Console.Write("Opcode={0:X2}, ", opcode);
            Console.Write("A={0:X2}, ", REG[A]);
            Console.Write("B={0:X2}, ", REG[B]);
            Console.Write("C={0:X2}, ", REG[C]);
            Console.Write("D={0:X2}, ", REG[D]);
            Console.Write("E={0:X2}, ", REG[E]);
            Console.Write("H={0:X2}, ", REG[H]);
            Console.Write("L={0:X2}, ", REG[L]);
            Console.WriteLine("F={0:X2}", REG[F]);              

            // Decode Opcode
            // Execute Opcode
            DecodeExecute(opcode);

            //Thread.Sleep(100);

            return RedrawFlag;
        }        

        public void Load(byte[] programCode)
        {
            for (int i = 0; i < programCode.Length; i++)
            {
                RAM[i] = programCode[i];               
            }
        } 

        private void DecodeExectueCB(byte opcode)
        {
            switch (opcode)
            {
                // RLC r
                case 0x00: case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x07:
                    {
                        int reg = opcode & 0x0F;

                        REG[reg] = APU.ROT(REG[reg], ref flags, true, APU.Direction.Left);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // RRC r
                case 0x08: case 0x09: case 0x0A: case 0x0B: case 0x0C: case 0x0D: case 0x0F:
                    {
                        int reg = opcode & 0x0F;

                        REG[reg] = APU.ROT(REG[reg], ref flags, true, APU.Direction.Right);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // RL r
                case 0x10: case 0x11: case 0x12: case 0x13: case 0x14: case 0x15: case 0x17:
                    {
                        int reg = opcode & 0x0F;

                        REG[reg] = APU.ROT(REG[reg], ref flags, false, APU.Direction.Left);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // RR r
                 case 0x18: case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1F:
                    {
                        int reg = opcode & 0x0F;

                        REG[reg] = APU.ROT(REG[reg], ref flags, false, APU.Direction.Right);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // RLC (HL)
                case 0x06:
                    {
                        WriteMem(HL, APU.ROT(ReadMem(HL), ref flags, true, APU.Direction.Left));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // RRC(HL)
                case 0x0E:
                    {
                        WriteMem(HL, APU.ROT(ReadMem(HL), ref flags, true, APU.Direction.Right));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // RL (HL)
                case 0x16:
                    {
                        WriteMem(HL, APU.ROT(ReadMem(HL), ref flags, false, APU.Direction.Left));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // RR (HL)
                case 0x1E:
                    {
                        WriteMem(HL, APU.ROT(ReadMem(HL), ref flags, false, APU.Direction.Right));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // SLA r
                case 0x20: case 0x21: case 0x22: case 0x23: case 0x24: case 0x25: case 0x27:
                    {
                        int reg = opcode & 0x0F;

                        flags.C = (REG[reg] & 0b10000000) > 0;
                        REG[reg] = (byte)(REG[reg] << 1);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // SRA r
                case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C: case 0x2D: case 0x2F:
                    {
                        int reg = opcode & 0x0F;

                        flags.C = (REG[reg] & 0b00000001) > 0;
                        REG[reg] = (byte)(REG[reg] >> 1);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // SLA (HL)
                case 0x26:
                    {
                        flags.C = (ReadMem(HL) & 0b10000000) > 0;
                        WriteMem(HL, (byte)(ReadMem(HL) << 1));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // SRA (HL)
                case 0x2E:
                    {
                        flags.C = (ReadMem(HL) & 0b00000001) > 0;
                        WriteMem(HL, (byte)(ReadMem(HL) >> 1));

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // SWAP r
                case 0x30: case 0x31: case 0x32: case 0x33: case 0x34: case 0x35: case 0x37:
                    {
                        int reg = opcode & 0x0F;

                        REG[reg] = APU.SWP(REG[reg], ref flags);

                        PC++;
                        _cycleCount += 8;                        
                    }
                    break;

                // SRL r
                case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3F:
                    {
                        int reg = opcode & 0x0F;

                        PC++;
                        _cycleCount += 16;                                         
                    }
                    break;
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
                    PC++;
                    _cycleCount += 4;
                    break;

                // STOP
                case 0x10:
                    {
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // HALT
                case 0x76:
                    {
                        PC++;
                    }
                    break;

                // DI
                case 0xF3:
                    {
                        IME = 0;
                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // EI
                case 0xFB:
                    {
                        IME_enable = true;
                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // PREFIX CB
                case 0xCB:
                    {
                        PC++;
                        DecodeExectueCB(opcode);
                    }
                    break;

                /********************************
                 * Load / Store 
                 ********************************/

                // LD Rx, Ry
                case 0x40: case 0x41: case 0x42: case 0x43: case 0x44: case 0x45: case 0x47: case 0x48: case 0x49: case 0x4A: case 0x4B: case 0x4C: case 0x4D: case 0x4F:
                case 0x50: case 0x51: case 0x52: case 0x53: case 0x54: case 0x55: case 0x57: case 0x58: case 0x59: case 0x5A: case 0x5B: case 0x5C: case 0x5D: case 0x5F:
                case 0x60: case 0x61: case 0x62: case 0x63: case 0x64: case 0x65: case 0x67: case 0x68: case 0x69: case 0x6A: case 0x6B: case 0x6C: case 0x6D: case 0x6F:
                case 0x78: case 0x79: case 0x7A: case 0x7B: case 0x7C: case 0x7D: case 0x7F:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        byte y = (byte)(opcode & 0b00000111);
                        REG[x] = REG[y];
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // LD Rx, n
                case 0x06: case 0x16: case 0x26:
                case 0x0E: case 0x1E: case 0x2E: case 0x3E:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        REG[x] = ReadMem(++PC);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD Rx, (HL)
                case 0x46: case 0x56: case 0x66:
                case 0x4E: case 0x5E: case 0x6E: case 0x7E:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        REG[x] = ReadMem(HL);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL), Rx
                case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75:                
                    {
                        byte x = (byte)(opcode & 0b00000111);
                        WriteMem(HL, REG[x]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL), n
                case 0x36:        
                    {
                        WriteMem(HL, ReadMem(++PC));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD A, (BC)
                case 0x0A:
                    {
                        REG[A] = ReadMem(BC);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (DE)
                case 0x1A:
                    {
                        REG[A] = ReadMem(DE);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (BC), A
                case 0x02:
                    {
                        WriteMem(BC, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (DE), A
                case 0x12:
                    {
                        WriteMem(DE, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (nn)
                case 0xFA:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        REG[A] = ReadMem(addr);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD HL, SP+r8
                case 0xF8:
                    {
                        flags.N = false;
                        flags.Z = false;
                        flags.H = IsHalfCarry(SP, (ushort)(sbyte)ReadMem(++PC));
                        flags.C = SP + (sbyte)ReadMem(++PC) > 0xFFFF;
                        HL = (ushort)(SP + (sbyte)ReadMem(++PC));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD (nn), A
                case 0xEA:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        WriteMem(addr, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (C)
                case 0xF2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        REG[A] = ReadMem(addr);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH (C), A
                case 0xE2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        WriteMem(addr, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (n)
                case 0xF0:
                    {
                        ushort addr = (ushort)(0xFF00 + ReadMem(++PC));
                        REG[A] = ReadMem(addr);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LDH (n), A
                case 0xE0:
                    {
                        ushort addr = (ushort)(0xFF00 + ReadMem(++PC));
                        WriteMem(addr, REG[A]);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD A, (HL-)
                case 0x3A:
                    {
                        REG[A] = ReadMem(HL--);                                               
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL-), A
                case 0x32:
                    {
                        WriteMem(HL--, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (HL++)
                case 0x2A:
                    {
                        REG[A] = ReadMem(HL++);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL++), A
                case 0x22:
                    {
                        WriteMem(HL++, REG[A]);
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD BC, nn
                case 0x01:
                    {
                        BC = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD DE, nn
                case 0x11:
                    {
                        DE = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD HL, nn
                case 0x21:
                    {
                        HL = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD SP, nn
                case 0x31:
                    {
                        SP = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD(nn), SP
                case 0x08:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        WriteMem(addr, (byte)(SP & 0x00FF));
                        WriteMem((ushort)(addr + 1), (byte)(SP & 0xFF00 >> 8));
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
                case 0xC5: case 0xD5: case 0xE5: case 0xF5:
                    {
                        byte x = (byte)(opcode & 0b00110000 >> 3);

                        WriteMem(SP--, REG[x]);
                        WriteMem(SP--, REG[x + 1]);
                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // POP Rx
                case 0xC1: case 0xD1: case 0xE1: case 0xF1:
                    {
                        byte x = (byte)(opcode & 0b00110000 >> 3);

                        REG[x] = ReadMem(SP++);
                        REG[x + 1] = ReadMem(SP++);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                /********************************
                 * Arithmetic Logic
                 ********************************/

                // INC Rx
                case 0x04: case 0x14: case 0x24: case 0x0C: case 0x1C: case 0x2C: case 0x3C:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        flags.H = IsHalfCarry(REG[x], 1);
                        REG[x] += 1;
                        flags.Z = (REG[x] == 0);
                        flags.N = false;                        
                        PC++;
                        _cycleCount += 4;
                    }
                    break;                

                // DEC Rx
                case 0x05: case 0x15: case 0x25: case 0x0D: case 0x1D: case 0x2D: case 0x3D:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        flags.H = IsHalfCarry(REG[x], 0xFF);
                        REG[x] -= 1;
                        flags.Z = (REG[x] == 0);
                        flags.N = true;                        
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
                        flags.H = IsHalfCarry(ReadMem(HL), 1);
                        byte result = (byte)(ReadMem(HL) + 1);
                        WriteMem(HL, result);

                        flags.Z = result == 0;
                        flags.N = false;

                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // DEC (HL)
                case 0x35:
                    {
                        flags.H = IsHalfCarry(ReadMem(HL), 0xFF);
                        byte result = (byte)(ReadMem(HL) - 1);
                        WriteMem(HL, result);

                        flags.Z = result == 0;
                        flags.N = true;      
                        
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // CPL
                case 0x2F:
                    {
                        flags.N = true;
                        flags.H = true;

                        REG[A] = (byte)~REG[A];

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SCF
                case 0x37:
                    {
                        flags.N = false;
                        flags.H = false;
                        flags.C = true;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // DAA
                case 0x27:
                    {
                        // note: assumes a is a uint8_t and wraps from 0xff to 0
                        if (!flags.N)
                        {  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
                            if (flags.C || REG[A] > 0x99)
                            {
                                REG[A] += 0x60; flags.C = true;
                            }
                            if (flags.H || (REG[A] & 0x0F) > 0x09)
                            {
                                REG[A] += 0x06;
                            }
                        }
                        else
                        {  // after a subtraction, only adjust if (half-)carry occurred
                            if (flags.C)
                            {
                                REG[A] -= 0x60;
                            }
                            if (flags.H)
                            {
                                REG[A] -= 0x06;
                            }
                        }
                        // these flags are always updated
                        flags.Z = (REG[A] == 0); // the usual z flag
                        flags.H = false; // h flag is always cleared

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD A, r8
                case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: case 0x87:
                    {
                        int reg = opcode & 0x0F;
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], REG[reg]);
                        flags.C = REG[A] + REG[reg] > 0xFF;

                        REG[A] += REG[reg];
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD A, d8
                case 0xC6:
                    {
                        byte value = ReadMem(++PC);
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], value);
                        flags.C = REG[A] + value > 0xFF;

                        REG[A] += value;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD SP, r8
                case 0xE8:
                    {
                        sbyte value = (sbyte)ReadMem(++PC);
                        flags.N = false;
                        flags.H = IsHalfCarry(SP, (ushort)value);
                        flags.C = value + value > 0xFF;

                        SP += (ushort)value;
                        flags.Z = false;

                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // ADC A, r8
                case 0x88: case 0x89: case 0x8A: case 0x8B: case 0x8C: case 0x8D: case 0x8F:
                    {
                        int reg = opcode & 0x0F;
                        byte val = (byte)(REG[reg] + (flags.C ? 1 : 0));
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] + val > 0xFF;

                        REG[A] += val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADC A, d8
                case 0xCE:
                    {
                        byte val = (byte)(ReadMem(++PC) + (flags.C ? 1 : 0));
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] + val > 0xFF;

                        REG[A] += val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADC A, (HL)
                case 0x8E:
                    {
                        byte val = (byte)(ReadMem(HL) + (flags.C ? 1 : 0));
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] + val > 0xFF;

                        REG[A] += val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // ADD A, (HL)
                case 0x86:
                    {
                        flags.N = false;
                        flags.H = IsHalfCarry(REG[A], ReadMem(HL));
                        flags.C = REG[A] + ReadMem(HL) > 0xFF;

                        REG[A] += ReadMem(HL);
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, r8
                case 0x90: case 0x91: case 0x92: case 0x93: case 0x94: case 0x95: case 0x97:
                    {
                        int reg = opcode & 0x0F;
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], REG[reg]);
                        flags.C = REG[A] - REG[reg] < 0;

                        REG[A] -= REG[reg];
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, d8
                case 0xD6:
                    {
                        byte value = ReadMem(++PC);
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], value);
                        flags.C = REG[A] - value < 0;

                        REG[A] -= value;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, r8
                case 0x98: case 0x99: case 0x9A: case 0x9B: case 0x9C: case 0x9D: case 0x9F:
                    {
                        int reg = opcode & 0x0F;
                        byte val = (byte)(REG[reg] + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        REG[A] -= val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, d8
                case 0xDE:
                    {
                        byte val = (byte)(ReadMem(++PC) + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        REG[A] -= val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SBC A, (HL)
                case 0x9E:
                    {
                        byte val = (byte)(ReadMem(HL) + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        REG[A] -= val;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // SUB A, (HL)
                case 0x96:
                    {
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], ReadMem(HL));
                        flags.C = REG[A] - ReadMem(HL) < 0;

                        REG[A] -= ReadMem(HL);
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, r8
                case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: case 0xA7:
                    {
                        int reg = opcode & 0x0F;
                        flags.N = false;
                        flags.H = true;
                        flags.C = false;

                        REG[A] &= REG[reg];
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, d8
                case 0xE6:
                    {
                        byte value = ReadMem(++PC);
                        flags.N = false;
                        flags.H = true;
                        flags.C = false;

                        REG[A] &= value;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // AND A, (HL)
                case 0xA6:
                    {
                        flags.N = false;
                        flags.H = true;
                        flags.C = false;

                        REG[A] &= ReadMem(HL);
                        flags.Z = REG[A] == 0;

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
                        int reg = opcode & 0x0F;
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] |= REG[reg];
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // OR A, d8
                case 0xF6:
                    {
                        byte value = ReadMem(++PC);
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] |= value;
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // OR A, (HL)
                case 0xB6:
                    {
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] |= ReadMem(HL);
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, r8
                case 0xA8: case 0xA9: case 0xAA: case 0xAB: case 0xAC: case 0xAD: case 0xAF:
                    {
                        int reg = opcode & 0x0F;
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] ^= REG[reg];
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, r8
                case 0xEE:
                    {
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] ^= ReadMem(++PC);
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // XOR A, (HL)
                case 0xAE:
                    {
                        flags.N = false;
                        flags.H = false;
                        flags.C = false;

                        REG[A] ^= ReadMem(HL);
                        flags.Z = REG[A] == 0;

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // CP A, r8 
                case 0xB8: case 0xB9: case 0xBA: case 0xBB: case 0xBC: case 0xBD: case 0xBF:
                    {
                        int reg = (opcode & 0x0F) - 8;
                        byte val = (byte)(REG[reg] + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        flags.Z = (REG[A] - val) == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // CP A, d8
                case 0xFE:
                    {
                        byte val = (byte)(ReadMem(++PC) + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        flags.Z = (REG[A] - val) == 0;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // CP A, (HL)
                case 0xBE:
                    {
                        byte val = (byte)(ReadMem(HL) + (flags.C ? 1 : 0));
                        flags.N = true;
                        flags.H = IsHalfCarry(REG[A], val);
                        flags.C = REG[A] - val < 0;

                        flags.Z = (REG[A] - val) == 0;

                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // CCF
                case 0x3F:
                    {
                        flags.N = false;
                        flags.H = false;

                        flags.C = !flags.C;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // ADD HL, BC
                case 0x09:
                    AddHL(BC);
                    break;

                // ADD HL, BC
                case 0x19:
                    AddHL(DE);
                    break;

                // ADD HL, HL
                case 0x29:
                    AddHL(HL);
                    break;

                // ADD HL, SP
                case 0x39:
                    AddHL(SP);
                    break;

                /********************************
                 * Rotation Shift Bit
                 ********************************/

                // RLC A
                case 0x07:
                    {
                        flags.Z = false;
                        flags.N = false;
                        flags.H = false;

                        flags.C = (REG[A] & 0b10000000) > 0;

                        REG[A] = (byte)(REG[A] << 1);

                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // RRC A
                case 0x0F:
                    {
                        flags.Z = false;
                        flags.N = false;
                        flags.H = false;

                        flags.C = (REG[A] & 0b00000001) > 0;

                        REG[A] = (byte)(REG[A] >> 1);

                        _cycleCount += 4;
                        PC++;
                    }
                    break;

                // RL A
                case 0x17:
                    {
                        flags.Z = false;
                        flags.N = false;
                        flags.H = false;

                        bool cAfter = (REG[A] & 0b10000000) > 0;

                        REG[A] = (byte)(REG[A] << 1);
                        REG[A] += (byte)(flags.C ? 1 : 0);
                        flags.C = cAfter;

                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // RR A
                case 0x1F:
                    {
                        flags.Z = false;
                        flags.N = false;
                        flags.H = false;

                        bool cAfter = (REG[A] & 0b00000001) > 0;

                        REG[A] = (byte)(REG[A] >> 1);
                        REG[A] += (byte)(flags.C ? 0b10000000 : 0);
                        flags.C = cAfter;

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
                        sbyte offset = (sbyte)ReadMem(++PC);
                        if(!flags.Z)
                        {
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
                        sbyte offset = (sbyte)ReadMem(++PC);
                        if (!flags.C)
                        {
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
                        sbyte offset = (sbyte)ReadMem(++PC);
                        if (flags.Z)
                        {
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
                        sbyte offset = (sbyte)ReadMem(++PC);
                        if (flags.C)
                        {
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
                        sbyte offset = (sbyte)ReadMem(++PC);
                       
                        PC += (ushort)offset;
                        _cycleCount += 12;                        
                    }
                    break;                

                // RET NZ
                case 0xC0:
                    {
                        if(!flags.Z)
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
                        if (!flags.C)
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
                        if (flags.Z)
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
                        if (flags.C)
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
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
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
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (!flags.Z)
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                    }
                    break;

                // JP NZ a16
                case 0xD2:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (!flags.C)
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                    }
                    break;

                // JP Z a16
                case 0xCA:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (flags.Z)
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                    }
                    break;

                // JP C a16
                case 0xDA:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (flags.C)
                        {
                            PC = addr;
                            _cycleCount += 16;
                        }
                    }
                    break;

                // CALL a16
                case 0xCD:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        WriteMem(--SP, (byte)(PC >> 8));
                        WriteMem(--SP, (byte)(PC & 0x00FF));
                        PC = addr;
                        _cycleCount += 24;
                    }
                    break;

                // CALL Z 16
                case 0xCC:
                    {
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (flags.Z)
                        {                            
                            WriteMem(--SP, (byte)(PC >> 8));
                            WriteMem(--SP, (byte)(PC & 0x00FF));
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
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (flags.C)
                        {
                            WriteMem(--SP, (byte)(PC >> 8));
                            WriteMem(--SP, (byte)(PC & 0x00FF));
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
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (!flags.Z)
                        {
                            WriteMem(--SP, (byte)(PC >> 8));
                            WriteMem(--SP, (byte)(PC & 0x00FF));
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
                        ushort addr = (ushort)(ReadMem(++PC) + (ReadMem(++PC) << 8));
                        if (!flags.C)
                        {
                            WriteMem(--SP, (byte)(PC >> 8));
                            WriteMem(--SP, (byte)(PC & 0x00FF));
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
                case 0xC7: case 0xD7: case 0xE7: case 0xF7:
                    {
                        byte addr = (byte)((opcode & 0xF0) - 0xC0);
                        WriteMem(--SP, (byte)(PC >> 8));
                        WriteMem(--SP, (byte)(PC & 0x00FF));
                        PC = addr;
                        _cycleCount += 16;
                    }
                    break;

                // RST
                case 0xCF: case 0xDF: case 0xEF: case 0xFF:
                    {
                        byte addr = (byte)(((opcode & 0xF0) - 0xC0) + 8);
                        WriteMem(--SP, (byte)(PC >> 8));
                        WriteMem(--SP, (byte)(PC & 0x00FF));
                        PC = addr;
                        _cycleCount += 4;
                    }
                    break;                

                // RETI
                case 0xD9:
                    {
                        ushort addr = (ushort)(ReadMem(SP++) + (ReadMem(SP++) << 8));
                        PC = addr;
                        _cycleCount += 4;
                        IME = 1;
                    }
                    break;

                // RET
                case 0xC9:
                    {
                        ushort addr = (ushort)(ReadMem(SP++) + (ReadMem(SP++) << 8));
                        PC = addr;
                        _cycleCount += 4;
                    }
                    break;

                default:
                    {
                        throw new Exception(opcode.ToString());                            
                    }
                    break;
            }
        }         

        private void Ret()
        {
            PC = (ushort)((ReadMem((ushort)(SP + 1)) << 8) + ReadMem(SP));
            SP += 2;
            _cycleCount += 16;
        }
        
        private void AddHL(ushort regVal)
        {
            flags.N = false;
            flags.H = IsHalfCarry(HL, BC);
            flags.C = HL + BC > ushort.MaxValue;
            HL += BC;

            PC++;
            _cycleCount += 8;
        }
    }
}
