using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using nQuant;

namespace To8BitPng {
    class Program {
        private static readonly Arguments appArgs = new Arguments();

        static void Main(string[] args) {

            Parser.ParseArgumentsWithUsage(args, appArgs);

            if (!string.IsNullOrWhiteSpace(appArgs.Directory)) {
                ImageCompression.To8BitPngByFolder(appArgs.Directory);
            } else if (!string.IsNullOrWhiteSpace(appArgs.File)) {
                ImageCompression.To8BitPngByFile(appArgs.File);
            } else {
                // nothing was provided - convert current directory
                ImageCompression.To8BitPngByFolder(Environment.CurrentDirectory);
            }
        }
    }


    public class ImageCompression {
        /// <summary>
        /// Converts all PNGs in the folder to 8-bit color
        /// </summary>
        /// <exception cref="System.Exception">Catch all error for a number of scenarios</exception>
        /// <param name="folderPath"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        public static void To8BitPngByFolder(string folderPath, int maxDegreeOfParallelism = -1)
        {
            string[] files = Directory.GetFiles(folderPath, "*.png");
            var options = new ParallelOptions();

            if (maxDegreeOfParallelism > 0)
            {
                options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            }

            foreach (var item in files)
            {
                To8BitPngByFile(item);
            }
        }

        /// <summary>
        /// Convert a PNG file to 8-bit color
        /// Note that it's responsibility of the caller to handle exceptions
        /// </summary>
        /// <exception cref="System.Exception">Catch all error for a number of scenarios</exception>
        /// <param name="filePath"></param>
        public static void To8BitPngByFile(string filePath, bool checkColorDepth = true)
        {
            if (checkColorDepth)
            {
                // check to see if the file is already 8 bit
                if (IsPng8BitColorDepth(filePath))
                {
                    // no need to convert - it's already 8 bit.
                    return;
                }
            }

            var quantizer = new WuQuantizer();
            Image quantized = null;

            try
            {
                var bitmap = new Bitmap(filePath);
                quantized = quantizer.QuantizeImage(bitmap);

                // release the original bitmap so that it can be overwriten
                bitmap.Dispose();

                quantized.Save(filePath, ImageFormat.Png);
            } finally
            {
                if (quantized != null)
                {
                    quantized.Dispose();
                }
            }
        }

        public static bool IsPng8BitColorDepth(string filePath)
        {
            const int COLOR_TYPE_BITS_8 = 3;
            const int COLOR_DEPTH_8 = 8;
            int startReadPosition = 24;
            int colorDepthPositionOffset = 0;
            int colorTypePositionOffset = 1;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Position = startReadPosition; 

                    byte[] buffer = new byte[2];
                    fs.Read(buffer, 0, 2);

                    int colorDepthValue = buffer[colorDepthPositionOffset];
                    int colorTypeValue = buffer[colorTypePositionOffset];

                    return colorDepthValue == COLOR_DEPTH_8 && colorTypeValue == COLOR_TYPE_BITS_8;
                }
            } 
            catch (Exception)
            {
                return false;
            }
        }
    }

    [CommandLineArguments(CaseSensitive = false)]
    public class Arguments {
        [Argument(ArgumentType.AtMostOnce, HelpText = "File", LongName = "File", ShortName = "f")]
        public string File;

        [Argument(ArgumentType.AtMostOnce, HelpText = "Directory", LongName = "Directory", ShortName = "d")]
        public string Directory;
    }

}
