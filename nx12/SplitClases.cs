using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nx12
{
    public class SplitClases
    {
        public class SplitData
        {
            public int iCol;
            public int iCurrentSplit;
            public float fHeight;
            public List<DivisionData> divisionData;
        }

        public class DivisionData
        {
            public float fSpace;
            public int iCurrentDivision;
            public List<DivisionInfo> divisionInfo;
            public Timing timing;
            public List<Step> step;
        }

        public struct Step
        {
            public int iRow;
            public int iCol;
            public int iPlayer;
            public int iLayer;
            public int iNote;
            public int iSpecial;
            public bool bEmptyStep;
        }

        public struct DivisionInfo
        {
            public int iScore;
            public int iMin;
            public int iMax;
        }

        public struct Timing
        {
            public float fTotalOffset;
            public float fBPM;
            public float fMystery;
            public float fOffset;
            public float fSpeed;
            public int iBeatSplit;
            public bool bZeroTickCount;
            public int iBeatPerMeasure;
            public int iSmooth;
            public int iRows;
        }
    }
}
