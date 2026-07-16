using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static nx12.SplitClases;

namespace nx12
{
    class NX10File
    {
        public List<SplitData> splitdata;
        public int iCol;
        public int iLevel;
    }

    class NX10Reader
    {
        public NX10File? Read(string sPath)
        {
            NX10File nx10File = new NX10File();

            FileStream fs = new FileStream(sPath, FileMode.Open);

            int[] iBytesHeader = new int[] {
                4,//signature
                4,//nothing ?
                4,//cols                INT32 - Little Endian (DCBA)
                4,      
            };

            int[] iBytesStep = new int[]
            {
                //4,//division            INT32 - Little Endian (DCBA)
                4,//total offset        Float - Little Endian (DCBA)
                4,//bpm                 Float - Little Endian (DCBA)
                //1,//0.25, 0.5 ?         Float - Little Endian (DCBA)
                //1,//0.25, 0.5 ?         Float - Little Endian (DCBA)
                //1,//0.25, 0.5 ?         Float - Little Endian (DCBA)
                //1,//0.25, 0.5 ?         Float - Little Endian (DCBA)
                4,//mystery             Float - Little Endian (DCBA)
                4,//this block offset   Float - Little Endian (DCBA)
                4,//speed               Float - Little Endian (DCBA)
                4,//division info position
                1,//beatsplit
                1,//?
                1,//beat per measure
                1,//smooth
            };

            List<byte[]> lHeader = new List<byte[]>();
            byte[] bByte;
            for (int i = 0; i < iBytesHeader.Length; i++)
            {
                bByte = new byte[iBytesHeader[i]];
                fs.Read(bByte, 0, iBytesHeader[i]);
                lHeader.Add(bByte);
            }

            int iCol = BitConverter.ToInt32(lHeader[2], 0);
            nx10File.iCol = iCol;

            int iSplits = BitConverter.ToInt32(lHeader[3], 0);

            long iOldSplitPos = 0;
            long iOldDivisionPos = 0;
            long iOldNotePos = 0;
            long iOldDivisionInfoPos = 0;

            int iPos;

            List<SplitData> splitData = new List<SplitData>();
            for (int s = 0; s < iSplits; s++)
            {
                SplitData split = new SplitData();

                bByte = new byte[4];
                fs.Read(bByte, 0, 4);
                iOldSplitPos = fs.Position;

                iPos = BitConverter.ToInt32(bByte, 0);
                fs.Position = iPos;

                byte[] bBlock = new byte[4];
                fs.Read(bBlock, 0, 4);
                int iDivision = BitConverter.ToInt32(bBlock, 0);
                split.divisionData = new List<DivisionData>();
                for (int d = 0; d < iDivision; d++)
                {
                    bByte = new byte[4];
                    fs.Read(bByte, 0, 4);
                    iOldDivisionPos = fs.Position;
                    iPos = BitConverter.ToInt32(bByte, 0);
                    fs.Position = iPos;

                    List<byte[]> lStep = new List<byte[]>();
                    DivisionData divisionData = new DivisionData();
                    for (int i = 0; i < iBytesStep.Length; i++)
                    {
                        bByte = new byte[iBytesStep[i]];
                        fs.Read(bByte, 0, iBytesStep[i]);
                        lStep.Add(bByte);
                    }

                    float fTotalOffset = BitConverter.ToSingle(lStep[0], 0);
                    float fBPM = BitConverter.ToSingle(lStep[1], 0);
                    float fMystery = BitConverter.ToSingle(lStep[2], 0);
                    float fOffset = BitConverter.ToSingle(lStep[3], 0);
                    float fSpeed = BitConverter.ToSingle(lStep[4], 0);
                    int iDivisionPosition = BitConverter.ToInt32(lStep[5], 0);
                    int iBeatSplit = (int)(lStep[6][0]);
                    int iBeatPerMeasure = (int)(lStep[8][0]);
                    int iSmooth = (int)(lStep[9][0]);

                    divisionData.divisionInfo = new List<DivisionInfo>();
                    if (iDivisionPosition != 0)
                    {
                        iOldDivisionInfoPos = fs.Position;
                        fs.Position = iDivisionPosition;
                        string[] sString = new string[] { "Perfect", "Great", "Good", "Bad", "Miss", "StepG", "StepW", "StepA", "StepB", "StepC" };


                        int[] iMinValues = new int[10];
                        int[] iMaxValues = new int[10];
                        for (int di = 0; di < sString.Length; di++)
                        {
                            bByte = new byte[4];
                            fs.Read(bByte, 0, 4);
                            iMinValues[di] = BitConverter.ToInt16(bByte, 0);
                        }
                        for (int di = 0; di < sString.Length; di++)
                        {
                            bByte = new byte[4];
                            fs.Read(bByte, 0, 4);
                            iMaxValues[di] = BitConverter.ToInt16(bByte, 0);
                        }
                        for (int di = 0; di < sString.Length; di++)
                        {
                            if (iMinValues[di] == 0 && iMaxValues[di] == 0)
                            {
                                continue;
                            }
                            DivisionInfo divisionInfo = new DivisionInfo();
                            divisionInfo.iScore = di;
                            divisionInfo.iMin = iMinValues[di];
                            divisionInfo.iMax = iMaxValues[di];
                            divisionData.divisionInfo.Add(divisionInfo);
                        }
                        fs.Position = iOldDivisionInfoPos;
                    }

                    bByte = new byte[4];
                    fs.Read(bByte, 0, 4);
                    int iRows = BitConverter.ToInt32(bByte, 0);


                    divisionData.timing.fTotalOffset = fTotalOffset;
                    divisionData.timing.fBPM = fBPM;
                    divisionData.timing.fMystery = fMystery;
                    divisionData.timing.fOffset = fOffset;
                    divisionData.timing.fSpeed = fSpeed;
                    divisionData.timing.iBeatSplit = iBeatSplit;
                    divisionData.timing.iBeatPerMeasure = iBeatPerMeasure;
                    divisionData.timing.iSmooth = iSmooth;
                    divisionData.timing.iRows = iRows;

                    divisionData.step = new List<Step>();
                    for (int b = 0; b < iRows; b++)
                    {

                        bByte = new byte[4];
                        fs.Read(bByte, 0, 4);

                        if (bByte[0] == 0 && bByte[1] == 0 && bByte[2] == 0 && bByte[3] == 0)
                        {
                            //converter
                            for (int c = 0; c < iCol; c++)
                            {
                                Step step = new Step();
                                step.bEmptyStep = true;
                                step.iRow = b;
                                step.iCol = c;
                                divisionData.step.Add(step);
                            }
                        }
                        else
                        {
                            iPos = BitConverter.ToInt32(bByte, 0);
                            iOldNotePos = fs.Position;
                            fs.Position = iPos;


                            for (int c = 0; c < iCol; c++)
                            {
                                Step step = new Step();
                                step.iRow = b;
                                bByte = new byte[2];
                                fs.Read(bByte, 0, 2);
                                if (bByte[0] != 0 || bByte[1] != 0)   //at least one tap
                                {
                                    step.bEmptyStep = false;
                                    step.iNote = bByte[0];
                                    step.iLayer = bByte[1];
                                    step.iCol = c;
                                }

                                divisionData.step.Add(step);
                            }
                            fs.Position = iOldNotePos;
                        }
                    }

                    divisionData.iCurrentDivision = d;
                    split.divisionData.Add(divisionData);
                    split.iCurrentSplit = s;
                    fs.Position = iOldDivisionPos;
                }

                fs.Position = iOldSplitPos;
                splitData.Add(split);
            }
            nx10File.splitdata = splitData;

            fs.Close();

            return nx10File;
        }
    }
}
