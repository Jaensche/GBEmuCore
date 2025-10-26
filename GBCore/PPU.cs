using System;
using System.Collections.Generic;

namespace GBCore
{
    public class Sprite
    {
        public byte Y { get; set; }
        public byte X { get; set; }
        public byte TileNumber { get; set; }
        public byte Flags { get; set; }

        public Sprite(byte y, byte x, byte tileNumber, byte flags)
        {
            this.Y = y;
            this.X = x;
            this.TileNumber = tileNumber;
            this.Flags = flags;
        }
    }

    public enum PPU_STATE
    {
        OAM_Scan = 2,
        Drawing = 3,
        H_Blank = 0,
        V_Blank = 1
    }

    public enum FETCH_STATE
    {
        ReadID,
        ReadData0,
        ReadData1,
        Push
    }

    public class PPU
    {
        private const int SCANLINES = 144;
        private const int OAM_START = 0xFE00;
        private const int OAM_END = 0xFE9F;
        private const int OAM_ENTRY_LENGTH = 4;
        private const int BG_MAP_A_START = 0x9800;
        private const int BG_MAP_A_END = 0x9BFF;

        private PPU_STATE ppuState = PPU_STATE.OAM_Scan;
        private FETCH_STATE fetchState = FETCH_STATE.ReadID;

        private long _ticks = 0;

        public PPU(Memory ram) 
        {
            _ram = ram;
        }

        /*
         * Tile Data
         * 0x8000-0x97FF Tile Data, 1 Tile = 16 bytes         
         *   8000 Method = Tile Number (byte) => offset (*16) from 0x8000
         *   8800 Method = Tile Number (sbyte) => offset (*16) from 0x9000
         * 
         * Background Maps
         *  0x9800-0x9BFF Background Map // 32x32 bytes representing tile numbers organized row by row
         *  0x9C00-0x9FFF Background Map
         * 
         * Object Attribute Memory (OAM)
         *  0xFE00-0xFE9F 
         *      Byte 0 - Y-Position
         *      Byte 1 - X-Position
         *      Byte 2 - Tile Number
         *      Byte 3 - Sprite Flags
         * 
         * Window
         * $FF4A WY // Y-Position at which the top border of the window should be placed, 0 meaning the very top
         * $FF4B WX // X-Position at which the window should be displayed (calculated -7)
         * 
         * Registers
         * $FF40 LCDC (LCD Control Register)
         * $FF41 STAT (LCD Status Register)
        */

        private readonly Memory _ram;
        //byte[] TileData = new byte[16 * 24 * 16];
        //byte[,] OAM = new byte[40 * 8 * 8, 2]; // 40 Tiles * 8x8 Pixels * 2 bytes (nibbles), Sprite Data

        //byte[,] Background = new byte[256, 256]; // 32x32 tiles, 256x256 pixels
        //byte[,] Window = new byte[256, 256]; // Overlay over Background, Position WX, WY // 32x32 tiles, 256x256 pixels
        //byte[,] Viewport = new byte[160, 144]; // Currently visible 20x18 tiles, 160x144 pixels

        private byte[,] Screen = new byte[160, 144];

        private byte LY = 0;

        private readonly List<Sprite> spriteBuffer = new List<Sprite>();

        private void Fetch()
        {
            switch (fetchState)
            {
                case FETCH_STATE.ReadID:
                    {
                        fetchState = FETCH_STATE.ReadData0;
                    }
                    break;
                case FETCH_STATE.ReadData0:
                    {
                        fetchState = FETCH_STATE.ReadData1;
                    }
                    break;
                case FETCH_STATE.ReadData1:
                    {
                        fetchState = FETCH_STATE.Push;
                    }
                    break;
                case FETCH_STATE.Push:
                    {
                        fetchState = FETCH_STATE.ReadID;
                    }
                    break;
            }
        }

        public void Cycle()
        {
            switch (ppuState)
            {
                case PPU_STATE.OAM_Scan: // MODE 2
                    {
                        // OAM Scan (Mode 2)
                        //for (ushort sprite = 0; sprite < 40; sprite++)
                        //{
                        //    Sprite currentSprite = new Sprite(
                        //        _ram.Read((ushort)(OAM_START + (sprite * OAM_ENTRY_LENGTH))), 
                        //        _ram.Read((ushort)(OAM_START + (sprite * OAM_ENTRY_LENGTH) + 1)), 
                        //        _ram.Read((ushort)(OAM_START + (sprite * OAM_ENTRY_LENGTH) + 2)), 
                        //        _ram.Read((ushort)(OAM_START + (sprite * OAM_ENTRY_LENGTH) + 3)));

                        //    if (currentSprite.X >= 0
                        //        && (LY + 16) >= currentSprite.Y
                        //        && (LY + 16) < (currentSprite.Y + 8) // TODO: Handle tall sprites
                        //        && spriteBuffer.Count < 10)
                        //    {
                        //        spriteBuffer.Add(currentSprite);
                        //    }
                        //}

                        if(_ticks == 80)
                        {
                            ppuState = PPU_STATE.Drawing; 
                        }
                    }
                    break;
                case PPU_STATE.Drawing: // MODE 3
                    {
                        // Background Pixel Fetching
                        // Fetch Tile No
                        // Fetch Tile Data(Low)
                        // Fetch Tile Data (High)
                        // Push to FIFO

                        if (_ticks == 160)
                        {
                            ppuState = PPU_STATE.H_Blank;
                        }
                    }
                    break;
                case PPU_STATE.H_Blank: // MODE 0
                    {
                        if(_ticks == 456)
                        {
                            _ticks = 0;

                            LY++;
                            if (LY >= 144)
                            {
                                ppuState = PPU_STATE.V_Blank;
                            }
                            else
                            {
                                ppuState = PPU_STATE.OAM_Scan;
                            } 
                        }
                    }
                    break;
                case PPU_STATE.V_Blank: // MODE 1
                    {
                        if (_ticks == 456)
                        {
                            _ticks = 0;
                            LY++;
                            if (LY >= 153) // idle for 10 lines
                            {
                                ppuState = PPU_STATE.OAM_Scan;
                                LY = 0;
                            }
                        }
                    }
                    break;
            }

            _ticks++;
        }
    }
}