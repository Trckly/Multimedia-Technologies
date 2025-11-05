using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Animated3DBalls
{
    public partial class MainWindow : Window
    {
        private readonly Random _rnd = new Random();
        private readonly List<Ball> _balls = new();
        private bool _isPaused = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => CreateBalls();

        private void CreateBalls()
        {
            var cfg = new[]
            {
                new BallConfig { Color = Colors.Red,     Radius = 150, OrbitRadius = 300, Speed = 8.0,  Center = new Point(960, 240) },
                new BallConfig { Color = Colors.Blue,    Radius = 50,  OrbitRadius = 100, Speed = 3.0,  Center = new Point(560, 440) },
                new BallConfig { Color = Colors.Green,   Radius = 125, OrbitRadius = 250, Speed = 5.0,  Center = new Point(910, 290) },
                new BallConfig { Color = Colors.Yellow,  Radius = 160, OrbitRadius = 320, Speed = 4.0,  Center = new Point(980, 220) },
                new BallConfig { Color = Colors.Magenta, Radius = 140, OrbitRadius = 280, Speed = 3.5,  Center = new Point(940, 260) },
                new BallConfig { Color = Colors.Cyan,    Radius = 60,  OrbitRadius = 120, Speed = 6.0,  Center = new Point(542, 422) }
            };

            foreach (var c in cfg)
                _balls.Add(new Ball(c, RootCanvas, _rnd));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // drag window when clicking background
            if (e.OriginalSource is Canvas) { DragMove(); return; }

            _isPaused = !_isPaused;
            foreach (var b in _balls)
            {
                if (_isPaused) b.Pause(); else b.Resume();
                b.RandomizeAppearance();
            }
        }
    }

    // --------------------------------------------------------------
    // Config
    // --------------------------------------------------------------
    internal class BallConfig
    {
        public Color Color { get; set; }
        public double Radius { get; set; }
        public double OrbitRadius { get; set; }
        public double Speed { get; set; }
        public Point Center { get; set; }
    }

    // --------------------------------------------------------------
    // Ball
    // --------------------------------------------------------------
    internal class Ball
    {
        private readonly Viewport3D _vp;
        private readonly TranslateTransform3D _translate;
        private readonly ScaleTransform3D _scale;
        private readonly DiffuseMaterial _material;
        private readonly Storyboard _orbit;
        private readonly GeometryModel3D _model;
        private readonly Random _rnd;

        public Ball(BallConfig cfg, Canvas canvas, Random rnd)
        {
            _rnd = rnd;

            // ---------- Viewport ----------
            _vp = new Viewport3D
            {
                Width = cfg.Radius * 2 + 100,
                Height = cfg.Radius * 2 + 100,
                Cursor = Cursors.Hand
            };
            _vp.ClipToBounds = false;
            Canvas.SetLeft(_vp, cfg.Center.X - _vp.Width / 2);
            Canvas.SetTop(_vp, cfg.Center.Y - _vp.Height / 2);
            canvas.Children.Add(_vp);

            // ---------- Camera ----------
            _vp.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, cfg.Radius * 4),
                LookDirection = new Vector3D(0, 0, -1),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 45
            };

            // ---------- Lights ----------
            var lights = new Model3DGroup();
            lights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)));
            lights.Children.Add(new DirectionalLight(Colors.White, new Vector3D(1, 1, -1)));

            // ---------- Sphere ----------
            var geometry = CreateSphereMesh(cfg.Radius);
            _material = new DiffuseMaterial(new SolidColorBrush(cfg.Color));
            _model = new GeometryModel3D(geometry, _material);

            // ---------- Transforms ----------
            _scale = new ScaleTransform3D(1, 1, 1);
            _translate = new TranslateTransform3D();

            var tg = new Transform3DGroup();
            tg.Children.Add(_scale);
            tg.Children.Add(_translate);
            _model.Transform = tg;

            // ---------- Visual ----------
            var content = new Model3DGroup();
            content.Children.Add(lights);
            content.Children.Add(_model);
            _vp.Children.Add(new ModelVisual3D { Content = content });

            // ---------- Orbit ----------
            _orbit = CreateOrbitStoryboard(cfg.OrbitRadius, cfg.Speed);
            _orbit.RepeatBehavior = RepeatBehavior.Forever;

            // Start animation after visual tree is ready
            _vp.Loaded += (s, e) => _vp.Dispatcher.BeginInvoke(
                new Action(() => _orbit.Begin(_vp, true)),
                DispatcherPriority.Loaded);
        }

        // --------------------------------------------------------------
        // Circular orbit animation
        // --------------------------------------------------------------
        private Storyboard CreateOrbitStoryboard(double radius, double period)
        {
            var sb = new Storyboard();
            const int steps = 16;

            var xAnim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(period) };
            var yAnim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(period) };

            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double angle = t * 2 * Math.PI;
                var time = TimeSpan.FromSeconds(t * period);

                xAnim.KeyFrames.Add(new LinearDoubleKeyFrame(radius * Math.Cos(angle), time));
                yAnim.KeyFrames.Add(new LinearDoubleKeyFrame(radius * Math.Sin(angle), time));
            }

            // Animate TranslateTransform3D inside the 3D model hierarchy
            Storyboard.SetTarget(xAnim, _vp);
            Storyboard.SetTarget(yAnim, _vp);

            var pathX = new PropertyPath("(Viewport3D.Children)[0].(ModelVisual3D.Content).(Model3DGroup.Children)[1].(GeometryModel3D.Transform).(Transform3DGroup.Children)[1].(TranslateTransform3D.OffsetX)");
            var pathY = new PropertyPath("(Viewport3D.Children)[0].(ModelVisual3D.Content).(Model3DGroup.Children)[1].(GeometryModel3D.Transform).(Transform3DGroup.Children)[1].(TranslateTransform3D.OffsetY)");

            Storyboard.SetTargetProperty(xAnim, pathX);
            Storyboard.SetTargetProperty(yAnim, pathY);

            sb.Children.Add(xAnim);
            sb.Children.Add(yAnim);
            return sb;
        }

        // --------------------------------------------------------------
        // Smooth sphere mesh
        // --------------------------------------------------------------
        private static MeshGeometry3D CreateSphereMesh(double radius)
        {
            var pos = new Point3DCollection();
            var idx = new Int32Collection();
            var uv = new PointCollection();

            const int thetaDiv = 48, phiDiv = 48;

            for (int phi = 0; phi <= phiDiv; phi++)
            {
                double phiAngle = Math.PI * phi / phiDiv;
                double y = radius * Math.Cos(phiAngle);
                double r = radius * Math.Sin(phiAngle);

                for (int theta = 0; theta <= thetaDiv; theta++)
                {
                    double thetaAngle = 2 * Math.PI * theta / thetaDiv;
                    double x = r * Math.Cos(thetaAngle);
                    double z = r * Math.Sin(thetaAngle);

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

        // --------------------------------------------------------------
        // Pause / Resume
        // --------------------------------------------------------------
        public void Pause() => _orbit.Pause(_vp);
        public void Resume() => _orbit.Resume(_vp);

        // --------------------------------------------------------------
        // Random colour + scale animation
        // --------------------------------------------------------------
        public void RandomizeAppearance()
        {
            var col = Color.FromRgb(
                (byte)_rnd.Next(256),
                (byte)_rnd.Next(256),
                (byte)_rnd.Next(256));

            var ca = new ColorAnimation
            {
                To = col,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ((SolidColorBrush)_material.Brush).BeginAnimation(SolidColorBrush.ColorProperty, ca);

            double target = 0.7 + _rnd.NextDouble() * 0.6;
            var sa = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            _scale.BeginAnimation(ScaleTransform3D.ScaleXProperty, sa);
            _scale.BeginAnimation(ScaleTransform3D.ScaleYProperty, sa);
            _scale.BeginAnimation(ScaleTransform3D.ScaleZProperty, sa);
        }
    }
}
