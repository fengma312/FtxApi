using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FtxApi.Util
{
    public static class Util
    {
        private static DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0);

        public static long GetMillisecondsFromEpochStart()
        {
            return GetMillisecondsFromEpochStart(DateTime.UtcNow);
        }

        public static long GetMillisecondsFromEpochStart(DateTime time)
        {
            return (long)(time - _epochTime).TotalMilliseconds;
        }

        public static long GetSecondsFromEpochStart(DateTime time)
        {
            return (long)(time - _epochTime).TotalSeconds;
        }

        /// <summary>
        /// Decompress the byte array to UTF8 string
        /// </summary>
        /// <param name="input">byte array</param>
        /// <returns>UTF8 string</returns>
        public static string Decompress(byte[] input)
        {
            using (var stream = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];

                using (var memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while (count > 0);

                    return Encoding.UTF8.GetString(memory.ToArray());
                }
            }
        }

        /// <summary>
        /// Compress the UTF8 string to byte array.
        /// This method is only used in Unit Test in SDK.
        /// </summary>
        /// <param name="input">UTF8 string</param>
        /// <returns>byte array</returns>
        public static byte[] Compress(string input)
        {
            byte[] raw = Encoding.UTF8.GetBytes(input);

            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

    }
}
