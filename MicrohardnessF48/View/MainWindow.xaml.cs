using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microhardness.View;
using MicroHardness.Services;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        public void LoadCsv_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files only (*.csv)|*.csv",
                InitialDirectory = Environment.CurrentDirectory
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
                        hvStatistics = hvStatistics.Where(x => x != hvArray[i]).ToArray();
                    }
                    else if (value > quantileUpper)
                    {
                        hvStatistics = hvStatistics.Where(x => x != hvArray[i]).ToArray();
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

                double mean = pop.mean.Round(2);
                string meanString = mean.ToString();
                double std = pop.stdErr.Round(2);
                string stdString = std.ToString();
                double q25 = pop.Q1.Round(2);
                string q25String = q25.ToString();
                double q75 = pop.Q3.Round(2);
                string q75String = q75.ToString();
                double max = pop.max.Round(2);
                string maxString = max.ToString();
                double min = pop.min.Round(2);
                string minString = min.ToString();
                double median = pop.median.Round(2);
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
                BoxPlot.Plot.Title($"{sampleCode}");
                BoxPlot.Plot.YLabel("HV");
                BoxPlot.Plot.XAxis.Ticks(false);

                var linePlot = LinePlot.Plot.AddScatter(xAxis, hvStatistics);
                linePlot.LineWidth = 4;
                LinePlot.Plot.AddHorizontalLine(mean);
                LinePlot.Plot.YAxis.SetZoomInLimit(100);
                LinePlot.Plot.YAxis.SetZoomOutLimit(100);
                LinePlot.Plot.Title($"{sampleCode}");
                LinePlot.Plot.YLabel("HV");
                LinePlot.Plot.XLabel("Prova");
                LinePlot.Plot.XAxis.ManualTickPositions(xAxis, xAxisString);

                BoxPlot.Refresh();
                LinePlot.Refresh();
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (hvSample.Text == "") return;

            //string path = @"\\svr2012\Laboratorio Analisi\Dati Strumenti\Microdurometro\2023" + $"{hvSample.Text}";
            //string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{hvSample.Text}");

            SaveFileDialog saveAll = new SaveFileDialog
            {
                Filter = "All files (*.*)|*.*",
                InitialDirectory = Environment.CurrentDirectory,
                FileName = $"{hvSample.Text}"
            };
            if (saveAll.ShowDialog() == true)
            {
                string path = saveAll.FileName;
                Directory.CreateDirectory(path);
                LinePlot.Plot.SaveFig(path + $"/{hvSample.Text}_LinePlot.png");
                BoxPlot.Plot.SaveFig(path + $"/{hvSample.Text}_BoxPlot.png");

                List<string> reference = new List<string>()
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

                List<string> results = new List<string>()
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

                List<string> combined = new List<string>();
                int count = reference.Count >= results.Count ? reference.Count : results.Count;
                for (int i = 0; i < count; i++)
                {
                    string firstColumn = reference.Count <= i ? "" : reference[i];
                    string secondColumn = results.Count <= i ? "" : results[i];

                    firstColumn += new string(' ', 19 - firstColumn.Length);

                    combined.Add(string.Format("{0} {1}", firstColumn, secondColumn));
                }
                System.IO.File.WriteAllLines(path + $"/{hvSample.Text}_Riasunto.txt", combined);

                rawData.SelectAllCells();
                rawData.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, rawData);
                rawData.UnselectAllCells();
                string result = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue); //TODO: formatear rawData decimales

                result = result.Replace('"', ' ');

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var dataExcel = new ExcelPackage();

                var dataSheet = dataExcel.Workbook.Worksheets.Add("Dati");

                var format = new ExcelTextFormat
                {
                    Delimiter = ','
                };

                var ts = TableStyles.None;

                dataSheet.Cells["A1"].LoadFromText(result, format, ts, true);

                dataExcel.SaveAs(new FileInfo(path + $"/{hvSample.Text}_Dati.xlsx"));
            }

            Clipboard.Clear();
        }
    }
}