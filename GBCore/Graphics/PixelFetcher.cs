namespace GBCore.Graphics
{
    public enum FETCH_STATE
    {
        ReadID,
        ReadData0,
        ReadData1,
        Push
    }

    public class PixelFetcher
    {
        private const int TILE_DATA_START = 0x8000;

        private ushort _tileIndex;
        private ushort _mapAddr;
        private ushort _tileLine;
        private FETCH_STATE _fetchState;
        private int _ticks;
        private byte _tileId;
        private byte[] _vram; 

        private byte _low;

        private byte _high;

        FixedSizeQueue _pixelFetcherQueue;

        public PixelFetcher(FixedSizeQueue pixelFetcherQueue, byte[] vram) 
        {
            _vram = vram;
            _pixelFetcherQueue = pixelFetcherQueue;
            _ticks = 0;
        }

        public void FetchReset(ushort mapAddr, ushort tileLine)
        {
            _tileIndex = 0;
            _mapAddr = mapAddr;
            _tileLine = tileLine;
            _fetchState = FETCH_STATE.ReadID;

            _pixelFetcherQueue.Clear();
        }

        public void Tick()
        {
            _ticks++;
            if(_ticks % 2 == 0) 
            {
                return;
            }

            ushort offset;
            ushort addr = 0;

            switch (_fetchState)
            {
                case FETCH_STATE.ReadID:                    
                    _tileId = _vram[(ushort)(_mapAddr - TILE_DATA_START + _tileIndex)];
                    _fetchState = FETCH_STATE.ReadData0;                    
                    break;

                case FETCH_STATE.ReadData0:
                    offset = (ushort)(TILE_DATA_START + _tileId * 16);
                    addr = (ushort)(offset + (ushort)(_tileLine * 2));

                    _low = _vram[addr - TILE_DATA_START];                    
                    
                    _fetchState = FETCH_STATE.ReadData1;
                    break;

                case FETCH_STATE.ReadData1:
                    offset = (ushort)(TILE_DATA_START + _tileId * 16);
                    addr = (ushort)(offset + (ushort)(_tileLine * 2));
                    _high = _vram[addr + 1 - TILE_DATA_START];  

                    _fetchState = FETCH_STATE.Push;
                    break;

                case FETCH_STATE.Push:
                    if (_pixelFetcherQueue.Size() <= 8)
                    {    
                        for (int i = 7; i >= 0; i--)
                        {
                            _pixelFetcherQueue.Enqueue(_high.Bit(i) * 2 + _low.Bit(i));
                        }
                        
                        _tileIndex++;

                        _fetchState = FETCH_STATE.ReadID;
                    }                    
                    break;
            }
        }
    }
}