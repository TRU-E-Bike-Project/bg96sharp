using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BG96Sharp.CommonBoards
{
    public class SixfabCellularIoTHat : BG96
    {
        public SixfabCellularIoTHat(ILogger logger, string serialPort) : base(logger, serialPort, 17, 18, 24)
        {
            PullEnableLow = true;
        }

        public override void ResetModule(bool saveConfig = true)
        {
            logger.LogInformation("Resetting module.");

            if (saveConfig)
            {
                SaveConfigurationsAsync();
                Thread.Sleep(200);
            }

            logger.LogInformation("Power cycling...");
            gpioController.Write(PowerKeyPin, 0);
            gpioController.Write(EnableWakeVBat, 1);

            Thread.Sleep(300);

            gpioController.Write(PowerKeyPin, 1);
            gpioController.Write(EnableWakeVBat, 0);

            Thread.Sleep(400);

            logger.LogInformation("Power cycle complete.");

        }
    }

    //Todo: need to rewrite GetIsReady() to support STATUS pin on this board
}
