using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    public class PPU
    {
        private const int SCANLINES = 144;
        private const int OAM_START = 0xFE00;
        private const int OAM_END = 0xFE9F;

        /*
         * 0x8000-0x97FF Tile Data, 1 Tile = 16 bytes
         * 
         * 8000 Method = Tile Number (byte) => offset (*16) from 0x8000
         * 8800 Method = Tile Number (sbyte) => offset (*16) from 0x9000
         * 
         * 0x9800-0x9BFF Background Map 
         * 0x9C00-0x9FFF Background Map
         * 
         * 0xFE00-0xFE9F Object Attribute Memory (OAM)
         * Byte 0 - Y-Position
         * Byte 1 - X-Position
         * Byte 2 - Tile Number
         * Byte 3 - Sprite Flags
        */

        byte[] VRAM = new byte[8 * 1024];
        //byte[] TileData = new byte[16 * 24 * 16];
        //byte[,] OAM = new byte[40 * 8 * 8, 2]; // 40 Tiles * 8x8 Pixels * 2 bytes (nibbles), Sprite Data

        //byte[,] Background = new byte[256, 256];
        //byte[,] Window = new byte[256, 256]; // Overlay over Background, Position WX, WY
        //byte[,] Viewport = new byte[160, 144];

        byte[,] Screen = new byte[160, 144];

        public void Run()
        {
            for(int LY = 0; LY < SCANLINES;  LY++) 
            {
                List<Sprite> oamBuffer = new List<Sprite>();

                // OAM Scan (Mode 2)
                for(int sprite = 0; sprite < 40; sprite++)
                {
                    Sprite currentSprite = new Sprite(VRAM[sprite * 4], VRAM[sprite * 4],VRAM[sprite * 4], VRAM[sprite * 4]);

                    if(currentSprite.X >= 0 
                        && (LY + 16) >= currentSprite.Y
                        && (LY + 16) < (currentSprite.Y + 8) // TODO: Handle tall sprites
                        && oamBuffer.Count < 10)
                    {
                        oamBuffer.Add(currentSprite);
                    }
                }

                // Drawing (Mode 3)
                    // Background Pixel Fetching
                        // Fetch Tile No
                        // Fetch Tile Data(Low)
                        // Fetch Tile Data (High)
                        // Push to FIFO

                // H-Blank (Mode 0)
            }

            // V-Blank (Mode 1)
        }
    }
}