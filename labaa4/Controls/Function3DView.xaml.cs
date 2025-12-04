using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using labaa4.Models;

namespace labaa4.Controls
{
    public partial class Function3DView : UserControl
    {
        public static readonly DependencyProperty FunctionDataProperty =
            DependencyProperty.Register(
                nameof(FunctionData),
                typeof(FunctionPoint[,]),
                typeof(Function3DView),
                new PropertyMetadata(null, OnFunctionDataChanged));

        private readonly GeometryModel3D _surfaceGeometry = new GeometryModel3D();
        private readonly Transform3DGroup _transformGroup = new Transform3DGroup();
        private readonly AxisAngleRotation3D _rotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -35);
        private readonly AxisAngleRotation3D _rotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 35);
        private bool _isRotating;
        private Point _lastMousePosition;

        public Function3DView()
        {
            InitializeComponent();
            _transformGroup.Children.Add(new RotateTransform3D(_rotationY));
            _transformGroup.Children.Add(new RotateTransform3D(_rotationX));
            _surfaceGeometry.Transform = _transformGroup;
            SurfaceModel.Content = _surfaceGeometry;
        }

        public FunctionPoint[,] FunctionData
        {
            get => (FunctionPoint[,])GetValue(FunctionDataProperty);
            set => SetValue(FunctionDataProperty, value);
        }

        private static void OnFunctionDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Function3DView view)
            {
                view.UpdateSurface();
            }
        }

        private void UpdateSurface()
        {
            var data = FunctionData;
            if (data == null || data.Length == 0)
            {
                _surfaceGeometry.Geometry = null;
                return;
            }

            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            if (rows < 2 || cols < 2)
            {
                _surfaceGeometry.Geometry = null;
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;

            foreach (var point in data)
            {
                if (point == null) continue;
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxZ = Math.Max(maxZ, point.Z);
            }

            if (double.IsInfinity(minX) || double.IsInfinity(minY) || double.IsInfinity(minZ))
            {
                _surfaceGeometry.Geometry = null;
                return;
            }

            double rangeX = Math.Max(maxX - minX, 0.0001);
            double rangeY = Math.Max(maxY - minY, 0.0001);
            double rangeZ = Math.Max(maxZ - minZ, 0.0001);

            double centerX = (minX + maxX) * 0.5;
            double centerY = (minY + maxY) * 0.5;
            double centerZ = (minZ + maxZ) * 0.5;

            double xScale = 3.5 / rangeX;
            double yScale = 3.5 / rangeY;
            double zScale = 2.4 / rangeZ;

            var positions = new Point3DCollection(rows * cols);
            var textureCoords = new PointCollection(rows * cols);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var point = data[r, c] ?? new FunctionPoint(0, 0, 0);
                    double x = (point.X - centerX) * xScale;
                    double y = (point.Z - centerZ) * zScale;
                    double z = (point.Y - centerY) * yScale;

                    positions.Add(new Point3D(x, y, z));

                    double normalizedX = (point.X - minX) / rangeX;
                    double normalizedZ = (point.Z - minZ) / rangeZ;
                    textureCoords.Add(new Point(normalizedX, 1 - normalizedZ));
                }
            }

            var indices = new Int32Collection((rows - 1) * (cols - 1) * 6);
            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int topLeft = r * cols + c;
                    int bottomLeft = (r + 1) * cols + c;
                    int topRight = r * cols + c + 1;
                    int bottomRight = (r + 1) * cols + c + 1;

                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            var mesh = new MeshGeometry3D
            {
                Positions = positions,
                TriangleIndices = indices,
                TextureCoordinates = textureCoords
            };

            _surfaceGeometry.Geometry = mesh;
            var material = CreateGradientMaterial();
            _surfaceGeometry.Material = material;
            _surfaceGeometry.BackMaterial = material;
        }

        private static Material CreateGradientMaterial()
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(0, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#FF14141C"), 0.0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#FFEF6C00"), 0.55),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFC107"), 1.0)
                }
            };

            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(gradient));
            materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 20));
            return materialGroup;
        }

        private void Viewport3D_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRotating = true;
            _lastMousePosition = e.GetPosition(SurfaceViewport);
            SurfaceViewport.CaptureMouse();
            e.Handled = true;
        }

        private void Viewport3D_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRotating = false;
            SurfaceViewport.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating || e.RightButton != MouseButtonState.Pressed)
            {
                return;
            }

            var current = e.GetPosition(SurfaceViewport);
            var delta = current - _lastMousePosition;
            _rotationY.Angle += delta.X * 0.4;
            _rotationX.Angle += delta.Y * 0.4;
            _lastMousePosition = current;
        }
    }
}

