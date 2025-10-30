using System.Collections.Generic;

namespace GBCore.Graphics
{
    public enum PPU_STATE
    {
        OAM_Scan = 2,
        Drawing = 3,
        H_Blank = 0,
        V_Blank = 1
    }    

    public class PPU
    {
        private const int SCANLINES = 144;
        private const int OAM_START = 0xFE00;
        private const int OAM_END = 0xFE9F;
        private const int OAM_ENTRY_LENGTH = 4;        
        private const int BG_MAP_A_START_ADDR = 0x9800;
        private const int BG_MAP_A_END = 0x9BFF;
        private const int SCY_ADDR = 0xFF42;
        private const int LY_ADDR = 0xFF44;
        private const int GB_SCREEN_WIDTH = 160;
        private const int GB_SCREEN_HEIGHT = 144;

        private PPU_STATE _ppuState = PPU_STATE.OAM_Scan;

        private long _ticks = 0;

        PixelFetcher _pixelFetcher; 
        FixedSizeQueue _pixelFetcherQueue;
        private ushort _x;
        private ushort _y;


        public PPU(Memory ram) 
        {
            _ram = ram;

            _pixelFetcherQueue = new FixedSizeQueue(16);
            _pixelFetcher = new PixelFetcher(_pixelFetcherQueue, ram);            
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
         * OxFF4A WY // Y-Position at which the top border of the window should be placed, 0 meaning the very top
         * OxFF4B WX // X-Position at which the window should be displayed (calculated -7)
         * 
         * Registers
         * 0xFF40 LCDC (LCD Control Register)
         * 0xFF41 STAT (LCD Status Register)
         * 0xFF42 SCY,
         * 0xFF44 LY,
         * 0xFF47 BGP,
        */

        private readonly Memory _ram;
        //byte[] TileData = new byte[16 * 24 * 16];
        //byte[,] OAM = new byte[40 * 8 * 8, 2]; // 40 Tiles * 8x8 Pixels * 2 bytes (nibbles), Sprite Data

        //byte[,] Background = new byte[256, 256]; // 32x32 tiles, 256x256 pixels
        //byte[,] Window = new byte[256, 256]; // Overlay over Background, Position WX, WY // 32x32 tiles, 256x256 pixels
        //byte[,] Viewport = new byte[160, 144]; // Currently visible 20x18 tiles, 160x144 pixels

        public int[] ScreenBuffer = new int[GB_SCREEN_WIDTH * (GB_SCREEN_HEIGHT + 10)];

        //private byte LY = 0;

        private readonly List<Sprite> spriteBuffer = new List<Sprite>();                  

        public void Cycle()
        {
            switch (_ppuState)
            {
                case PPU_STATE.OAM_Scan: // MODE 2
                    
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
                        _x = 0;
                        byte scy = _ram.Read(SCY_ADDR);
                        byte ly = _ram.Read(LY_ADDR);
                        
                        _y = (ushort)(scy + ly);
                        ushort tileLine = (ushort)(_y % 8);
                        ushort tileMapRowAddr = (ushort)(BG_MAP_A_START_ADDR + (ushort)(_y / 8) * 32);
                        _pixelFetcher.FetchReset(tileMapRowAddr, tileLine);

                        _ppuState = PPU_STATE.Drawing;
                    }                    
                    break;

                case PPU_STATE.Drawing: // MODE 3                    
                    // Background Pixel Fetching
                    // Fetch Tile No
                    // Fetch Tile Data(Low)
                    // Fetch Tile Data (High)
                    // Push to FIFO

                    _pixelFetcher.Tick();

                    int pixel = _pixelFetcherQueue.Dequeue();
                    if (pixel != -1)
                    {
                        if (_y < 144)
                        {
                            byte ly = _ram.Read(LY_ADDR);
                            ScreenBuffer[_x + ly * GB_SCREEN_WIDTH] = pixel;
                        }
                        _x++;
                    }                    
                    
                    if (_x == GB_SCREEN_WIDTH)
                    {                        
                        _ppuState = PPU_STATE.H_Blank;
                    }                    
                    break;

                case PPU_STATE.H_Blank: // MODE 0
                    if(_ticks == 456)
                    {
                        _ticks = 0;

                        byte ly = LYPlusOne();

                        if (ly >= SCANLINES)
                        {
                            _ppuState = PPU_STATE.V_Blank;
                        }
                        else
                        {
                            _ppuState = PPU_STATE.OAM_Scan;
                        } 
                    }                    
                    break;

                case PPU_STATE.V_Blank: // MODE 1                    
                    if (_ticks == 456)
                    {
                        // VBlank Interrupt
                        byte currentIrqFlags = _ram.Read(CPU.IRQ_FLAGS);
                        _ram.Write(CPU.IRQ_FLAGS, (byte)(currentIrqFlags & (byte)IrqFlags.VBlank));

                        _ticks = 0;
                        byte ly = LYPlusOne();

                        if (ly >= SCANLINES + 10)
                        {
                            _ppuState = PPU_STATE.OAM_Scan;
                            _ram.Write(LY_ADDR, 0);
                        }
                    }
                    break;
            }

            _ticks++;
        }

        private byte LYPlusOne()
        {
            byte ly = _ram.Read(LY_ADDR);
            ly++;
            _ram.Write(LY_ADDR, ly);
            return ly;
        }
    }
}