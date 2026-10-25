namespace GBCore
{
    public class Input
    {
        private const int SELECT_BUTTON_KEYS_BIT = 5;
        private const int SELECT_DIRECTION_KEYS_BIT = 4;
        private const int DOWN_OR_START = 3;
        private const int UP_OR_SELECT = 2;
        private const int LEFT_OR_B = 1;
        private const int RIGHT_OR_A = 0;

        private bool _selectButtons;
        private bool _selectDpad;

        private Keys _keys;

        public Input()
        {
            
        }

        public void SetKeys(Keys keys)
        {
            _keys = keys;
        }

        public byte Read()
        {
            byte result = 0xFF;

            if(_keys != null)
            {
                if(_selectButtons)
                {
                    result.SetBit(SELECT_BUTTON_KEYS_BIT, false);
                    if(_keys.Select)
                    {
                        result.SetBit(UP_OR_SELECT, false);
                    }
                    if(_keys.Start)
                    {
                        result.SetBit(DOWN_OR_START, false);
                    }
                    if(_keys.B)
                    {
                        result.SetBit(LEFT_OR_B, false);
                    }
                    if(_keys.A)
                    {
                        result.SetBit(RIGHT_OR_A, false);
                    }
                }
                if (_selectDpad)
                {
                    result.SetBit(SELECT_DIRECTION_KEYS_BIT, false);
                    if(_keys.Up)
                    {
                        result.SetBit(UP_OR_SELECT, false);
                    }
                    if(_keys.Down)
                    {
                        result.SetBit(DOWN_OR_START, false);
                    }
                    if(_keys.Left)
                    {
                        result.SetBit(LEFT_OR_B, false);
                    }
                    if(_keys.Right)
                    {
                        result.SetBit(RIGHT_OR_A, false);
                    }
                }
                return result;
            }

            // default to no keys pressed
            return result;
        }

        public void Write(byte data)
        {
            /*
            Bit 7 - Not used
            Bit 6 - Not used
            Bit 5 - P15 Select Button Keys (0=Select)
            Bit 4 - P14 Select Direction Keys (0=Select)
            Bit 3 - P13 Input Down or Start (0=Pressed) (Read Only)
            Bit 2 - P12 Input Up or Select (0=Pressed) (Read Only)
            Bit 1 - P11 Input Left or Button B (0=Pressed) (Read Only)
            Bit 0 - P10 Input Right or Button A (0=Pressed) (Read Only) 
            */

            _selectButtons = ((data & 0b00100000) >> 5) == 0;
            _selectDpad = ((data & 0b00010000) >> 4) == 0;
        }
    }
}