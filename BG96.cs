
using GPSTrackerSharp.Common;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace BG96Sharp
{
    // ReSharper disable once InconsistentNaming
    public class BG96 : BG9x
    {
        public BG96(ILogger logger, string serialPort, int enablePin = 20, int resetPin = 16, int pinPowerKey = 21) : base(logger, serialPort, enablePin, resetPin, pinPowerKey)
        {
        }
    }
}
