using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BG96Sharp
{
    public readonly struct Operator
    {
        public int Mode { get; }
        public int Format { get; }
        public string Oper { get; }
        public int Act { get; }

        public bool NoOperSelected { get; }

        public Operator(string result)
        {
            if (result.Contains("+CME ERROR:"))
                throw new Exception("Error with AT+COPS?: " + result);

            var r = Regex.Match(result, @"\+COPS: (\d),(\d),(.+),(\d)");
            Mode = int.Parse(r.Groups[1].Value);
            if (r.Groups.Count > 2)
            {
                Format = int.Parse(r.Groups[2].Value);
                Oper = r.Groups[3].Value;
                Act = int.Parse(r.Groups[4].Value);
                NoOperSelected = false;
            }
            else
            {
                Format = -1;
                Oper = "";
                Act = -1;
                NoOperSelected = true;
            }
        }

        public override string ToString() => $"{Mode},{Format},{Oper},{Act},{NoOperSelected}";
    }

}
