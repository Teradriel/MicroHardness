using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
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
        public string? SamplePath { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Issues_Click(object sender, RoutedEventArgs e)
        {
            KnownIssues knownIssues = new();
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
            //Setting all variables to empty so new files can be analyzed without exiting the software
            hvSample.Text = "";
            hvMeanStd.Text = "";
            hv25.Text = "";
            hv75.Text = "";
            hvMax.Text = "";
            hvMin.Text = "";
            hvMedian.Text = "";
            hvPointCount.Text = "";
            hvRSD.Text = "";
            string microPath = "";
            string sample = "";
            string sampleCode = "";
            SamplePath = "";
            double[] hvArray = Array.Empty<double>();
            double[] hvStatistics = Array.Empty<double>();
            double RSD = 0;
            double quantileBottom = 0;
            double quantileUpper = 0;
            double[] xAxis = Array.Empty<double>();
            double[] xAxisDouble = Array.Empty<double>();
            string[] xAxisString = Array.Empty<string>();
            BoxPlot.Plot.Clear();
            LinePlot.Plot.Clear();
            double mean = 0;
            string meanString = "";
            double std = 0;
            string stdString = "";
            double q25 = 0;
            string q25String = "";
            double q75 = 0;
            string q75String = "";
            double max = 0;
            string maxString = "";
            double min = 0;
            string minString = "";
            double median = 0;
            string medianString = "";
            int pointCount = 0;
            rawData.Items.Refresh();

            //This is the path for release
            //microPath = @"\\svr2012\Laboratorio Analisi\Dati Strumenti\Microdurometro\2023";

            //This is the path for debugging
            microPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            OpenFileDialog openFileDialog = new()
            {
                Filter = "CSV Files only (*.csv)|*.csv",
                InitialDirectory = microPath,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                sample = openFileDialog.FileName;
                sampleCode = openFileDialog.SafeFileName;

                SamplePath = Path.GetDirectoryName(openFileDialog.FileName);

                //Here the program removes the extension to get the name of the sample
                int removeExt = sampleCode.LastIndexOf(".");
                if (removeExt > 0) sampleCode = sampleCode.Remove(removeExt);

                //Populate the data grid
                rawData.DataContext = HVService.ReadFile(sample);
                hvArray = HVService.Results(sample);

                //Populate the array that is trimed with statistical analysis
                hvStatistics = hvArray;

                //RSD = relative standard deviation
                //Is a measure of the dispersion of a probability distribution.
                //Dividing the standard deviation by the mean of the data provides the relative magnitude of the standard deviation
                RSD = hvArray.StandardDeviation() / hvArray.Mean();

                if (RSD > 0.15)
                {
                    RSD = 0.15;
                }

                quantileBottom = hvArray.Quantile(RSD);

                quantileUpper = hvArray.Quantile(1 - RSD);

                //Applying the statistical analysis to the array
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

                //Quantity of measures after statistical analysis
                pointCount = hvStatistics.Length;

                //Setting axis of the plots
                xAxis = DataGen.Consecutive(pointCount);
                xAxisDouble = DataGen.Consecutive(pointCount);
                xAxisString = new string[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    xAxisString[i] = xAxisDouble[i].ToString();
                }

                var pop = new ScottPlot.Statistics.Population(hvStatistics);

                //Calculating data and setting as a string for better reading
                mean = Statistics.Mean(hvStatistics).Round(0);
                meanString = mean.ToString();
                std = Statistics.StandardDeviation(hvStatistics).Round(0);
                stdString = std.ToString();
                q25 = Statistics.LowerQuartile(hvStatistics).Round(0);
                q25String = q25.ToString();
                q75 = Statistics.UpperQuartile(hvStatistics).Round(0);
                q75String = q75.ToString();
                max = Statistics.Maximum(hvStatistics).Round(0);
                maxString = max.ToString();
                min = Statistics.Minimum(hvStatistics).Round(0);
                minString = min.ToString();
                median = Statistics.Median(hvStatistics).Round(0);
                medianString = median.ToString();

                //Calculating the real relative standard deviation
                RSD *= 100;

                //Setting the data to the UI
                hvSample.Text = $"{sampleCode}";
                hvMeanStd.Text = $"{meanString} ± {stdString}";
                hv25.Text = $"{q25String}";
                hv75.Text = $"{q75String}";
                hvMax.Text = $"{maxString}";
                hvMin.Text = $"{minString}";
                hvMedian.Text = $"{medianString}";
                hvPointCount.Text = $"{pointCount}";
                hvRSD.Text = $"{RSD}%";

                //Plotting the data
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

                //Plotting the data
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
            //If there is no data the program will not execute the function
            if (hvSample.Text == "") return;

            //Setting the save path
            string? savePath;
            if (SamplePath != null)
            {
                savePath = Path.Combine(SamplePath, $"{hvSample.Text}");
            }
            else
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{hvSample.Text}");
            }

            //Create a directory in the selected path
            Directory.CreateDirectory(savePath);

            //For printing plots the program has to visualize them first, showing a message fixed this
            TabControl.SetIsSelected(LineTab, true);
            MessageBox.Show("LinePlot stampato", "Avviso", MessageBoxButton.OK);
            TabControl.SetIsSelected(BoxTab, true);
            MessageBox.Show("BoxPlot stampato", "Avviso", MessageBoxButton.OK);

            //Save the plots
            LinePlot.Plot.SaveFig(savePath + $"/{hvSample.Text}_LinePlot.png");
            BoxPlot.Plot.SaveFig(savePath + $"/{hvSample.Text}_BoxPlot.png");

            //Returning to the main tab
            TabControl.SetIsSelected(Data, true);

            //Setting the data for the summary file
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

            //Making 2 columns in the text file with the previously analyzed data
            List<string> combined = new();
            int count = reference.Count >= results.Count ? reference.Count : results.Count;
            for (int i = 0; i < count; i++)
            {
                string firstColumn = reference.Count <= i ? "" : reference[i];
                string secondColumn = results.Count <= i ? "" : results[i];

                firstColumn += new string(' ', 19 - firstColumn.Length);

                combined.Add(string.Format("{0} {1}", firstColumn, secondColumn));
            }

            //Saving the text file
            File.WriteAllLines(savePath + $"/{hvSample.Text}_Riassunto.txt", combined);

            //Making and saving the Excel file with all the data
            using var book = new XLWorkbook();
            var worksheet = book.AddWorksheet("Dati");
            worksheet.Cell("A1").Value = "Prova";
            worksheet.Cell("B1").Value = "HV";
            worksheet.Cell("C1").Value = "Diag. Media";
            worksheet.Cell("A2").InsertData(rawData.ItemsSource as IEnumerable<TestHV>);
            book.SaveAs(savePath + $"/{hvSample.Text}_Dati.xlsx");
        }
    }
}