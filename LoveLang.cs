using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoveLangInterpreter
{
    internal class LoveLang
    {
        /// <summary>
        /// Heart
        /// </summary>
        enum Heart
        {
            None,
            Red,
            Orange,
            Yellow,
            Green,
            Blue,
            Purple,
            Brown,
            Black,
            White
        }

        /// <summary>
        /// Heart byte data
        /// </summary>
        public static byte[][] heartBytes = new byte[][]
        {
            new byte[] { 0xE2, 0x9D, 0xA4, 0xEF, 0xB8, 0x8F },  // Red
            new byte[] { 0xF0, 0x9F, 0xA7, 0xA1 },              // Orange
            new byte[] { 0xF0, 0x9F, 0x92, 0x9B },              // Yellow
            new byte[] { 0xF0, 0x9F, 0x92, 0x9A },              // Green
            new byte[] { 0xF0, 0x9F, 0x92, 0x99 },              // Blue
            new byte[] { 0xF0, 0x9F, 0x92, 0x9C },              // Purple
            new byte[] { 0xF0, 0x9F, 0xA4, 0x8E },              // Brown
            new byte[] { 0xF0, 0x9F, 0x96, 0xA4 },              // Black
            new byte[] { 0xF0, 0x9F, 0xA4, 0x8D },              // White
        };

        /// <summary>
        /// Parse heart from code
        /// </summary>
        private static Heart ParseHeart(ref byte[] code, ref int pos)
        {
            Heart heart = Heart.None;
            try
            {
                for (int i = 0; i < heartBytes.GetLength(0); ++i)
                {
                    bool found = true;
                    for (int j = 0; j < heartBytes[i].Length; ++j)
                    {
                        if (code[pos + j] != heartBytes[i][j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        heart = (Heart)(i + 1);
                        pos += heartBytes[i].Length;
                        break;
                    }
                }
            }
            catch (Exception) { }

            return heart;
        }

        /// <summary>
        /// Parse a number from code
        /// </summary>
        private static byte ParseNumber(ref List<Heart> code, ref int pos)
        {
            byte number = 0;
            while (pos < code.Count)
            {
                Heart heart = code[pos];
                if (heart != Heart.Black && heart != Heart.White)
                    break;
                number = (byte)((number << 1) | ((heart == Heart.White) ? 1 : 0));
                pos++;
            }
            return number;
        }

        /// <summary>
        /// Check value and get appropriate flag settings
        /// </summary>
        private static int SetFlagsFromVal(byte value)
        {
            int flags = 0;
            if (value == 0)
                flags |= 1;
            if (value >= 0x80)
                flags |= 2;
            return flags;
        }

        /// <summary>
        /// Check carry flag set
        /// </summary>
        private static int CheckCarry(byte value, byte oldValue, bool sub)
        {
            return ((sub && value > oldValue) || (!sub && value < oldValue)) ? 4 : 0;
        }

        /// <summary>
        /// Jump
        /// </summary>
        private static void Jump(ref List<Heart> code, ref int pos, sbyte jump)
        {
            try
            {
                while (jump != 0)
                {
                    if (jump < 0)
                    {
                        // Backwards
                        if (code[--pos] == Heart.Brown)
                        {
                            if (++jump == 0)
                            {
                                ++pos;
                                return;
                            }
                        }
                    }
                    else
                    {
                        // Forwards
                        if (code[pos++] == Heart.Brown)
                        {
                            if (--jump == 0)
                                return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                pos = Math.Max(Math.Min(pos, code.Count), 0);
            }
        }

        /// <summary>
        /// Interpret LoveLang code
        /// </summary>
        public static string Interpret(string code, string input)
        {
            // Get heart code
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);
            int codePos = 0;
            List<Heart> codeHearts = new List<Heart>();
            while (codePos < codeBytes.Length)
            {
                Heart heart = ParseHeart(ref codeBytes, ref codePos);
                if (heart != Heart.None)
                    codeHearts.Add(heart);
                else
                    ++codePos;
            }
            codePos = 0;

            // Prepare cells
            byte[] cells = new byte[0x1000];
            byte tempCell = 0;
            int cellPos = 0;

            // Prepare input and output
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            List<byte> outputBytes = new List<byte>();
            int inputPos = 0;

            // Prepare operations
            byte opValue = 0;
            int flags = 0;

            while (codePos < codeHearts.Count)
            {
                // Get instruction
                Heart heart1 = codeHearts[codePos++];
                Heart heart2 = Heart.None;
                if (heart1 != Heart.Brown && codePos < codeHearts.Count)
                {
                    heart2 = codeHearts[codePos++];
                    if (heart2 == Heart.Black || heart2 == Heart.White)
                        --codePos;
                }

                switch (heart1)
                {
                    case Heart.Red:
                        // Red
                        switch (heart2)
                        {
                            case Heart.Red:
                                // MOVE 1 CELL LEFT
                                cellPos = (cellPos - 1) & 0xFFF;
                                break;

                            case Heart.Orange:
                                // MOVE 1 CELL RIGHT
                                cellPos = (cellPos + 1) & 0xFFF;
                                break;

                            case Heart.Yellow:
                                // INPUT TO CELL
                                if (inputPos < inputBytes.Length)
                                {
                                    cells[cellPos] = inputBytes[inputPos++];
                                    flags = SetFlagsFromVal(cells[cellPos]);
                                }
                                else
                                    flags |= 4;
                                break;

                            case Heart.Green:
                                // INPUT TO TEMP
                                if (inputPos < inputBytes.Length)
                                {
                                    tempCell = inputBytes[inputPos++];
                                    flags = SetFlagsFromVal(tempCell);
                                }
                                else
                                    flags |= 4;
                                break;

                            case Heart.Blue:
                                // OUTPUT FROM CELL
                                outputBytes.Add(cells[cellPos]);
                                break;

                            case Heart.Purple:
                                // OUTPUT FROM TEMP
                                outputBytes.Add(tempCell);
                                break;

                            case Heart.Black:
                            case Heart.White:
                                // COPY IMMEDIATE TO TEMP
                                tempCell = ParseNumber(ref codeHearts, ref codePos);
                                flags = SetFlagsFromVal(tempCell);
                                break;
                        }
                        break;

                    case Heart.Orange:
                        // Orange
                        switch (heart2)
                        {
                            case Heart.Red:
                                // DECREMENT CELL
                                opValue = cells[cellPos]--;
                                flags = SetFlagsFromVal(cells[cellPos]) | CheckCarry(cells[cellPos], opValue, true);
                                break;

                            case Heart.Orange:
                                // INCREMENT CELL
                                opValue = cells[cellPos]++;
                                flags = SetFlagsFromVal(cells[cellPos]) | CheckCarry(cells[cellPos], opValue, false);
                                break;

                            case Heart.Yellow:
                                // DECREMENT TEMP
                                opValue = tempCell--;
                                flags = SetFlagsFromVal(tempCell) | CheckCarry(tempCell, opValue, true);
                                break;

                            case Heart.Green:
                                // INCREMENT TEMP
                                opValue = tempCell++;
                                flags = SetFlagsFromVal(tempCell) | CheckCarry(tempCell, opValue, false);
                                break;

                            case Heart.Blue:
                                // COPY CELL TO TEMP
                                tempCell = cells[cellPos];
                                flags = SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Purple:
                                // COPY TEMP TO CELL
                                cells[cellPos] = tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]);
                                break;
                        }
                        break;

                    case Heart.Yellow:
                        // Yellow
                        switch (heart2)
                        {
                            case Heart.Red:
                                // SHIFT TEMP 1 BIT LEFT
                                flags = (tempCell & 0x80) >> 5;
                                tempCell = (byte)((tempCell << 1) & 0xFE);
                                flags |= SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Orange:
                                // SHIFT TEMP 1 BIT RIGHT
                                flags = (tempCell & 1) << 2;
                                tempCell = (byte)((tempCell >> 1) & 0x7F);
                                flags |= SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Yellow:
                                // ROTATE TEMP 1 BIT LEFT
                                opValue = tempCell;
                                tempCell = (byte)(((tempCell << 1) & 0xFE) | ((flags >> 2) & 1));
                                flags = ((opValue & 0x80) >> 5) | SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Green:
                                // ROTATE TEMP 1 BIT RIGHT
                                opValue = tempCell;
                                tempCell = (byte)(((tempCell >> 1) & 0x7F) | ((flags << 5) & 0x80));
                                flags = ((opValue & 1) << 2) | SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Blue:
                                // TEST CELL
                                flags = SetFlagsFromVal(cells[cellPos]);
                                break;

                            case Heart.Purple:
                                // TEST TEMP
                                flags = SetFlagsFromVal(tempCell);
                                break;
                        }
                        break;

                    case Heart.Green:
                        // Green
                        switch (heart2)
                        {
                            case Heart.Red:
                                // TEMP + CELL -> TEMP
                                opValue = tempCell;
                                tempCell += cells[cellPos];
                                flags = SetFlagsFromVal(tempCell) | CheckCarry(tempCell, opValue, false);
                                break;

                            case Heart.Orange:
                                // TEMP - CELL -> TEMP
                                opValue = tempCell;
                                tempCell -= cells[cellPos];
                                flags = SetFlagsFromVal(tempCell) | CheckCarry(tempCell, opValue, true);
                                break;

                            case Heart.Yellow:
                                // CMP TEMP AND CELL
                                opValue = (byte)(tempCell - cells[cellPos]);
                                flags = SetFlagsFromVal(opValue) | CheckCarry(opValue, tempCell, true);
                                break;

                            case Heart.Green:
                                // TEMP & CELL -> TEMP
                                tempCell &= cells[cellPos];
                                flags = SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Blue:
                                // TEMP | CELL -> TEMP
                                tempCell |= cells[cellPos];
                                flags = SetFlagsFromVal(tempCell);
                                break;

                            case Heart.Purple:
                                // TEMP ^ CELL -> TEMP
                                tempCell ^= cells[cellPos];
                                flags = SetFlagsFromVal(tempCell);
                                break;
                        }
                        break;

                    case Heart.Blue:
                        // Blue
                        switch (heart2)
                        {
                            case Heart.Red:
                                // TEMP + CELL -> CELL
                                opValue = cells[cellPos];
                                cells[cellPos] += tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]) | CheckCarry(cells[cellPos], opValue, false);
                                break;

                            case Heart.Orange:
                                // TEMP - CELL -> CELL
                                opValue = cells[cellPos];
                                cells[cellPos] -= tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]) | CheckCarry(cells[cellPos], opValue, true);
                                break;

                            case Heart.Yellow:
                                // CMP CELL & TEMP
                                opValue = (byte)(cells[cellPos] - tempCell);
                                flags = SetFlagsFromVal(opValue) | CheckCarry(opValue, cells[cellPos], true);
                                break;

                            case Heart.Green:
                                // TEMP & CELL -> CELL
                                cells[cellPos] &= tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]);
                                break;

                            case Heart.Blue:
                                // TEMP | CELL -> CELL
                                cells[cellPos] |= tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]);
                                break;

                            case Heart.Purple:
                                // TEMP ^ CELL -> CELL
                                cells[cellPos] ^= tempCell;
                                flags = SetFlagsFromVal(cells[cellPos]);
                                break;
                        }
                        break;

                    case Heart.Purple:
                        // Purple
                        switch (heart2)
                        {
                            case Heart.Red:
                                // JUMP IF ZERO
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 1) != 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Orange:
                                // JUMP IF CARRY
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 4) != 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Yellow:
                                // JUMP IF NEGATIVE
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 2) != 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Green:
                                // JUMP IF NOT ZERO
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 1) == 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Blue:
                                // JUMP IF NOT CARRY
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 4) == 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Purple:
                                // JUMP IF NOT NEGATIVE
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                if ((flags & 2) == 0)
                                    Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;

                            case Heart.Black:
                            case Heart.White:
                                // JUMP
                                opValue = ParseNumber(ref codeHearts, ref codePos);
                                Jump(ref codeHearts, ref codePos, (sbyte)opValue);
                                break;
                        }
                        break;
                }
            }

            return Encoding.UTF8.GetString(outputBytes.ToArray());
        }
    }
}
