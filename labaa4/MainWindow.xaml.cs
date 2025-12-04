using System;
using System.Windows;
using System.Collections.ObjectModel;
using labaa4.Models;

namespace labaa4
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<FunctionSurface> Surfaces { get; } = new ObservableCollection<FunctionSurface>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            SeedSurfaces();
        }

        private void SeedSurfaces()
        {
            Surfaces.Clear();
            Surfaces.Add(new FunctionSurface(
                "Sine × Cosine",
                "A smooth wave created from sin(x) · cos(y) sampled within ±π.",
                SampleSurface((x, y) => Math.Sin(x) * Math.Cos(y), -Math.PI, Math.PI, 45, -Math.PI, Math.PI, 45)));

            Surfaces.Add(new FunctionSurface(
                "Gaussian Hill",
                "A radial Gaussian bump: exp(-(x² + y²) / 3).",
                SampleSurface((x, y) => Math.Exp(-(x * x + y * y) / 3d), -3, 3, 40, -3, 3, 40)));

            Surfaces.Add(new FunctionSurface(
                "Hyperbolic Saddle",
                "The classic saddle surface x² - y² rendered over ±2.5.",
                SampleSurface((x, y) => (x * x - y * y) / 4d, -2.5, 2.5, 40, -2.5, 2.5, 40)));

            Surfaces.Add(new FunctionSurface(
                "Ripple Bowl",
                "Circular ripples given by sin(r) / (r + 1) where r = √(x² + y²).",
                SampleSurface((x, y) =>
                {
                    double r = Math.Sqrt(x * x + y * y);
                    return Math.Sin(r) / (r + 1);
                }, -6, 6, 50, -6, 6, 50)));
        }

        private static FunctionPoint[,] SampleSurface(Func<double, double, double> function,
                                                      double xMin, double xMax, int xSteps,
                                                      double yMin, double yMax, int ySteps)
        {
            var samples = new FunctionPoint[xSteps, ySteps];
            for (int xi = 0; xi < xSteps; xi++)
            {
                double x = Lerp(xMin, xMax, xi, xSteps);
                for (int yi = 0; yi < ySteps; yi++)
                {
                    double y = Lerp(yMin, yMax, yi, ySteps);
                    double z = function(x, y);
                    samples[xi, yi] = new FunctionPoint(x, y, z);
                }
            }

            return samples;
        }

        private static double Lerp(double min, double max, int index, int total)
        {
            if (total <= 1)
            {
                return min;
            }

            double t = index / (double)(total - 1);
            return min + (max - min) * t;
        }
    }
}
