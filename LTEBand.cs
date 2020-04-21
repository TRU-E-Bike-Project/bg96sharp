using System;

namespace BG96Sharp
{
    [Flags]
    public enum LTEBand : long
    {
        LTE_B1 = 0x01,
        LTE_B2 = 0x02,
        LTE_B3 = 0x04,
        LTE_B4 = 0x08,
        LTE_B5 = 0x10,
        LTE_B8 = 0x80,
        LTE_B12 = 0x800,
        LTE_B13 = 0x1000,
        LTE_B18 = 0x20000,
        LTE_B19 = 0x40000,
        LTE_B20 = 0x80000,
        LTE_B26 = 0x2000000,
        LTE_B28 = 0x8000000,
        /// <summary>
        /// Available on Cat.M1 only
        /// </summary>
        LTE_B39 = 0x4000000000,
        LTE_NO_CHANGE = 0x40000000,
        CATM1_ANY = 0x400A0E189F,
        CATNB1_ANY = 0xA0E189F
    }
}