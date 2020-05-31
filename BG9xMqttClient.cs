using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GPSTrackerSharp.Common;
using Microsoft.Extensions.Logging;

namespace BG96Sharp
{
    public class BG9xMqttClient : IMqttClient
    {
        public string Hostname { get; }
        public int Port { get; }
        public string ClientID { get; }
        public string Username { get; }
        public string Password { get; }

        public int DefaultQOS { get; set; } = 1;
        public bool ShouldServerRetain { get; set; } = false;

        public bool IsConnected { get; private set; }

        public int CurrentTcpConnectId { get; private set; }

        private int _currentMessageId = 1; //TODO: we need to set a maximum here.

        private BG9x _module;
        private ILogger logger;

        public event MqttMessageReceivedHandler MessageReceived;
        public event EventHandler Connected;

        public BG9xMqttClient(BG9x module, ILogger logger, string hostname, int port, string clientID)
        {
            _module = module;
            this.logger = logger;
            Hostname = hostname;
            Port = port;
            ClientID = clientID;
        }

        public BG9xMqttClient(BG9x module, ILogger logger, string hostname, int port, string clientID, string username, string password) : this(module, logger, hostname, port, clientID)
        {
            Username = username;
            Password = password;
        }

        public Task<bool> SendMqttMessageAsync(string topic, string data) =>
            SendMqttMessageAsync(topic, Encoding.UTF8.GetBytes(data));

        public Task<bool> SendMqttMessageAsync(string topic, byte[] data) =>
            SendMqttMessageAsync(topic, data, CancellationToken.None);

        public async Task<bool> SendMqttMessageAsync(string topic, byte[] data, CancellationToken cancellationToken)
        {
            if (data.Length > 1548)
                throw new Exception("Message is too long!");

            if (_module.CurrentBinaryWriteTask != null)
                throw new Exception("Cannot create another binary write task when one is already queued!");

            var command =  $"AT+QMTPUB={CurrentTcpConnectId},{_currentMessageId},{DefaultQOS},{(ShouldServerRetain ? 1 : 0)},\"{topic}\"";
            _currentMessageId++;

            _module.WriteLine(command);
            var waitTaskSource = new TaskCompletionSource<bool>();
            _module.CurrentBinaryWriteTask = waitTaskSource;
            var waitTaskResult = await waitTaskSource.Task;
            _module.CurrentBinaryWriteTask = null; //very important!
            if (waitTaskResult)
            {
                _module.BaseSerialPort.Write(data, 0, data.Length);
                _module.BaseSerialPort.Write("\u001A");

            }
            else throw new Exception("Waiting for binary read signal failed!");

            var waitResponseSource = new TaskCompletionSource<CommandResult>();
            _module.AtCommandResultQueue.AddFirst(waitResponseSource); //push to top of queue, since we should be there anyways and we had received the > char
            var result = await waitResponseSource.Task;
            result.ThrowIfError();
            return !result.WasCMEError && result.Result == ATCommandResultCode.OK;
        }

        public async Task<bool> SubscribeToTopicAsync(string topic, int qos)
        {
            var command = $"AT+QMTSUB={CurrentTcpConnectId},{_currentMessageId},\"{topic}\",{qos}";
            _currentMessageId++;

            var (result, _) = await _module.SendATCommandAsync(new ATCommandWithReply(command, "+QMTSUB"));
            result.ThrowIfError();
            return result.Result == ATCommandResultCode.OK;
        }

        public async Task<bool> ConnectAsync()
        {
            //First, open MQTT network client
            var (tcpConnectId, result) = await OpenMQTTNetworkClientAsync();

            if (result == OpenMQTTNetworkClientResult.Success)
            {
                CurrentTcpConnectId = tcpConnectId;
                var (executionResult, returnCode) = await ConnectMQTTClientAsync();
                var success = executionResult == NetworkCommandExecutionResult.PacketSendSuccessfully &&
                       returnCode == MQTTConnectionStatusReturnCode.ConnectionAccepted;

                if (success)
                    Connected?.Invoke(this, EventArgs.Empty);

                return success;
            }
            else
            {
                logger.LogInformation("Failed to open network connection.");
                return false;
            }
        }

        public async Task<bool> CloseAsync()
        {
            var result = await _module.SendATCommandAsync(new ATCommandWithReply($"AT+QMTCLOSE={CurrentTcpConnectId}"));
            result.Result.ThrowIfError();
            return result.Response.Contains("0"); //bad. -1 is fail, 0 is success
        }

        private async Task<(int TCPConnectID, OpenMQTTNetworkClientResult Result)> OpenMQTTNetworkClientAsync()
        {
            var tcpConnectID = _module.UsedTcpConnectIds.Count;
            _module.UsedTcpConnectIds.Add(tcpConnectID);

            if (tcpConnectID < 0 || tcpConnectID > 5) throw new Exception("Could not open MQTT network client: Invalid TCP Connect ID. Range: 0-5; supplied: " + tcpConnectID);
            if (Encoding.UTF8.GetByteCount(Hostname) > 100) throw new Exception("Host name is too long; host name cannot exceed 100 bytes");
            if (Port < 1 || Port > 65535) throw new Exception("Port out of range");

            var result = await _module.SendATCommandAsync(new ATCommandWithReply($"AT+QMTOPEN={tcpConnectID},\"{Hostname}\",{Port}", "+QMTOPEN: "));
            result.Result.ThrowIfError();

            var r = Regex.Match(result.Response, @"\+QMTOPEN: (\d),(-?\d)"); //woops, turns out \d does not include if it's a negative number. who woulda thunk?
            return (int.Parse(r.Groups[1].Value), (OpenMQTTNetworkClientResult)int.Parse(r.Groups[2].Value));
        }
        
        /// <summary>
        /// Used to create an MQTT client and connect it to an MQTT server
        /// </summary>
        /// <param name="tcpConnectID">MQTT socket identifier. <see cref="OpenMQTTNetworkClient(int, string, int)"/></param>
        /// <param name="clientID">Client idenfitier string</param>
        /// <param name="username">(Optional) Username for MQTT authentication</param>
        /// <param name="password">(Optional) Password for MQTT authentication</param>
        /// <returns></returns>
        private async Task<(NetworkCommandExecutionResult executionResult, MQTTConnectionStatusReturnCode returnCode)> ConnectMQTTClientAsync()
        {
            var command = $"AT+QMTCONN={CurrentTcpConnectId},\"{ClientID}\"";
            if (!string.IsNullOrEmpty(Username))
                command += $",\"{Username}\",\"{Password}\"";

            var result = await _module.SendATCommandAsync(new ATCommandWithReply(command, "+QMTCONN:"));
            result.Result.ThrowIfError();

            var r = Regex.Match(result.Response, @"\+QMTCONN: (\d),(\d),?(\d)?");

            if (int.Parse(r.Groups[1].Value) != CurrentTcpConnectId)
                throw new Exception("Received result but tcpConnectID did not match!");

            return ((NetworkCommandExecutionResult)int.Parse(r.Groups[2].Value),
                r.Groups.Count > 3 ? (MQTTConnectionStatusReturnCode)int.Parse(r.Groups[3].Value) : MQTTConnectionStatusReturnCode.Unknown);
        }

        public Task<bool> SubscribeToTopicAsync(string topic) => throw new NotImplementedException();
    }
}
