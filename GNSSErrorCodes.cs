namespace BG96Sharp
{
    public enum GNSSErrorCodes
    {
        InvalidParameter = 501,
        OperationNotSupported = 502,
        GNSSSubsystemBusy = 503,
        SessionOngoing = 504,
        SessionNotActive = 505,
        OperationTimeout = 506,
        FunctionNotEnabled = 507,
        TimeInformationError = 508,
        XTRANotEnabled = 509,
        ValidityTimeOutOfRange = 510,
        InternalResourceError = 513,
        GNSSLocked = 514,
        EndByE911 = 515,
        NotFixedNow = 516,
        GeoFenceIDNotFound = 517,
        Unknown = 549
    }
}