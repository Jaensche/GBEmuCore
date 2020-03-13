using System;

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
        private const int B = 0;
        private const int C = 1;
        private const int D = 2;
        private const int E = 3;
        private const int H = 4;
        private const int L = 5;
        private const int F = 6;
        private const int A = 7;
        private readonly byte[] REG = new byte[8];

        private bool FlagZ;
        private bool FlagN;
        private bool FlagH;
        private bool FlagC;


        private bool IsHalfCarryAdd(byte a, byte b)
        {
            return (((a& 0xf) + (b & 0xf)) & 0x10) == 0x10;
        }
                
        private ushort AF
        {
            get
            {
                return (ushort)((REG[A] << 8) + REG[F]);
            }
            set
            {
                REG[A] = (byte)(value & 0x00FF);
                REG[F] = (byte)(value & 0xFF00 >> 8);
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
                REG[B] = (byte)(value & 0x00FF);
                REG[C] = (byte)(value & 0xFF00 >> 8);
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
                REG[D] = (byte)(value & 0x00FF);
                REG[E] = (byte)(value & 0xFF00 >> 8);
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
                REG[H] = (byte)(value & 0x00FF);
                REG[L] = (byte)(value & 0xFF00 >> 8);
            }
        }

        public byte[] RAM = new byte[8196];
        public byte[] VRAM = new byte[8196];       

        public ushort I;
        public ushort PC;        

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

        public void Reset()
        {
            PC = 0x0100;     // Program counter starts at 0x200
            I = 0;          // Reset index register
            SP = 0xFFFE;         // Reset stack pointer
            HL = 0x014D;
            DE = 0x00D8;
            BC = 0x0013;
            AF = 0x01B0;

            RAM[0xFF05] = 0x00;
            RAM[0xFF06] = 0x00;
            RAM[0xFF07] = 0x00;
            RAM[0xFF10] = 0x80;
            RAM[0xFF11] = 0xBF;
            RAM[0xFF12] = 0xF3;
            RAM[0xFF14] = 0xBF;
            RAM[0xFF16] = 0x3F;
            RAM[0xFF17] = 0x00;
            RAM[0xFF19] = 0xBF;
            RAM[0xFF1A] = 0x7F;
            RAM[0xFF1B] = 0xFF;
            RAM[0xFF1C] = 0x9F;
            RAM[0xFF1E] = 0xBF;
            RAM[0xFF20] = 0xFF;
            RAM[0xFF21] = 0x00;
            RAM[0xFF22] = 0x00;
            RAM[0xFF23] = 0xBF;
            RAM[0xFF24] = 0x77;
            RAM[0xFF25] = 0xF3;
            RAM[0xFF26] = 0xF1;
            RAM[0xFF40] = 0x91;
            RAM[0xFF42] = 0x00;
            RAM[0xFF43] = 0x00;
            RAM[0xFF45] = 0x00;
            RAM[0xFF47] = 0xFC;
            RAM[0xFF48] = 0xFF;
            RAM[0xFF49] = 0xFF;
            RAM[0xFF4A] = 0x00;
            RAM[0xFF4B] = 0x00;
            RAM[0xFFFF] = 0x00;

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

            // Decode Opcode
            // Execute Opcode
            DecodeExecute(opcode);

            return RedrawFlag;
        }        

        public void Load(byte[] programCode)
        {
            const int PROGMEMSTART = 0x200;

            for (int i = 0; i < programCode.Length; i++)
            {
                RAM[PROGMEMSTART + i] = programCode[i];
            }
        }

        private void DecodeExecute(byte opcode)
        {
            switch (opcode)
            {
                //NOP
                case 0x00: 
                    PC++;
                    _cycleCount += 4;
                    break;

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
                case 0x0E: case 0x1E: case 0x2E:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        REG[x] = RAM[PC++];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD Rx, (HL)
                case 0x46: case 0x56: case 0x66:
                case 0x4E: case 0x5E: case 0x6E: case 0x7E:
                    {
                        byte x = (byte)((opcode & 0b00111000) >> 3);
                        REG[x] = RAM[HL];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL), Rx
                case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75: case 0x76:                
                    {
                        byte x = (byte)(opcode & 0b00000111);
                        RAM[HL] = REG[x];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL), n
                case 0x36:        
                    {
                        RAM[HL] = RAM[PC++];
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD A, (BC)
                case 0x0A:
                    {
                        REG[A] = RAM[BC];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (DE)
                case 0x1A:
                    {
                        REG[A] = RAM[DE];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (BC), A
                case 0x02:
                    {
                        RAM[BC] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (DE), A
                case 0x12:
                    {
                        RAM[DE] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (nn)
                case 0xFA:
                    {
                        ushort addr = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        REG[A] = RAM[addr];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (nn), A
                case 0xEA:
                    {
                        ushort addr = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        RAM[addr] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (C)
                case 0xF2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        REG[A] = RAM[addr];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH (C), A
                case 0xE2:
                    {
                        ushort addr = (ushort)(0xFF00 + REG[C]);
                        RAM[addr] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LDH A, (n)
                case 0xF0:
                    {
                        ushort addr = (ushort)(0xFF00 + RAM[PC++]);
                        REG[A] = RAM[addr];
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LDH (n), A
                case 0xE0:
                    {
                        ushort addr = (ushort)(0xFF00 + RAM[PC++]);
                        RAM[addr] = REG[A];
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD A, (HL-)
                case 0x3A:
                    {
                        REG[A] = RAM[HL--];                                               
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL-), A
                case 0x32:
                    {
                        RAM[HL--] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD A, (HL++)
                case 0x2A:
                    {
                        REG[A] = RAM[HL++];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD (HL++), A
                case 0x22:
                    {
                        RAM[HL++] = REG[A];
                        PC++;
                        _cycleCount += 8;
                    }
                    break;

                // LD BC, nn
                case 0x01:
                    {
                        BC = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD DE, nn
                case 0x11:
                    {
                        DE = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD HL, nn
                case 0x21:
                    {
                        HL = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD SP, nn
                case 0x31:
                    {
                        SP = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // LD(nn), SP
                case 0x08:
                    {
                        ushort addr = (ushort)(RAM[PC++] + RAM[PC++] << 8);
                        RAM[addr] = (byte)(SP & 0x00FF);
                        RAM[addr + 1] = (byte)(SP & 0xFF00 >> 8);
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

                // PUSH Rx
                case 0xC5: case 0xD5: case 0xE5: case 0xF5:
                    {
                        byte x = (byte)(opcode & 0b00110000 >> 3);

                        RAM[SP--] = REG[x];
                        RAM[SP--] = REG[x + 1];
                        PC++;
                        _cycleCount += 16;
                    }
                    break;

                // POP Rx
                case 0xC1: case 0xD1: case 0xE1: case 0xF1:
                    {
                        byte x = (byte)(opcode & 0b00110000 >> 3);

                        REG[x] = RAM[SP++];
                        REG[x + 1] = RAM[SP++];
                        PC++;
                        _cycleCount += 12;
                    }
                    break;

                // INC R (B, D, H)
                case 0x04: case 0x14: case 0x24:
                    {
                        int reg = opcode & 0xF0 << 1;
                        REG[reg] += 1;
                        FlagZ = (REG[reg] == 0);
                        FlagN = false;
                        FlagH = IsHalfCarryAdd(REG[reg], 1);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // INC R (C, D L)
                case 0x0C: case 0x1C: case 0x2C:
                    {
                        int reg = (opcode & 0xF0 << 1) + 1;
                        REG[reg] += 1;
                        FlagZ = (REG[reg] == 0);
                        FlagN = false;
                        FlagH = IsHalfCarryAdd(REG[reg], 1);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // DEC R (B, D, H)
                case 0x05: case 0x15: case 0x25:
                    {
                        int reg = opcode & 0xF0 << 1;
                        REG[reg] -= 1;
                        FlagZ = (REG[reg] == 0);
                        FlagN = true;
                        FlagH = IsHalfCarryAdd(REG[reg], 1);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // DEC R (C, E, L)
                case 0x0D: case 0x1D: case 0x2D:
                    {
                        int reg = (opcode & 0xF0 << 1) + 1;
                        REG[reg] -= 1;
                        FlagZ = (REG[reg] == 0);
                        FlagN = true;
                        FlagH = IsHalfCarryAdd(REG[reg], 1);
                        PC++;
                        _cycleCount += 4;
                    }
                    break;

                // INC BC
                case 0x03:
                    {
                        BC += 1;
                        PC++;
                    }
                    break;

                // INC DE
                case 0x13:
                    {
                        DE += 1;
                        PC++;
                    }
                    break;

                // INC HL
                case 0x23:
                    {
                        HL += 1;
                        PC++;
                    }
                    break;

                // INC SP
                case 0x33:
                    {
                        SP += 1;
                        PC++;
                    }
                    break;

                // DEC BC
                case 0x0B:
                    {
                        BC -= 1;
                        PC++;
                    }
                    break;

                // DEC DE
                case 0x1B:
                    {
                        DE -= 1;
                        PC++;
                    }
                    break;

                // DEC HL
                case 0x2B:
                    {
                        HL -= 1;
                        PC++;
                    }
                    break;

                // DEC SP
                case 0x3B:
                    {
                        SP -= 1;
                        PC++;
                    }
                    break;

                default:
                    {
                        PC++;
                    }
                    break;
            }
        }        
    }
}
