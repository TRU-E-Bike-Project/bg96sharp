namespace BG96Sharp
{
    public enum OpenMQTTNetworkClientResult
    {
        FailedToOpenNetwork = -1,
        Success = 0,
        WrongParameter = 1,
        MQTTIdentOccupied = 2,
        PDPActivateFailed = 3,
        DomainParseFailed = 4,
        NetworkDisconnect = 5
    }
}