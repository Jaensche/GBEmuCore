using System;

namespace GBCore
{
    public class PPU
    {
        byte[] VRAM = new byte[8 * 1024];
        byte[] TileData = new byte[16 * 24 * 16];
        byte[] OAM = new byte[100];
    }
}