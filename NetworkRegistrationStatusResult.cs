using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace BG96Sharp
{
    /// <summary>
    /// Provides basic network registration status information. For more information, check if this object is of type <see cref="FullNetworkRegistrationStatus"/>
    /// </summary>
    public class BaseNetworkRegistrationStatus
    {
        public BaseNetworkRegistrationStatus(NetworkRegistrationStatusCode status)
        {
            Status = status;
        }

        /// <inheritdoc cref="UnsolicitedNetworkRegistrationCode"/>
        public NetworkRegistrationStatusCode Status { get; private set; }

        /// <summary>
        /// Create a network registration status object.
        /// </summary>
        /// <param name="response">Response message from serial port</param>
        /// <returns>A <see cref="FullNetworkRegistrationStatus" /> if the data is available; otherwise, creates a <see cref="BaseNetworkRegistrationStatus"/></returns>
        public static BaseNetworkRegistrationStatus FromResponse(string response)
        {
            if (!response.StartsWith("+CREG"))
                throw new ArgumentException("Input does not start with required message.");

            var commaCount = response.Count(x => x == ',');
            if (commaCount == 0)
            {
                //it's only a base
                var result = Regex.Match(response, @"\+CREG: (\d)");
                return new BaseNetworkRegistrationStatus((NetworkRegistrationStatusCode)int.Parse(result.Groups[1].Value));
            }
            else
            {
                var result = Regex.Match(response, @"\+CREG: (\d),(\d)");
                return new FullNetworkRegistrationStatus((UnsolicitedNetworkRegistrationCode)int.Parse(result.Groups[1].Value), (NetworkRegistrationStatusCode)int.Parse(result.Groups[2].Value));
            }
        }
    }

    /// <summary>
    /// Provides full network registration information.
    /// </summary>
    public class FullNetworkRegistrationStatus : BaseNetworkRegistrationStatus
    {
        public FullNetworkRegistrationStatus(UnsolicitedNetworkRegistrationCode networkRegistrationUnsolicitedResultCodeState, NetworkRegistrationStatusCode networkRegistrationStatusCode) : base(networkRegistrationStatusCode)
        {
            NetworkRegistrationUnsolicitedResultCodeState = networkRegistrationUnsolicitedResultCodeState;
        }

        /// <summary>
        /// Current state of unsolicited network status messages. If enabled, the module will send messages when network status changes on its own.
        /// </summary>
        public UnsolicitedNetworkRegistrationCode NetworkRegistrationUnsolicitedResultCodeState { get; private set; } = UnsolicitedNetworkRegistrationCode.Unknown;

        //TODO: implement the rest of the properties that are available
    }

    /// <summary>
    /// Code representing whether the module will send unsolicited network registration information
    /// </summary>
    public enum UnsolicitedNetworkRegistrationCode
    {
        /// <summary>
        /// (Used by library) Unknown registration status
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// Disable network registration unsolicited result code
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// Enable network registration unsolicited result code, with status only
        /// </summary>
        EnabledStatOnly = 1,
        /// <summary>
        /// Enable network registration and location information unsolicited result code will full information
        /// </summary>
        EnabledFull = 2
    }

    /// <summary>
    /// Code representing current registration status on the mobile network.
    /// </summary>
    public enum NetworkRegistrationStatusCode
    {
        /// <summary>
        /// Not registered. MT is not currently searching an operator to register to.
        /// </summary>
        Unregistered = 0,
        /// <summary>
        /// Registered, home network.
        /// </summary>
        RegisteredHomeNetwork = 1,
        /// <summary>
        /// Not registered, but MT is currently trying to attach or searching an operator to register to.
        /// </summary>
        NotRegisteredSearching = 2,
        /// <summary>
        /// Registration denied.
        /// </summary>
        RegistrationDenied = 3,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 4,
        /// <summary>
        /// Registered on a roaming network.
        /// </summary>
        RegisteredRoaming = 5
    }
}
