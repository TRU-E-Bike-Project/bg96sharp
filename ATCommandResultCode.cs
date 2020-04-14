using System;
using System.Collections.Generic;
using System.Text;

namespace BG96Sharp
{
    /// <summary>
    /// Indicates the result of an AT command.
    /// See BG96_AT_Commands_Manual V2.3 page 20 for details.
    /// </summary>
    public enum ATCommandResultCode
    {
        Unknown = -1,
        /// <summary>
        /// Acknowledges execution of a command.
        /// </summary>
        OK = 0,
        /// <summary>
        /// A connection has been established. The DCE is moving from command mode to data mode.
        /// </summary>
        Connect = 1,
        /// <summary>
        /// The DCE has detected an incoming call signal from network.
        /// </summary>
        Ring = 2,
        /// <summary>
        /// The connection has been terminated or the attempt to establish a connection failed.
        /// </summary>
        NoCarrier = 3,
        /// <summary>
        /// Command not recognized, command line maximum length exceeded, parameter value invalid, or other problem with processing the command line.
        /// </summary>
        Error = 4,
        /// <summary>
        /// No dial tone detected.
        /// </summary>
        NoDialtone = 6,
        /// <summary>
        /// Engaged (busy) signal detected.
        /// </summary>
        Busy = 7,
        /// <summary>
        /// “@” (Wait for Quiet Answer) dial modifier was used, but remote ringing followed by five seconds of silence was not detected before expiration of the connection timer (S7).
        /// </summary>
        NoAnswer = 8
    }
}
