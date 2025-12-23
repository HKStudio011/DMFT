using System;
using System.Collections.Generic;
using System.Text;

namespace DMFT.Model
{
    static class StatusMessage
    {
        public const int New = 0;
        public const int Waiting = 1;
        public const int Downloading = 2;
        public const int Canceled = 3;
        public const int Success = 4;
        public const int Error = 99;
        public const int VideoAudioOriginError = 100;
        public const int VideoError = 101;
        public const int AudioOriginError = 102;
        public const int AudioOnlyError = 103;
    }
}
