namespace BG96Sharp
{
    public class SerialPortPath
    {
        public static string LINUX_DEFAULT_USB = "/dev/ttyUSB2";
        public static string LINUX_DEBUG_USB = "/tmp/slsnif_pty";
        public static string LINUX_RPI_DEFAULT_UART = "/dev/serial0"; //Note: you need to run ``sudo raspi-config``, go to interfacing options, serial, select No for console login and Yes for enable
        public static string WINDOWS_DEFAULT_USB_COM_13 = "COM13";
        public static string WINDOWS_DEFAULT_USB_COM_16 = "COM16";
    }
}