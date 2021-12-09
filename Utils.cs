using System;
using System.Buffers;
using System.Security.Cryptography;

namespace CalendarReminder
{
    static class Utils
    {
        public static void Hash(SHA1 sha, DateTime time) => sha.TransformBlock(BitConverter.GetBytes(time.ToBinary()), 0, 8, null, 0);

        public static void Hash(SHA1 sha, string str)
        {
            if(str == null) str = string.Empty;
            int length = System.Text.Encoding.UTF8.GetByteCount(str);
            byte[] data = ArrayPool<byte>.Shared.Rent(length + 1);
            data[0] = (byte)length;
            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, data, 1);
            sha.TransformBlock(data, 0, length + 1, null, 0);
            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
