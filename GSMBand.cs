using System;

namespace BG96Sharp
{
    [Flags]
    public enum GSMBand
    {
        GSM_NO_CHANGE = 0,
        GSM_900 = 0x01,
        GSM_1800 = 0x02,
        GSM_850 = 0x04,
        GSM_1900 = 0x08,
        GSM_ANY = 0x0F
    }
}