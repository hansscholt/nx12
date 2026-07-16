using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static nx12.SplitClases;

namespace nx12
{
    public class NX20File
    {
        public List<SplitData> splitdata;
        public int iCol;
        public int iLevel;
    }
    public class NX20Writter
    {
        float fLastSeenBPM = 60;
        public void Write(string sPath, NX20File nx20File)
        {
            using (FileStream fs = new FileStream(sPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.ASCII.GetBytes("NX20"));   // signature
                bw.Write(BitConverter.GetBytes(0));          // nothing
                bw.Write(BitConverter.GetBytes(nx20File.iCol)); // column count
                bw.Write(BitConverter.GetBytes(0));          // lightmap
                bw.Write(BitConverter.GetBytes(0));          // trash count
                bw.Write(BitConverter.GetBytes(nx20File.splitdata.Count)); // splits count

                foreach (var split in nx20File.splitdata)
                {
                    WriteSplit(bw, split);
                }
            }
        }

        private void WriteSplit(BinaryWriter bw, SplitData split)
        {
            // flag (0 = no extra data)
            bw.Write(BitConverter.GetBytes(0));

            // the 'something' flag
            bw.Write(BitConverter.GetBytes(0));

            // division count
            bw.Write(BitConverter.GetBytes(split.divisionData.Count));

            foreach (var division in split.divisionData)
            {
                WriteDivision(bw, division);
            }
        }

        private void WriteDivision(BinaryWriter bw, DivisionData division)
        {
            if (division.timing.fBPM == 0)
            {
                //mimic the nx20 file
                division.timing.iSmooth = 2;

                //rescue the last valid bpm
                division.timing.fBPM = fLastSeenBPM;
            }
            else
            {
                fLastSeenBPM = division.timing.fBPM;
            }

            // --- TIMING ---
            bw.Write(BitConverter.GetBytes(division.timing.fTotalOffset));
            bw.Write(BitConverter.GetBytes(division.timing.fBPM));
            bw.Write(BitConverter.GetBytes(division.timing.fMystery));
            bw.Write(BitConverter.GetBytes(division.timing.fOffset));
            bw.Write(BitConverter.GetBytes(division.timing.fSpeed)); // non-inverted speed
            bw.Write((byte)division.timing.iBeatSplit);
            bw.Write((byte)division.timing.iBeatPerMeasure);
            bw.Write((byte)division.timing.iSmooth);
            bw.Write((byte)0);//??

            // division info
            //bw.Write(BitConverter.GetBytes(0));
            bw.Write(BitConverter.GetBytes(division.divisionInfo.Count));
            //bw.Write((byte)division.divisionInfo.Count);

            //score types
            //0 perfect
            //1 great
            //2 good
            //3 bad
            //4 miss
            //5 g
            //6 w
            //7 a
            //8 b
            //9 c
            // --- DIVISION  ---
            foreach (var info in division.divisionInfo)
            {
                bw.Write(BitConverter.GetBytes(info.iScore));
                bw.Write(BitConverter.GetBytes((short)info.iMin));
                bw.Write(BitConverter.GetBytes((short)info.iMax));
            }

            // --- ROWS ---
            bw.Write(BitConverter.GetBytes(division.timing.iRows));

            // --- STEPS ---
            for (int row = 0; row < division.timing.iRows; row++)
            {
                var rowSteps = division.step.FindAll(s => s.iRow == row);

                if (rowSteps.TrueForAll(s => s.bEmptyStep))
                {
                    bw.Write(new byte[] { 128, 0, 0, 0 });
                }
                else
                {
                    foreach (var step in rowSteps)
                    {
                        var nx20Step = NX102NX20(step);
                        bw.Write(new byte[] {
                            (byte)nx20Step.iNote,
                            (byte)nx20Step.iLayer,
                            (byte)nx20Step.iPlayer,
                            (byte)nx20Step.iSpecial
                        });
                    }
                }
            }
        }

        int NX20ItemCode(int NX10code)
        {
            switch (NX10code)
            {
                case 3: return 0;//action
                case 7: return 1;//shield
                case 11: return 2;//change
                case 15: return 3;//acc
                case 19: return 4;//weather
                case 23: return 5;//mine
                case 27: return 6;//mine layer
                case 31: return 7;//gauge break
                case 35: return 8;//drain
                case 39: return 9;//heart
                case 43: return 10;//x2
                case 47: return 11;//random
                case 51: return 12;//x3
                case 55: return 13;//x4
                case 59: return 14;//x8
                case 63: return 15;//x1
                case 67: return 16;//potion
                case 71: return 17;//up
                case 75: return 18;//right
                case 79: return 19;//down
                case 83: return 20;//left
                default:
                    Console.WriteLine($"unknown nx10 item:{NX10code}");
                    return 0;
            }
        }

        Step NX102NX20(Step nx10)
        {
            //nx10 uses a bunch of codes instead of layers
            var nx20 = new Step();
            switch (nx10.iNote)
            {
                // taps
                case 179: nx20.iNote = 67; nx20.iLayer = 3; break; // normal tap
                case 147: nx20.iNote = 67; nx20.iLayer = 1; break; // appear tap
                case 163: nx20.iNote = 67; nx20.iLayer = 2; break; // vanish tap
                case 131: nx20.iNote = 67; nx20.iLayer = 0; break; // hidden tap
                case 243: nx20.iNote = 99; nx20.iLayer = 3; break; // bonus tap
                case 211: nx20.iNote = 99; nx20.iLayer = 1; break; // bonus appear tap
                case 227: nx20.iNote = 99; nx20.iLayer = 2; break; // bonus vanish tap
                case 195: nx20.iNote = 99; nx20.iLayer = 3; break; // bonus hidden tap
                case 115: nx20.iNote = 35; nx20.iLayer = 3; break; // ghost tap
                case 83: nx20.iNote = 35; nx20.iLayer = 1; break; // ghost appear tap
                case 99: nx20.iNote = 35; nx20.iLayer = 2; break; // ghost vanish tap

                // heads
                case 180: nx20.iNote = 87; nx20.iLayer = 3; break; // normal head
                case 148: nx20.iNote = 87; nx20.iLayer = 1; break; // appear head
                case 164: nx20.iNote = 87; nx20.iLayer = 2; break; // vanish head
                case 132: nx20.iNote = 87; nx20.iLayer = 0; break; // hidden head
                case 244: nx20.iNote = 119; nx20.iLayer = 3; break; // bonus head
                case 212: nx20.iNote = 119; nx20.iLayer = 1; break; // bonus appear head
                case 228: nx20.iNote = 119; nx20.iLayer = 2; break; // bonus vanish head
                case 196: nx20.iNote = 119; nx20.iLayer = 0; break; // bonus hidden head
                case 116: nx20.iNote = 55; nx20.iLayer = 3; break; // ghost head
                case 84: nx20.iNote = 55; nx20.iLayer = 1; break; // ghost appear head
                case 100: nx20.iNote = 55; nx20.iLayer = 2; break; // ghost vanish head

                // bodies
                case 182: nx20.iNote = 91; nx20.iLayer = 3; break; // normal body
                case 150: nx20.iNote = 91; nx20.iLayer = 1; break; // appear body
                case 166: nx20.iNote = 91; nx20.iLayer = 2; break; // vanish body
                case 134: nx20.iNote = 91; nx20.iLayer = 0; break; // hidden body
                case 246: nx20.iNote = 123; nx20.iLayer = 3; break; // bonus body
                case 214: nx20.iNote = 123; nx20.iLayer = 1; break; // bonus appear body
                case 230: nx20.iNote = 123; nx20.iLayer = 2; break; // bonus vanish body
                case 198: nx20.iNote = 123; nx20.iLayer = 0; break; // bonus hidden body
                case 118: nx20.iNote = 59; nx20.iLayer = 3; break; // ghost body
                case 86: nx20.iNote = 59; nx20.iLayer = 1; break; // ghost appear body
                case 102: nx20.iNote = 59; nx20.iLayer = 2; break; // ghost vanish body

                // tails
                case 183: nx20.iNote = 95; nx20.iLayer = 3; break; // normal tail
                case 151: nx20.iNote = 95; nx20.iLayer = 1; break; // appear tail
                case 167: nx20.iNote = 95; nx20.iLayer = 2; break; // vanish tail
                case 135: nx20.iNote = 95; nx20.iLayer = 0; break; // hidden tail
                case 247: nx20.iNote = 127; nx20.iLayer = 3; break; // bonus tail
                case 215: nx20.iNote = 127; nx20.iLayer = 1; break; // bonus appear tail
                case 231: nx20.iNote = 127; nx20.iLayer = 2; break; // bonus vanish tail
                case 199: nx20.iNote = 127; nx20.iLayer = 0; break; // bonus hidden tail
                case 119: nx20.iNote = 63; nx20.iLayer = 3; break; // ghost tail
                case 87: nx20.iNote = 63; nx20.iLayer = 1; break; // ghost appear tail
                case 103: nx20.iNote = 63; nx20.iLayer = 2; break; // ghost vanish tail

                //for nxa items, seems like special 192 indicates an item and the player is used for the item code
                case 241: //item bonus(default)
                    nx20.iNote = 97;
                    nx20.iLayer = 3;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;
                case 225: //item vanish
                    nx20.iNote = 97;
                    nx20.iLayer = 2;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;
                case 209: //item appear
                    nx20.iNote = 97;
                    nx20.iLayer = 1;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;
                case 193: //item hidden
                    nx20.iNote = 97;
                    nx20.iLayer = 0;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;
                case 113: //item ghost
                    nx20.iNote = 33;
                    nx20.iLayer = 3;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;

                case 178: //division normal(these come from an older stepedit, before 5.4)
                case 242: //division bonus(default)
                    nx20.iNote = 66;
                    nx20.iLayer = 0;
                    nx20.iSpecial = 192;
                    nx20.iPlayer = NX20ItemCode(nx10.iLayer);
                    break;

                case 0: break;
                default:
                    Console.WriteLine($"unknown type: note:{nx10.iNote} layer:{nx10.iLayer}");
                    break;
            }

            switch (nx10.iLayer)
            {
                // taps
                case 0: nx20.iPlayer = 0; nx20.iSpecial = 0; break; // player bank 1 //default
                case 5: nx20.iPlayer = 1; nx20.iSpecial = 64; break; // player bank 2
                case 10: nx20.iPlayer = 2; nx20.iSpecial = 128; break; // player bank 3

                default:
                    break;
            }

            return new Step
            {
                iNote = nx20.iNote,
                iLayer = nx20.iLayer,
                iPlayer = nx20.iPlayer,
                iSpecial = nx20.iSpecial,
            };
        }
    }
}
