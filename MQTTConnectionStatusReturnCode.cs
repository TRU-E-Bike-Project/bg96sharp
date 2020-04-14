namespace BG96Sharp
{
    public enum MQTTConnectionStatusReturnCode
    {
        Unknown = -1,
        ConnectionAccepted,
        RefusedUnacceptableProtocolVersion,
        RefusedIdenfitierRejected,
        RefusedServerUnavailable,
        RefusedAuthenticationFailed,
        RefusedNotAuthorized
    }
}