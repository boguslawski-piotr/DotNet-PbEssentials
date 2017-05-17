using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
    public interface ICompressor
    {
        MemoryStream Compress(Stream from);
        MemoryStream Decompress(Stream from);

        string Compress(string d);
        string Decompress(string d);
    }
}
