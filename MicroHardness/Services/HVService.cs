using MicroHardness.Model;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MicroHardness.Services
{
    public static class HVService
    {
        public static List<TestHV> ReadFile(string path)
        {
            //The program reads the file and pass the data to the main window
            var lines = File.ReadAllLines(path);
            var lastLine = File.ReadAllLines(path).Length;

            var data = from line in lines.Skip(29).Take(lastLine - 30)
                       let split = line.Split(',')
                       select new TestHV
                       {
                           Test = int.Parse(split[2]),
                           HV = float.Parse(split[5], CultureInfo.InvariantCulture),
                           MeanDiag = float.Parse(split[10], CultureInfo.InvariantCulture),
                       };

            return data.ToList();
        }

        public static double[] Results(string path)
        {
            //The program reads the file and uses only the data for calulations
            var lines = File.ReadAllLines(path);
            var lastLine = File.ReadAllLines(path).Length;

            List<double> hv = new();

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