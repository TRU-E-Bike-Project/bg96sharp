using System;
using System.Collections.Generic;
using System.Text;

namespace BG96Sharp
{
    /// <summary>
    /// Creates ATCommand objects for commonly used AT commands.
    /// </summary>
    public static class CommonATCommands
    {
        public static ATCommandWithReply PowerDown => new ATCommandWithReply("AT+QPOWD", "POWERED DOWN");
        public static ATCommandWithReply GetIMEI => new ATCommandWithReply("AT+CGSN");
        public static ATCommandWithReply GetFirmwareInfo => new ATCommandWithReply("AT+CGMR");
        public static ATCommandWithReply GetHardwareInfo => new ATCommandWithReply("AT+CGMM");
        public static ATCommandWithReply GetManufacturerInfo => new ATCommandWithReply("AT+CGMI");
        public static ATCommandWithReply GetBandConfiguration => new ATCommandWithReply("AT+QCFG=\"band\"", "+QCFG");
        public static BaseATCommand SaveConfigurations => new BaseATCommand("AT&W");

        public static ATCommandWithReply GetSignalQuality => new ATCommandWithReply("AT+CSQ");
        public static ATCommandWithReply GetNetworkInfo => new ATCommandWithReply("AT+QNWINFO");
        public static ATCommandWithReply GetMqttClientStatus => new ATCommandWithReply("AT+QMTCONN?");
        public static ATCommandWithReply GetNetworkRegistrationStatus => new ATCommandWithReply("AT+CREG?");

        public static BaseATCommand SetPacketServiceStatus(bool enable) => new BaseATCommand("AT+CGATT=" + (enable ? 1 : 0));
    }

    public interface IATCommand
    {
        bool HasReply { get; }
        string Command { get; }
    }

    public interface IATCommandWithReply : IATCommand
    {
        string DesiredReply { get; }
    }

    /// <summary>
    /// A default AT command, that will expect a <see cref="ATCommandResultCode"/> in response.
    /// </summary>
    public class BaseATCommand : IATCommand
    {
        public BaseATCommand(string command)
        {
            Command = command;
        }

        public string Command { get; set; }

        public virtual bool HasReply => false;
    }

    /// <summary>
    /// An AT command that will expect a different response than an <see cref="ATCommandResultCode"/>. If that is not desired behavior, use <see cref="BaseATCommand"/>
    /// </summary>
    public class ATCommandWithReply : BaseATCommand, IATCommandWithReply
    {
        public ATCommandWithReply(string command, string desiredReply) : base(command)
        {
            DesiredReply = desiredReply;
        }

        public ATCommandWithReply(string command) : base(command) { }

        public override bool HasReply => true;

        private string _desiredReply = string.Empty;

        public string DesiredReply
        {
            get => string.IsNullOrEmpty(_desiredReply) ? Command.Remove(0, 2).Split('=')[0].Replace("?", "") : _desiredReply;
            set => _desiredReply = value;
        }
    }

    public class OpenMqttNetworkClientCommand : IATCommandWithReply
    {
        public string DesiredReply => throw new NotImplementedException();

        public bool HasReply => true;

        public string Command => throw new NotImplementedException();
    }
}
