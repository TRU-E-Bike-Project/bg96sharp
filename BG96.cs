using Microsoft.Extensions.Logging;

namespace BG96Sharp
{
    // ReSharper disable once InconsistentNaming
    public class BG96 : BG9x
    {
        public BG96(ILogger logger, string serialPort, int enablePin, int resetPin, int pinPowerKey) : base(logger, serialPort, enablePin, resetPin, pinPowerKey)
        {
        }
    }
}
