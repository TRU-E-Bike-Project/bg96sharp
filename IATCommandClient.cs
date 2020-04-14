using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BG96Sharp
{
    /// <summary>
    /// Specifies that a class provides a way to communicate with a device through AT commands
    /// </summary>
    public interface IATCommandClient
    {
        Task<CommandResult> SendATCommandAsync(string command);
        Task<CommandResult> SendATCommandAsync(IATCommand command);

        Task<(CommandResult Result, string Response)> SendATCommandAsync(IATCommandWithReply command);
    }
}
