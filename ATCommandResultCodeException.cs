using System;

namespace BG96Sharp
{
    public class ATCommandResultCodeException : Exception
    {
        public ATCommandResultCodeException(ATCommandResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public ATCommandResultCode ResultCode { get; }
    }
}