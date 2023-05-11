using MicroHardness.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Globalization;

namespace MicroHardness.Services
{
    public static class HVService
    {
        public static List<ProvaHV> ReadFile(string path)
        {
            var lines = File.ReadAllLines(path);
            var lastLine = File.ReadAllLines(path).Length;

            var data = from line in lines.Skip(29).Take(lastLine - 30)
                       let split = line.Split(',')
                       select new ProvaHV
                       {
                           Test = int.Parse(split[2]),
                           HV = float.Parse(split[5], CultureInfo.InvariantCulture),
                           MeanDiag = float.Parse(split[10], CultureInfo.InvariantCulture),
                       };

            return data.ToList();
        }

        public static double[] Results(string path)
        {
            var lines = File.ReadAllLines(path);
            var lastLine = File.ReadAllLines(path).Length;

            List<double> hv = new List<double>();

            foreach (var line in lines.Skip(29).Take(lastLine - 30))
            {
                var split = line.Split(",");
                double actualData = double.Parse(split[5], CultureInfo.InvariantCulture);
                hv.Add(actualData);
            }

            double[] hvArray = hv.ToArray();

            return hvArray;
        }
    }
}