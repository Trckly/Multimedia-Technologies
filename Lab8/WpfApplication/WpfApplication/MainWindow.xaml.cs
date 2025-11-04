using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace WpfApplication
{
    public partial class MainWindow : Window
    {
        private bool _isPaused = false;
        private readonly List<Viewport3D> _allViewports = new List<Viewport3D>();
        private readonly Random _random = new Random();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                BuildAllSpheres();
                CollectAllViewports();
            };
        }

        private void CollectAllViewports()
        {
            // Find all Viewport3D elements and store them
            _allViewports.Clear();
            CollectViewportsRecursive(this);
        }

        private void CollectViewportsRecursive(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Viewport3D viewport)
                {
                    _allViewports.Add(viewport);
                }
                CollectViewportsRecursive(child);
            }
        }

        private void BuildAllSpheres()
        {
            // Define the visual radius for each ball (distinct from orbit radius in animations)
            // Adjusted to be smaller than orbit for better visuals, but proportional to XAML comments
            var ballRadii = new Dictionary<string, double>
            {
                { "Ball1Mesh", 150 },  // Red, orbit 300 → ball 150
                { "Ball2Mesh", 50 },   // Blue, orbit 100 → ball 50
                { "Ball3Mesh", 125 },  // Green, orbit 250 → ball 125
                { "Ball4Mesh", 160 },  // Yellow, orbit 320 → ball 160
                { "Ball5Mesh", 140 },  // Magenta, orbit 280 → ball 140
                { "Ball6Mesh", 60 }    // Cyan, orbit 120 → ball 60
            };

            // Build spheres for each named mesh
            foreach (var kvp in ballRadii)
            {
                var mesh = this.FindName(kvp.Key) as MeshGeometry3D;
                if (mesh != null)
                {
                    var sphere = CreateSphereMesh(kvp.Value, 48, 48);
                    mesh.Positions = sphere.Positions;
                    mesh.TriangleIndices = sphere.TriangleIndices;
                    mesh.TextureCoordinates = sphere.TextureCoordinates;
                }
            }
        }

        #region Mouse handling
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only drag if clicking on the Canvas background (not on a viewport)
            if (e.OriginalSource is Canvas)
            {
                DragMove();
            }
        }

        private void Viewport_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Viewport3D viewport)
            {
                viewport.Cursor = Cursors.Hand;
            }
        }

        private void Viewport_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Viewport3D viewport)
            {
                viewport.Cursor = Cursors.Arrow;
            }
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show("Click detected! Sender: " + sender.GetType().Name + "\nPaused: " + _isPaused);

            // Toggle pause state
            _isPaused = !_isPaused;

            // Apply to ALL balls
            foreach (var viewport in _allViewports)
            {
                if (_isPaused)
                    PauseBall(viewport);
                else
                    ResumeBall(viewport);

                ChangeBallAppearance(viewport);
            }
        }

        private void PauseBall(Viewport3D viewport)
        {
            // Pause all storyboards in the viewport
            foreach (var trigger in viewport.Triggers.OfType<EventTrigger>())
            {
                foreach (var action in trigger.Actions.OfType<BeginStoryboard>())
                {
                    action.Storyboard?.Pause(viewport);
                }
            }
        }

        private void ResumeBall(Viewport3D viewport)
        {
            // Resume all storyboards in the viewport
            foreach (var trigger in viewport.Triggers.OfType<EventTrigger>())
            {
                foreach (var action in trigger.Actions.OfType<BeginStoryboard>())
                {
                    action.Storyboard?.Resume(viewport);
                }
            }
        }

        private void ChangeBallAppearance(Viewport3D viewport)
        {
            // Find the GeometryModel3D and change its color
            var model = FindVisualChild<GeometryModel3D>(viewport);
            if (model?.Material is DiffuseMaterial material && material.Brush is SolidColorBrush brush)
            {
                // Generate random color
                var newColor = Color.FromRgb(
                    (byte)_random.Next(256),
                    (byte)_random.Next(256),
                    (byte)_random.Next(256)
                );

                // Animate color change
                var colorAnimation = new ColorAnimation
                {
                    To = newColor,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            }

            // Animate size change (scale)
            var scaleTransform = FindOrCreateScaleTransform(model);
            if (scaleTransform != null)
            {
                double newScale = 0.7 + _random.NextDouble() * 0.6; // Random scale between 0.7 and 1.3

                var scaleAnimation = new DoubleAnimation
                {
                    To = newScale,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform3D.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform3D.ScaleYProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform3D.ScaleZProperty, scaleAnimation);
            }
        }

        private ScaleTransform3D? FindOrCreateScaleTransform(GeometryModel3D? model)
        {
            if (model == null) return null;

            if (model.Transform is Transform3DGroup transformGroup)
            {
                var scaleTransform = transformGroup.Children.OfType<ScaleTransform3D>().FirstOrDefault();
                if (scaleTransform != null) return scaleTransform;

                scaleTransform = new ScaleTransform3D(1, 1, 1);
                transformGroup.Children.Add(scaleTransform);
                return scaleTransform;
            }
            else
            {
                var currentTransform = model.Transform ?? Transform3D.Identity;
                var newGroup = new Transform3DGroup();
                newGroup.Children.Add(currentTransform);
                var scaleTransform = new ScaleTransform3D(1, 1, 1);
                newGroup.Children.Add(scaleTransform);
                model.Transform = newGroup;
                return scaleTransform;
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        #endregion

        #region Sphere generator (48×48 → smooth)
        private MeshGeometry3D CreateSphereMesh(double radius, int thetaDiv, int phiDiv)
        {
            var pos = new Point3DCollection();
            var idx = new Int32Collection();
            var uv  = new PointCollection();

            for (int phi = 0; phi <= phiDiv; phi++)
            {
                double phiAngle = Math.PI * phi / phiDiv;
                double y = radius * Math.Cos(phiAngle);
                double rSlice = radius * Math.Sin(phiAngle);

                for (int theta = 0; theta <= thetaDiv; theta++)
                {
                    double thetaAngle = 2 * Math.PI * theta / thetaDiv;
                    double x = rSlice * Math.Cos(thetaAngle);
                    double z = rSlice * Math.Sin(thetaAngle);

                    pos.Add(new Point3D(x, y, z));
                    uv.Add(new Point((double)theta / thetaDiv, (double)phi / phiDiv));
                }
            }

            for (int phi = 0; phi < phiDiv; phi++)
            {
                int base0 = phi * (thetaDiv + 1);
                int base1 = (phi + 1) * (thetaDiv + 1);

                for (int theta = 0; theta < thetaDiv; theta++)
                {
                    idx.Add(base0 + theta);
                    idx.Add(base1 + theta);
                    idx.Add(base1 + theta + 1);

                    idx.Add(base0 + theta);
                    idx.Add(base1 + theta + 1);
                    idx.Add(base0 + theta + 1);
                }
            }

            return new MeshGeometry3D
            {
                Positions = pos,
                TriangleIndices = idx,
                TextureCoordinates = uv
            };
        }
        #endregion
    }

    // ----------  Attached properties (only for XAML documentation) ----------
    public static class Ball3D
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.RegisterAttached("Radius", typeof(double), typeof(Ball3D), new UIPropertyMetadata(0.0));

        public static double GetRadius(DependencyObject obj) => (double)obj.GetValue(RadiusProperty);
        public static void SetRadius(DependencyObject obj, double value) => obj.SetValue(RadiusProperty, value);

        public static readonly DependencyProperty CenterXProperty =
            DependencyProperty.RegisterAttached("CenterX", typeof(double), typeof(Ball3D), new UIPropertyMetadata(0.0));
        public static readonly DependencyProperty CenterYProperty =
            DependencyProperty.RegisterAttached("CenterY", typeof(double), typeof(Ball3D), new UIPropertyMetadata(0.0));

        public static double GetCenterX(DependencyObject obj) => (double)obj.GetValue(CenterXProperty);
        public static void SetCenterX(DependencyObject obj, double value) => obj.SetValue(CenterXProperty, value);
        public static double GetCenterY(DependencyObject obj) => (double)obj.GetValue(CenterYProperty);
        public static void SetCenterY(DependencyObject obj, double value) => obj.SetValue(CenterYProperty, value);
    }
}