using static nx12.SplitClases;
using System.ComponentModel;

namespace nx12
{
    internal class Program
    {
        static void Main(string[] args)
        {
            args = new string[5] { "fr.nx", "-da", "0", "-path", "c:/nx20" };
            if (args.Length < 1)
            {
                Console.WriteLine("program.exe file.nx -da <integer> -path <string>");
                return;
            }

            string fileName = args[0];
            float delayAdjust = 0;
            string outputPath = "NX20";


            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("-da", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (!float.TryParse(args[i + 1], out delayAdjust))
                        delayAdjust = 0;
                    i++;
                }
                else if (args[i].Equals("-path", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    outputPath = args[i + 1];
                    i++;
                }
            }

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"file not found: {fileName}");
                return;
            }

            Read(fileName, delayAdjust, outputPath);
        }


        static void Read(string sFile, float delayAdjust, string outFolder)
        {
            int iType = -1;//nx10, 1 = nx20, 2 = STF4            
            FileStream fs = new FileStream(sFile, FileMode.Open);
            List<byte[]> lHeader = new List<byte[]>();
            byte[] bByte;
            //for (int i = 0; i < 4; i++)
            {
                bByte = new byte[4];
                fs.Read(bByte, 0, 4);
                lHeader.Add(bByte);
            }

            if (lHeader[0][0] == 78 && lHeader[0][1] == 88 && lHeader[0][2] == 49 && lHeader[0][3] == 48)
                iType = 0;
            if (lHeader[0][0] == 78 && lHeader[0][1] == 88 && lHeader[0][2] == 50 && lHeader[0][3] == 48)
                iType = 1;
            if (lHeader[0][0] == 83 && lHeader[0][1] == 84 && lHeader[0][2] == 70 && lHeader[0][3] == 52)
                iType = 2;

            fs.Close();
            
            if (iType != 0)
            {
                Console.WriteLine("format is not nx10");
                return;
            }

            List<SplitData> splitData = new List<SplitData>();
            
            NX10Reader nx10Reader = new NX10Reader();

            NX10File nx10File = nx10Reader.Read(sFile);
            if (nx10File is null)
            {
                return;
            }

            var firstSplit = nx10File.splitdata[0];
            float originalDelay = firstSplit.divisionData[0].timing.fTotalOffset;
            Console.WriteLine($"file:{sFile} originalDelay:{originalDelay} newDelay:{(originalDelay + delayAdjust)}");

            //first, adjust the new delay to each division of the first split
            foreach (var division in firstSplit.divisionData)
            {
                division.timing.fOffset += delayAdjust;
            }

            //second, adjust the totaloffset of each divison of each split
            foreach (var split in nx10File.splitdata)
            {
                foreach (var division in split.divisionData)
                {
                    division.timing.fTotalOffset += delayAdjust;
                }
            }

            NX20File nx20File = new NX20File();
            nx20File.splitdata = nx10File.splitdata;
            nx20File.iCol = nx10File.iCol;
            nx20File.iLevel = nx10File.iLevel;

            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }

            NX20Writter nX20Writter = new NX20Writter();
            nX20Writter.Write(Path.Combine(outFolder,sFile), nx20File);
        }
    }
}
