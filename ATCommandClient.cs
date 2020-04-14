using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BG96Sharp
{
    public class ATCommandClient : IATCommandClient
    {
        protected string _serialPort;

        public SerialPort BaseSerialPort { get; protected set; }

        public bool IsConnected => BaseSerialPort.IsOpen;

        internal LinkedList<TaskCompletionSource<CommandResult>> AtCommandResultQueue = new LinkedList<TaskCompletionSource<CommandResult>>();
        internal List<KeyValuePair<string, TaskCompletionSource<string>>> AtCommandResponseResultQueue = new List<KeyValuePair<string, TaskCompletionSource<string>>>();

        private readonly object _serialLock = new object();
        internal TaskCompletionSource<bool> CurrentBinaryWriteTask = null; 

        public bool ATResultPresentationCodeMode { get; set; } = false;
        public bool EchoModeOn { get; set; } = true;
        public char LineTerminationCharacter { get; set; } = (char) 0x0D; //CR
        public char ResponseFormattingCharacter { get; set; } = (char) 0x0A; //LF
        
        public event EventHandler CommandLineReceived;

        private bool RequestCancel { get; set; } = false;

        private ILogger logger;

        private Thread _atListenThread;

        public ATCommandClient(string serialPort, ILogger logger)
        {
            _serialPort = serialPort;
            this.logger = logger;
        }

        public void CloseSerialPort()
        {
            BaseSerialPort.Close();
        }

        public void OpenSerialPort()
        {
            _atListenThread = new Thread(ReadDataLoop);
            BaseSerialPort.Open();
            BaseSerialPort.Encoding = Encoding.UTF8; //technically, this should be GSM but whatever
            _atListenThread.Start();
        }

        private void ReadDataLoop()
        {
            while (!RequestCancel)
            {
                var s = new StringBuilder();
                var binaryWriteSignal = false;
                while (true)
                {
                    if (RequestCancel) return;

                    var c = BaseSerialPort.ReadChar(); //this will block the thread until it reads something.
                    if (c < 0) 
                        break; //means we couldn't read
                    if (c == '>') //we hit an input thing, need to signal to start writing.
                    {
                        logger.LogInformation("Received binary start symbol");
                        binaryWriteSignal = true;
                        break;
                    }
                    if (c == ResponseFormattingCharacter)
                        break;
                    s.Append((char)c);
                }

                if (binaryWriteSignal)
                {
                    //TODO: start signal
                    if (CurrentBinaryWriteTask == null)
                        throw new Exception("Binary write task was null but received start character!");
                    CurrentBinaryWriteTask.SetResult(true);
                    continue;
                }

                logger.LogInformation("Received serial line: " + s.ToString().Replace("\n", "<LN>").Replace("\r", "<CR>"));

                var line = s.ToString().Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("AT+")) continue; //means that we are receiving what we sent (echo)

                if (line == "OK" || line == "ERROR")
                {
                    if (CurrentBinaryWriteTask != null)
                    {
                        CurrentBinaryWriteTask.SetResult(false);
                    }
                    else
                    {
                        try
                        {
                            AtCommandResultQueue.First.Value.SetResult(new CommandResult(line switch
                            {
                                "OK" => ATCommandResultCode.OK,
                                "ERROR" => ATCommandResultCode.Error
                            }));
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical("Error setting AtCommandResult value: " + ex.Message + "\n" + ex.StackTrace);
                            throw ex;
                        }
                        AtCommandResultQueue.RemoveFirst();
                    }
                }
                else if (line.StartsWith("+"))
                {
                    CommandLineReceived?.Invoke(this, new CommandLineReceivedEventArgs(line));

                    if (line.StartsWith("+QMTRECV"))
                    {
                        logger.LogInformation("Received MQTT message.");
                        //we got an MQTT message
                    }
                    else if (line.StartsWith("+CME ERROR"))
                    {
                        logger.LogWarning("Received CME error: " + line);
                        //just grab the first mofo I guess? I mean, hopefully they don't sit in the queue long enough... Especially if they have a CME error.
                        //if (_atCommandResponseResultQueue.Count > 0)
                        //    _atCommandResponseResultQueue.First().Value.SetResult(line);
                        AtCommandResultQueue.First.Value.SetResult(new CommandResult(true, line));
                        AtCommandResultQueue.RemoveFirst();
                    }
                    else
                    {
                        var firstItem = AtCommandResponseResultQueue.FirstOrDefault(x => line.StartsWith(x.Key));
                        if (!string.IsNullOrEmpty(firstItem.Key))
                        {
                            var removedItem =
                                AtCommandResponseResultQueue.Remove(firstItem);
                            firstItem.Value.SetResult(line);
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Received line but was not a response code nor a command response: \"" + line + "\"");
                }
            }
        }
        internal void WriteLine(string command)
        {
            lock (_serialLock)
            {
                BaseSerialPort.Write(command + LineTerminationCharacter);
            }
        }

        public Task<CommandResult> SendATCommandAsync(string command)
        {
            WriteLine(command);
            var completionSource = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            AtCommandResultQueue.AddLast(completionSource);
            return completionSource.Task;
        }

        public Task<CommandResult> SendATCommandAsync(IATCommand command)
        {
            if (command.HasReply)
                throw new Exception("Wrong method called! You should be doing something with the result!");
            return SendATCommandAsync(command.Command);
        }

        public async Task<(CommandResult Result, string Response)> SendATCommandAsync(IATCommandWithReply command)
        {
            var commandResultTaskCompletionSource = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            AtCommandResultQueue.AddLast(commandResultTaskCompletionSource);

            var commandResponseResultTaskCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var queueItem = new KeyValuePair<string, TaskCompletionSource<string>>(command.DesiredReply,
                commandResponseResultTaskCompletionSource);
            AtCommandResponseResultQueue.Add(queueItem);

            lock (_serialLock)
            {
                BaseSerialPort.Write(command.Command + LineTerminationCharacter);
            }

            var resultCommandResult = await commandResultTaskCompletionSource.Task;
            if (resultCommandResult.WasCMEError)
            {
                AtCommandResponseResultQueue.Remove(queueItem); //clean up, since we got ourselves an error
                return (commandResultTaskCompletionSource.Task.Result, resultCommandResult.CMEError);
            }

            await commandResponseResultTaskCompletionSource.Task;

            return (commandResultTaskCompletionSource.Task.Result, commandResponseResultTaskCompletionSource.Task.Result);
        }
    }

    public class CommandLineReceivedEventArgs : EventArgs
    {
        public CommandLineReceivedEventArgs(string commandLine)
        {
            CommandLine = commandLine;
        }

        public string CommandLine { get; }
    }

    public class CommandResult
    {
        public CommandResult(bool wasCmeError, string cmeError)
        {
            WasCMEError = wasCmeError;
            CMEError = cmeError;
        }

        public CommandResult(ATCommandResultCode result)
        {
            WasCMEError = false;
            Result = result;
        }

        public void ThrowIfError()
        {
            if (WasCMEError)
                throw new CmeErrorException(CMEError);
            if (Result != ATCommandResultCode.OK)
                throw new ATCommandResultCodeException(Result);
        }

        public bool WasCMEError { get; }
        public string CMEError { get; }
        public ATCommandResultCode Result { get; private set; } = ATCommandResultCode.Unknown;
    }
}
