using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BG96Sharp.CommonBoards
{
    public class AVNetTelusIoTBoard : BG96
    {
        public AVNetTelusIoTBoard(ILogger logger, string serialPort) : base(logger, serialPort, 20, 16, 21)
        {
        }
    }
}
