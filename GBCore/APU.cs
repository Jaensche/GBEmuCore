using System;
using System.Collections.Generic;
using System.Text;

namespace GBCore
{
    public static class APU
    {
        public enum Direction
        {
            Left,
            Right
        }

        public static byte SWP(byte input)
        {
            byte result = (byte)(((input & 0xF0) >> 4) | ((input & 0x0F) << 4));

            return result;
        }

        public static ushort SWP(ushort input)
        {
            ushort result = (ushort)(((input & 0xFF00) >> 8) | ((input & 0x00FF) << 8));

            return result;
        }

        public static byte ROT(byte input, ref byte flags, bool carry, Direction direction)
        {
            byte result;
            bool newCarry;

            if (direction == Direction.Left)
            {
                result = (byte)(input << 1);
                newCarry = (input & 0b10000000) > 0;
            }
            else
            {
                result = (byte)(input >> 1);
                newCarry = (input & 0b00000001) > 0;
            }

            if (carry)
            {
                if (direction == Direction.Left)
                {
                    result += (byte)((input & 0b10000000) >> 7);
                }
                else
                {
                    result += (byte)((input & 0b00000001) << 7);
                }
            }
            else
            {
                if (direction == Direction.Left)
                {
                    result += (byte)((flags & 0b00010000) >> 4);
                }
                else
                {
                    result += (byte)((flags & 0b00010000) << 3);
                }
            }

            flags = newCarry ? flags |= (byte)Flags.C : flags &= (byte)~Flags.C;
            flags &= (byte)~Flags.N;
            flags &= (byte)~Flags.H;
            flags = result == 0 ? flags |= (byte)Flags.Z : flags &= (byte)~Flags.Z;

            return result;
        }        
    }
}
