using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GPSTrackerSharp.Common;
using Microsoft.Extensions.Logging;

namespace BG96Sharp
{
    // ReSharper disable once InconsistentNaming
    public abstract class BG9x : ATCommandClient
    {
        internal GpioController gpioController;

        public int ResetPin { get; }
        public int PowerKeyPin { get; }
        public int EnableWakeVBat { get; }
        public List<int> UsedTcpConnectIds { get; } = new List<int>();

        /// <summary>
        /// Some devices (Sixfab) require the pins to be pulled low to send power to the device, others require these pins to be driven high (AVNet). 
        /// </summary>
        public bool PullEnableLow { get; set; } = false;
        
        internal ILogger logger;

        //Mapping: D7 -> GPIO16; D11 -> GPIO20; D10 -> GPIO21
        protected BG9x(ILogger logger, string serialPort, int enablePin, int resetPin, int pinPowerKey) : base(serialPort, logger)
        {
            this.logger = logger;
            PowerKeyPin = pinPowerKey;
            EnableWakeVBat = enablePin;
            ResetPin = resetPin;

            gpioController = new GpioController(PinNumberingScheme.Logical);
            gpioController.OpenPin(PowerKeyPin, PinMode.Output);
            gpioController.OpenPin(ResetPin, PinMode.Output);
            gpioController.OpenPin(EnableWakeVBat, PinMode.Output);
        }

        /// <summary>
        /// Reset and start module.
        /// </summary>
        public virtual void Startup()
        {
            if (!GetIsReady())
                throw new Exception("Could not start BG96 module.");
        }

        /// <summary>
        /// Safely power down the module.
        /// </summary>
        public virtual async Task PowerDown()
        {
            logger.LogInformation("Powering down module.");
            var result = await SendATCommandAsync(CommonATCommands.PowerDown).ConfigureAwait(false); //should wait 65s
            CloseSerialPort();
        }

        protected virtual bool GetIsReady()
        {
            ResetModule(false);

            logger.LogInformation("Waiting for module to become ready.");
            var watch = Stopwatch.StartNew();
            var moduleBack = false;
            while (!moduleBack)
            {
                if (watch.ElapsedMilliseconds > 30000)
                {
                    //timed out.
                    throw new Exception("Serial port did not come online.");
                }

                moduleBack = SerialPort.GetPortNames().Contains(_serialPort);
                Thread.Sleep(100);
            }

            logger.LogInformation("Module came back online; sleeping to stop a permissions issue.");
            Thread.Sleep(5000); //add sleep to stop permission issue
            logger.LogInformation("Sleep complete, connecting to serial port.");

            var atUsbSerialPort = new SerialPort(_serialPort, 115200, Parity.None, 8, StopBits.One); //default to this, since USB is cool
            atUsbSerialPort.Encoding = Encoding.UTF8; //source: https://github.com/sixfab/Sixfab_RPi_CellularIoT_App_Shield/blob/master/cellulariot/cellulariot.py
            //I think technically the encoding is called "GSM" though....
            BaseSerialPort = atUsbSerialPort;
            OpenSerialPort();
            return true;
        }

        /// <summary>
        /// Saves configuration and resets the BG96 module
        /// </summary>
        public virtual void ResetModule(bool saveConfig = true)
        {
            logger.LogInformation("Resetting module.");
            //Heavily inspired by https://github.com/Avnet/BG96-driver/blob/master/BG96/BG96.cpp > BG96::reset(void)
            if (saveConfig)
            {
                SaveConfigurationsAsync();
                Thread.Sleep(200);
            }

            logger.LogInformation("Power cycling...");
            //disable
            gpioController.Write(ResetPin, 0);
            gpioController.Write(PowerKeyPin, 0);
            gpioController.Write(EnableWakeVBat, 0);

            Thread.Sleep(300);

            gpioController.Write(ResetPin, 1);
            gpioController.Write(PowerKeyPin, 1);
            gpioController.Write(EnableWakeVBat, 1);

            Thread.Sleep(400);

            gpioController.Write(ResetPin, 0);
            Thread.Sleep(10);
            logger.LogInformation("Power cycle complete.");
        }
        
        public async Task<CommandResult> SetGSMBandAsync(GSMBand gsmBand)
        {
            var command = $"AT+QCFG=\"band\",{gsmBand:X},{LTEBand.LTE_NO_CHANGE:X},{LTEBand.LTE_NO_CHANGE:X}";
            return await SendATCommandAsync(command).ConfigureAwait(false);
        }

        public async Task<CommandResult> SetCATM1BandAsync(LTEBand catm1band)
        {
            var command = $"AT+QCFG=\"band\",{GSMBand.GSM_NO_CHANGE:X},{catm1band:X},{LTEBand.LTE_NO_CHANGE:X}";
            return await SendATCommandAsync(command).ConfigureAwait(false);
        }

        public async Task<CommandResult> SetNBIOTBandAsync(LTEBand nbiotband)
        {
            var command = $"AT+QCFG=\"band\",{GSMBand.GSM_NO_CHANGE:X},{LTEBand.LTE_NO_CHANGE:X},{nbiotband:X}";
            return await SendATCommandAsync(command).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests current cellular band configuration from the device.
        /// </summary>
        /// <returns></returns>
        public async Task<CellularBandInfoCommandResult> GetBandConfigurationAsync()
        {
            var result = await SendATCommandAsync(CommonATCommands.GetBandConfiguration).ConfigureAwait(false);
            result.Result.ThrowIfError();

            var regexResult = Regex.Match(result.Response, @"\+QCFG: ""band"",(0[xX][0-9a-fA-F]+),(0[xX][0-9a-fA-F]+),(0[xX][0-9a-fA-F]+)");
            var res = new CellularBandInfoCommandResult(result.Result.Result, (GSMBand)Convert.ToInt32(regexResult.Groups[1].Value, 16), (LTEBand)Convert.ToInt64(regexResult.Groups[2].Value, 16), (LTEBand)Convert.ToInt64(regexResult.Groups[3].Value, 16));
            return res;
        }

        /// <summary>
        /// Sets the BG96 to operate in the supplied cellular mode. 
        /// This does not save the configuration to the device. 
        /// </summary>
        public async Task<CommandResult> SetCellularModeAsync(CellularMode mode)
        {
            logger.LogInformation("Setting cellular mode to " + mode.ToString());

            var result = await SendATCommandAsync($"AT+QCFG=\"nwscanseq\",0{(int)mode},1").ConfigureAwait(false);
            result.ThrowIfError();

            var scanMode = mode switch {
                CellularMode.CATM1_MODE => 3,
                CellularMode.CATNB1_MODE => 3,
                CellularMode.GSM_Mode => 1,
                CellularMode.AutoMode => 0,
                _ => throw new Exception("Invalid cellular mode")
            };

            result = await SendATCommandAsync($"AT+QCFG=\"nwscanmode\",{scanMode},1").ConfigureAwait(false);
            result.ThrowIfError();

            //TODO: this isn't supported on BG96-M according to docs. 
            var iotopMode = mode switch
            {
                CellularMode.CATM1_MODE => LteRatMode.CatM1,
                CellularMode.CATNB1_MODE => LteRatMode.CatNB1,
                _ => LteRatMode.CatM1CatNB1
            };

            result = await SendATCommandAsync($"AT+QCFG=\"iotopmode\",{(int)iotopMode},1").ConfigureAwait(false);
            result.ThrowIfError();
            return result;
        }

        /// <summary>
        /// Tells the device to save its current confurations to memory. 
        /// </summary>
        public Task<CommandResult> SaveConfigurationsAsync()
        {
            return SendATCommandAsync(CommonATCommands.SaveConfigurations);
        }

        public async Task<(CommandResult, string)> GetIccidAsync()
        {
            var commandResult = await SendATCommandAsync(CommonATCommands.GetICCID).ConfigureAwait(false);
            commandResult.Result.ThrowIfError();
            var iccid = Regex.Match(commandResult.Response, @"\+QCCID: (\d+)").Groups[1].Value;
            return (commandResult.Result, iccid);
        }
        
        /// <summary>
        /// Retrieves the signal quality.
        /// </summary>
        /// <returns>
        /// Returns tuple representing RSSI and bitrate error (in percent). 
        /// </returns>
        public async Task<(int rssi, int bitrate)> GetSignalQualityAsync()
        {
            var commandResult = await SendATCommandAsync(CommonATCommands.GetSignalQuality).ConfigureAwait(false);
            commandResult.Result.ThrowIfError();
            
            var result = Regex.Match(commandResult.Response, @"\+CSQ: (\d+),(\d+)");
            if (result.Groups.Count < 3) throw new Exception("Could not get signal quality");
            return (int.Parse(result.Groups[1].Value), int.Parse(result.Groups[2].Value));
        }

        /// <inheritdoc cref="AttachPacketServiceAsync"/>
        /// <seealso cref="AttachPacketServiceAsync"/>
        public Task<bool> ConnectToOperatorAsync() => AttachPacketServiceAsync();

        /// <summary>
        /// Attaches module to the Packet Service.
        /// See "AT Commands" Chapter 9.1 for more information.
        /// </summary>
        public async Task<bool> AttachPacketServiceAsync()
        {
            var commandResult = await SendATCommandAsync(CommonATCommands.SetPacketServiceStatus(true)).ConfigureAwait(false); 
            commandResult.ThrowIfError();
            return commandResult.Result == ATCommandResultCode.OK;
        }


        //public string GetNetworkRegStatus() => SendATCommand("AT+CREG?");

        /// <summary>
        /// Get current network registration status from device
        /// </summary>
        public async Task<BaseNetworkRegistrationStatus> GetNetworkRegistrationStatusAsync()
        {
            var commandResult = await SendATCommandAsync(CommonATCommands.GetNetworkRegistrationStatus).ConfigureAwait(false);
            commandResult.Result.ThrowIfError();
            var result = BaseNetworkRegistrationStatus.FromResponse(commandResult.Response);
            return result;
        }

        //public Operator GetOperator() => new Operator(SendATCommand("AT+COPS?"));

        public async Task<CommandResult> ConfigureAPNAsync(string apn, string username = "", string password = "", int apnContext = 1, int authenticationMode = 0)
        {
            var commandResult = await SendATCommandAsync($"AT+QICSGP={apnContext},1,\"{apn}\",\"{username}\",\"{password}\",{authenticationMode}");
            commandResult.ThrowIfError();
            return commandResult;
        }
        
        
        #region GNSS

        /// <summary>
        ///
        /// 
        /// </summary>
        /// <param name="mode">GNSS working mode</param>
        /// <param name="fixMaxTime">The maximum positioning time (unit: s). which indicate the response time of GNSS receiver while measuring the GNSS pseudo range, and the upper time limit of GNSS satellite searching. It also includes the time for demodulating the ephemeris data and calculating the position. 1-30-255 Maximum positioning time</param>
        /// <param name="fixMaxDistance">Accuracy threshold of positioning. Unit: m. 1-50-1000</param>
        /// <param name="fixCounts">Number of attempts for positioning. 0–1000. 0 indicates continuous positioning. Non-zero values indicate the actual number of attempts for positioning.</param>
        /// <param name="fixRate">The interval time between the first and second time positioning. Unit: s. 1–65535</param>
        public async Task TurnOnGNSSAsync(GNSSMode mode, int fixMaxTime = 30, int fixMaxDistance = 50, int fixCounts = 0, int fixRate = 1)
        {
            if (fixMaxTime < 1 || fixMaxTime > 255)
                throw new ArgumentException("Invalid value", nameof(fixMaxTime));

            if (fixMaxDistance < 1 || fixMaxDistance > 1000)
                throw new ArgumentException("Invalid value", nameof(fixMaxDistance));

            if (fixCounts < 0 || fixCounts > 1000)
                throw new ArgumentException("Invalid value", nameof(fixCounts));

            if (fixRate < 1 || fixRate > 65535)
                throw new ArgumentException("Invalid value", nameof(fixRate));

            (await SendATCommandAsync($"AT+QGPS={(int)mode},{fixMaxTime},{fixMaxDistance},{fixCounts},{fixRate}"))
                .ThrowIfError();
        }

        public async Task TurnOffGNSSAsync()
        {
            (await SendATCommandAsync("AT+QGPSEND"))
                .ThrowIfError();
        }

        ///// <summary>
        ///// TODO: document
        ///// </summary>
        ///// <param name="deleteType"></param>
        //public void DeleteGNSSAssistanceData(int deleteType)
        //{
        //    SendATCommand("AT+QGPSDEL=" + deleteType);
        //}

        public async Task<PositioningInformation> GetGNSSPositionAsync(PositionDisplayFormatMode mode = PositionDisplayFormatMode.PositiveNegativeLatLong)
        {
            var result = await SendATCommandAsync(new ATCommandWithReply("AT+QGPSLOC=" + (int)mode, "+QGPSLOC: "));
            result.Result.ThrowIfError();
            var response = new PositioningInformation(result.Response, mode);
            return response;
        }

        #endregion
    }
}
