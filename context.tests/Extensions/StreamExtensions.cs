using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace context.tests.Extensions
{
    public static class StreamExtensions
    {
        public static Stream ToStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
