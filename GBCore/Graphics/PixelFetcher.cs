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
        private Memory _ram;
        private byte _tileId;
        int[] _pixelData = new int[8];
        FixedSizeQueue _pixelFetcherQueue;

        public PixelFetcher(FixedSizeQueue pixelFetcherQueue, Memory ram) 
        {
            _ram = ram;
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

            switch (_fetchState)
            {
                case FETCH_STATE.ReadID:                    
                    _tileId = _ram.Read((ushort)(_mapAddr + _tileIndex));
                    _fetchState = FETCH_STATE.ReadData0;                    
                    break;

                case FETCH_STATE.ReadData0:
                    ushort offset = (ushort)(TILE_DATA_START + _tileId * 16);

                    ushort addr = (ushort)(offset + (ushort)(_tileLine * 2));

                    byte data = _ram.Read(addr);
                    for (int bitPos = 0; bitPos <= 7; bitPos++)
                    {
                        _pixelData[bitPos] = data >> bitPos & 1;
                    }

                    if(data != 0x00)
                    {

                    }
                    
                    _fetchState = FETCH_STATE.Push;
                    break;

                case FETCH_STATE.ReadData1:                    
                    _fetchState = FETCH_STATE.Push;
                    break;

                case FETCH_STATE.Push:
                    if (_pixelFetcherQueue.Size() <= 8)
                    {
                        for (int i = 7; i >= 0; i--)
                        {
                            _pixelFetcherQueue.Enqueue(_pixelData[i]);
                        }
                        
                        _tileIndex++;

                        _fetchState = FETCH_STATE.ReadID;
                    }                    
                    break;
            }
        }
    }
}