using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageTools.IO
{
    public interface IStreamingImageDecoder : IImageDecoder
    {
        IEnumerable<ImageBase> DecodeStream(ExtendedImage image, Stream stream);
    }
}
