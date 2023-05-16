using ClosedXML.Excel;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microhardness.View;
using MicroHardness.Model;
using MicroHardness.Services;
using Microsoft.Win32;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MicroHardness.View
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Issues_Click(object sender, RoutedEventArgs e)
        {
            KnownIssues knownIssues = new KnownIssues();
            knownIssues.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new();
            aboutWindow.ShowDialog();
        }

        public void LoadCsv_Click(object sender, RoutedEventArgs e)
        {
            string microPath = @"\\svr2012\Laboratorio Analisi\Dati Strumenti\Microdurometro\2023";
            //string microPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            OpenFileDialog openFileDialog = new()
            {
                Filter = "CSV Files only (*.csv)|*.csv",
                InitialDirectory = microPath,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                string sampleCode = openFileDialog.SafeFileName;
                int removeExt = sampleCode.LastIndexOf(".");
                if (removeExt > 0) sampleCode = sampleCode.Remove(removeExt);
                rawData.DataContext = HVService.ReadFile(path);
                double[] hvArray = HVService.Results(path);

                double[] hvStatistics = hvArray;

                double quantileBottom = hvArray.Quantile(0.15);

                double quantileUpper = hvArray.Quantile(0.85);

                for (int i = 0; i < hvArray.Length; i++)
                {
                    double value = hvArray[i];
                    if (value < quantileBottom)
                    {
                        hvStatistics = hvStatistics.Where(value => value != hvArray[i]).ToArray();
                    }
                    else if (value > quantileUpper)
                    {
                        hvStatistics = hvStatistics.Where(value => value != hvArray[i]).ToArray();
                    }
                }

                int pointCount = hvStatistics.Length;

                double[] xAxis = DataGen.Consecutive(pointCount);
                double[] xAxisDouble = DataGen.Consecutive(pointCount);
                string[] xAxisString = new string[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    xAxisString[i] = xAxisDouble[i].ToString();
                }

                var pop = new ScottPlot.Statistics.Population(hvStatistics);

                double mean = Statistics.Mean(hvStatistics).Round(2);
                string meanString = mean.ToString();
                double std = Statistics.StandardDeviation(hvStatistics).Round(2);
                string stdString = std.ToString();
                double q25 = Statistics.LowerQuartile(hvStatistics).Round(2);
                string q25String = q25.ToString();
                double q75 = Statistics.UpperQuartile(hvStatistics).Round(2);
                string q75String = q75.ToString();
                double max = Statistics.Maximum(hvStatistics).Round(2);
                string maxString = max.ToString();
                double min = Statistics.Minimum(hvStatistics).Round(2);
                string minString = min.ToString();
                double median = Statistics.Median(hvStatistics).Round(2);
                string medianString = median.ToString();

                hvSample.Text = $"{sampleCode}";
                hvMeanStd.Text = $"{meanString} ± {stdString}";
                hv25.Text = $"{q25String}";
                hv75.Text = $"{q75String}";
                hvMax.Text = $"{maxString}";
                hvMin.Text = $"{minString}";
                hvMedian.Text = $"{medianString}";
                hvPointCount.Text = $"{pointCount}";

                var boxPlot = BoxPlot.Plot.AddPopulation(pop);
                boxPlot.DistributionCurve = false;
                boxPlot.ErrorBarAlignment = ScottPlot.HorizontalAlignment.Center;
                BoxPlot.Configuration.LeftClickDragPan = false;
                BoxPlot.Configuration.MiddleClickAutoAxis = false;
                BoxPlot.Configuration.Zoom = false;
                BoxPlot.Plot.Title($"{sampleCode}");
                BoxPlot.Plot.YLabel("HV");
                BoxPlot.Plot.XAxis.Ticks(false);

                BoxPlot.Refresh();

                var linePlot = LinePlot.Plot.AddScatterLines(xAxis, hvStatistics);
                linePlot.LineWidth = 4;
                LinePlot.Plot.AddHorizontalLine(mean);
                LinePlot.Plot.SetAxisLimits(-1, pointCount + 1, 0, max * 1.1);
                LinePlot.Configuration.LeftClickDragPan = false;
                LinePlot.Configuration.MiddleClickAutoAxis = false;
                LinePlot.Configuration.Zoom = false;
                LinePlot.Plot.Title($"{sampleCode}");
                LinePlot.Plot.YLabel("HV");
                LinePlot.Plot.XLabel("Prova");
                LinePlot.Plot.XAxis.ManualTickPositions(xAxis, xAxisString);

                LinePlot.Refresh();
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (hvSample.Text == "") return;

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{hvSample.Text}");
            Directory.CreateDirectory(path);

            TabControl.SetIsSelected(LineTab, true);
            MessageBox.Show("LinePlot stampato", "Avviso", MessageBoxButton.OK);
            TabControl.SetIsSelected(BoxTab, true);
            MessageBox.Show("BoxPlot stampato", "Avviso", MessageBoxButton.OK);

            LinePlot.Plot.SaveFig(path + $"/{hvSample.Text}_LinePlot.png");

            BoxPlot.Plot.SaveFig(path + $"/{hvSample.Text}_BoxPlot.png");

            TabControl.SetIsSelected(Data, true);

            List<string> reference = new()
            {
                "Campione:",
                "Media e Dev. Std.:",
                "Massimo:",
                "Minimo:",
                "Mediana:",
                "1º Quartile:",
                "3º Quartile:",
                "Quantità di misure:"
            };

            List<string> results = new()
            {
                hvSample.Text,
                hvMeanStd.Text,
                hvMax.Text,
                hvMin.Text,
                hvMedian.Text,
                hv25.Text,
                hv75.Text,
                hvPointCount.Text,
            };

            List<string> combined = new();
            int count = reference.Count >= results.Count ? reference.Count : results.Count;
            for (int i = 0; i < count; i++)
            {
                string firstColumn = reference.Count <= i ? "" : reference[i];
                string secondColumn = results.Count <= i ? "" : results[i];

                firstColumn += new string(' ', 19 - firstColumn.Length);

                combined.Add(string.Format("{0} {1}", firstColumn, secondColumn));
            }

            File.WriteAllLines(path + $"/{hvSample.Text}_Riasunto.txt", combined);

            using var book = new XLWorkbook();
            var worksheet = book.AddWorksheet("Dati");
            worksheet.Cell("A1").Value = "Prova";
            worksheet.Cell("B1").Value = "HV";
            worksheet.Cell("C1").Value = "Diag. Media";
            worksheet.Cell("A2").InsertData(rawData.ItemsSource as IEnumerable<ProvaHV>);
            book.SaveAs(path + $"/{hvSample.Text}_Dati.xlsx");
        }
    }
}