using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApplication
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer animationTimer;
        private List<Ball> balls = new List<Ball>();
        private Ball selectedBall = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas canvas = this.FindName("canvas") as Canvas;

            // 6 кульок з різними кольорами
            Color[] colors = { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Magenta, Colors.Cyan };
            double[] radii = { 50, 60, 70, 55, 65, 58 };
            double[] speeds = { 0.007, 0.02, 0.012, 0.018, 0.03, 0.025 };
            double[] orbitRadii = { 300, 100, 250, 320, 280, 120 };

            for (int i = 0; i < 6; i++)
            {
                Ball ball = new Ball(25, colors[i], radii[i], speeds[i], orbitRadii[i], i);
                balls.Add(ball);
                canvas.Children.Add(ball.Ellipse);
            }

            // Налаштування таймера анімації
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(16);
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Обробники подій мишки
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.MouseMove += Canvas_MouseMove;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            Canvas canvas = this.FindName("canvas") as Canvas;
            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;

            foreach (Ball ball in balls)
            {
                if (!ball.IsPaused)
                {
                    // Рух по колоподібній траєкторії
                    ball.Angle += ball.Speed;
                    double x = centerX + ball.OrbitRadius * Math.Cos(ball.Angle);
                    double y = centerY + ball.OrbitRadius * Math.Sin(ball.Angle);

                    Canvas.SetLeft(ball.Ellipse, x - ball.Radius);
                    Canvas.SetTop(ball.Ellipse, y - ball.Radius);
                }
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = this.FindName("canvas") as Canvas;
            Point p = e.GetPosition(canvas);

            foreach (Ball ball in balls)
            {
                double ballX = Canvas.GetLeft(ball.Ellipse) + ball.Radius;
                double ballY = Canvas.GetTop(ball.Ellipse) + ball.Radius;
                double distance = Math.Sqrt(Math.Pow(p.X - ballX, 2) + Math.Pow(p.Y - ballY, 2));

                if (distance <= ball.Radius)
                {
                    selectedBall = ball;
                    ball.IsPaused = !ball.IsPaused;

                    // Зміна розміру при клацанні
                    if (ball.IsPaused)
                    {
                        ball.Radius *= 1.3;
                    }
                    else
                    {
                        ball.Radius /= 1.3;
                    }

                    // Зміна кольору при клацанні
                    Random rand = new Random();
                    ball.SetColor(Color.FromRgb(
                        (byte)rand.Next(100, 256),
                        (byte)rand.Next(100, 256),
                        (byte)rand.Next(100, 256)
                    ));

                    ball.Ellipse.Width = ball.Radius * 2;
                    ball.Ellipse.Height = ball.Radius * 2;

                    e.Handled = true;
                    break;
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            selectedBall = null;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedBall != null)
            {
                Canvas canvas = this.FindName("canvas") as Canvas;
                Point p = e.GetPosition(canvas);
                Canvas.SetLeft(selectedBall.Ellipse, p.X - selectedBall.Radius);
                Canvas.SetTop(selectedBall.Ellipse, p.Y - selectedBall.Radius);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }

    public class Ball
    {
        public Ellipse Ellipse { get; set; }
        public double Radius { get; set; }
        public double Angle { get; set; }
        public double Speed { get; set; }
        public double OrbitRadius { get; set; }
        public bool IsPaused { get; set; }

        public Ball(double radius, Color color, double orbitRadius, double speed, double initialOrbitRadius, int index)
        {
            Radius = radius;
            Speed = speed;
            OrbitRadius = initialOrbitRadius;
            IsPaused = false;
            Angle = (Math.PI * 2 / 4) * index;

            Ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2
            };
        }

        public void SetColor(Color color)
        {
            Ellipse.Fill = new SolidColorBrush(color);
        }
    }
}