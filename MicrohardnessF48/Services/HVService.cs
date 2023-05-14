using MicroHardness.Model;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MicroHardness.Services
{
    public static class HVService
    {
        public static List<ProvaHV> ReadFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            int lastLine = File.ReadAllLines(path).Length;

            var data = from line in lines.Skip(29).Take(lastLine - 30)
                       let split = line.Split(',')
                       select new ProvaHV
                       {
                           Test = int.Parse(split[2]),
                           HV = float.Parse(split[5], CultureInfo.GetCultureInfo(1033)),
                           MeanDiag = float.Parse(split[10], CultureInfo.GetCultureInfo(1033)),
                       };

            return data.ToList();
        }

        public static double[] Results(string path)
        {
            string[] lines = File.ReadAllLines(path);
            int lastLine = File.ReadAllLines(path).Length;

            List<double> hv = new List<double>();

            foreach (var line in lines.Skip(29).Take(lastLine - 30))
            {
                string[] split = line.Split(',');
                double actualData = double.Parse(split[5], CultureInfo.GetCultureInfo(1033));
                hv.Add(actualData);
            }

            double[] hvArray = hv.ToArray();

            return hvArray;
        }
    }
}