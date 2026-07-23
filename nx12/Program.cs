using static nx12.SplitClases;
using System.ComponentModel;

namespace nx12
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //args = new string[6] { "-s", "C:\\Pump\\NXSTEP\\nx2 1.60\\step\\c01", "-d", "c:/nx20", "-da", "0" };

            if (args.Length < 2)
            {
                Console.WriteLine("program.exe -s <source folder> -d <destination folder> -da <integer>");
                return;
            }

            string sourcePath = "";
            string outputPath = "NX20"; // default
            float delayAdjust = 0;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-s", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    sourcePath = args[i + 1];
                    i++;
                }
                else if (args[i].Equals("-d", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    outputPath = args[i + 1];
                    i++;
                }
                else if (args[i].Equals("-da", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (!float.TryParse(args[i + 1], out delayAdjust))
                        delayAdjust = 0;
                    i++;
                }
            }

            if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
            {
                Console.WriteLine($"source folder not found: {sourcePath}");
                return;
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string[] files = Directory.GetFiles(sourcePath, "*.nx");
            foreach (var file in files)
            {
                //Console.WriteLine($"> {Path.GetFileName(file)} (delayAdjust={delayAdjust}, output={outputPath})");
                Read(file, delayAdjust, outputPath);
                //if (Read(file, delayAdjust, outputPath))
                //    Console.WriteLine();
                //else
                //    Console.WriteLine("\t error");
            }
        }


        static void Read(string sourceFile, float delayAdjust, string destinationFolder)
        {
            int iType = -1;//nx10, 1 = nx20, 2 = STF4            
            FileStream fs = new FileStream(sourceFile, FileMode.Open);
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

            NX10File nx10File = nx10Reader.Read(sourceFile);
            if (nx10File is null)
            {
                return;
            }

            var firstSplit = nx10File.splitdata[0];
            float originalDelay = firstSplit.divisionData[0].timing.fTotalOffset;
            Console.WriteLine($"file:{sourceFile} originalDelay:{originalDelay} newDelay:{(originalDelay + delayAdjust)}");

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
            nx20File.bLightMap = nx10File.bLightMap;

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            NX20Writter nX20Writter = new NX20Writter();
            nX20Writter.Write(Path.Combine(destinationFolder, Path.GetFileName(sourceFile)), nx20File);
        }
    }
}
