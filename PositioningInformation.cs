using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BG96Sharp
{
    public readonly struct PositioningInformation
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public double HorizontalPrecision { get; }
        public double Altitude { get; }
        public int Fix { get; }
        public double Cog { get; }
        public double SpeedKM { get; }
        public double SpeedKnots { get; }
        public DateTime Date { get; }
        public int NumSattelites { get; }

        public PositionDisplayFormatMode FormatMode { get; }

        public PositioningInformation(string result, PositionDisplayFormatMode mode)
        {
            FormatMode = mode;

            switch (mode)
            {
                case PositionDisplayFormatMode.LongLatLong:
                    {
                        //+QGPSLOC: <UTC>,<latitude>,<longitude>,<hdop>,<altitude>,<fix>,<cog>,<spkm>,<spkn>,<date>,<nsat>
                        var regex = Regex.Match(result, @"\+QGPSLOC: (\d+.\d+),(\d+.\d+),(.),(\d+.\d+),(.),(\d+.\d+),(\d+.\d+),(\d),(\d+.\d+),(\d+.\d+),(\d+.\d+),(\d+),(\d+)");
                        if (regex.Groups.Count < 13)
                            throw new Exception("Invalid result from QGPSLOC: " + result);

                        //[1] is time (in UTC)
                        Latitude = double.Parse(regex.Groups[2].Value);
                        Longitude = double.Parse(regex.Groups[4].Value);
                        HorizontalPrecision = double.Parse(regex.Groups[6].Value);
                        Altitude = double.Parse(regex.Groups[7].Value);
                        Fix = int.Parse(regex.Groups[8].Value);
                        Cog = double.Parse(regex.Groups[9].Value);
                        SpeedKM = double.Parse(regex.Groups[10].Value);
                        SpeedKnots = double.Parse(regex.Groups[11].Value);
                        //[12] is date (in UTC)
                        NumSattelites = int.Parse(regex.Groups[13].Value);

                        Date = DateTime.ParseExact(regex.Groups[1].Value + " " + regex.Groups[12].Value, "HHmmss.f ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    }
                    break;

                case PositionDisplayFormatMode.PositiveNegativeLatLong:
                    {

                        var regex = Regex.Match(result, @"\+QGPSLOC: (\d+.\d+),(-?\d+.\d+),(-?\d+.\d+),(\d+.\d+),(\d+.\d+),(\d),(\d+.\d+),(\d+.\d+),(\d+.\d+),(\d+),(\d+)");
                        if (regex.Groups.Count < 11)
                            throw new Exception("Invalid result from QGPSLOC: " + result);

                        //[1] is time (in UTC)
                        Latitude = double.Parse(regex.Groups[2].Value);
                        Longitude = double.Parse(regex.Groups[3].Value);
                        HorizontalPrecision = double.Parse(regex.Groups[4].Value);
                        Altitude = double.Parse(regex.Groups[5].Value);
                        Fix = int.Parse(regex.Groups[6].Value);
                        Cog = double.Parse(regex.Groups[7].Value);
                        SpeedKM = double.Parse(regex.Groups[8].Value);
                        SpeedKnots = double.Parse(regex.Groups[9].Value);
                        NumSattelites = int.Parse(regex.Groups[11].Value);

                        Date = DateTime.ParseExact(regex.Groups[1].Value + " " + regex.Groups[10].Value, "HHmmss.f ddMMyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
          
        }
    }
}