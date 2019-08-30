using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Arkanoid
{
    abstract class GameElement
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public Brush Color { get; set; }
        public Thickness ActualPosition { get; set; }
        public Shape Surface;
    }
    class Ball : GameElement
    {
        public int Up { get; set; }
        public int Down { get; set; }
    }
    class Platform : GameElement
    {
        public int Ticks { get; set; }
        public int Stopper { get; set; }
    }
    public partial class Game : Window
    {
        string ballTexturePath = "pack://application:,,,/Arkanoid;component/bin/Debug/ball.jpg";
        string backgroundPath = "pack://application:,,,/Arkanoid;component/bin/Debug/bg.png";
        string boxPath = "pack://application:,,,/Arkanoid;component/bin/Debug/box.jpg";
        string platformPath = "pack://application:,,,/Arkanoid;component/bin/Debug/platform.jpg";

        Ball ball = new Ball();
        Platform platform = new Platform();
        DispatcherTimer Phisics, whileKeyPress;
        Arkanoid.MainWindow mainWindow;

        TextBlock pointsBlock;

        int points = 0;

        bool speedChangeAllowed = false;
        bool gameStarted = false;
        bool keyPressed = false;
        bool pausePressed = false;

        KeyEventArgs key;
        Key pauseKey = Key.Escape;
        Key speedUpKey = Key.D1;
        Key slowDownKey = Key.D2;
        Key slowYKey = Key.S;
        Key speedYKey = Key.W;
        Key slowXKey = Key.A;
        Key speedXKey = Key.D;

        struct Prev
        {
            public double BallX;
            public double BallY;
            public double BrickX;
            public double BrickY;
        }

        Prev prev;

        const int bricksAmount = 1;
        const int maxBallSpeed = 50;
        const int stratingBallSpeedX = 3;
        const int startingBallSpeedY = 3;
        const int ballSpeedFreq = 1;
        const int platformSensitivity = 47;//47
        const int bHeight = 25;//25
        const int bWidth = 25;//25
        const int pHeight = 20;//20
        const int pWidth = 6;//6

        int x = stratingBallSpeedX, y = startingBallSpeedY; //szybokść piłeczki w osi x i y

        int speedCounter = 0;

        List<Rectangle> brick = new List<Rectangle>(bricksAmount);
        List<Random> rand = new List<Random>(bricksAmount);
        List<Brush> brickColor = new List<Brush>(bricksAmount);

/*-----------------------------------------------------------------------------------------------------------------------------------------------------*/
        /*MECHANIZM KOLIZJI*/

        bool BrickXCollision(int i)
        {
            return (prev.BallY - ball.Surface.Height < prev.BrickY + brick[i].Height &&
            prev.BallY + ball.Surface.Height > prev.BrickY - brick[i].Height &&
            Math.Abs(Math.Abs(prev.BallY) - Math.Abs(prev.BrickY)) <
            Math.Abs(Math.Abs(prev.BallX) - Math.Abs(prev.BrickX)));
        }
        bool BrickYCollision(int i)
        {
            return (prev.BallX - ball.Surface.Width < prev.BrickX + brick[i].Width &&
            prev.BallX + ball.Surface.Width > prev.BrickX - brick[i].Width &&
            Math.Abs(Math.Abs(prev.BallX) - Math.Abs(prev.BrickX)) <
            Math.Abs(Math.Abs(prev.BallY) - Math.Abs(prev.BrickY)));
        }
        bool BrickCollision(int i)
        {
            return (ball.Surface.Margin.Right - (ball.Width) <= brick[i].Margin.Right + (brick[i].Width) &&
            ball.Surface.Margin.Right + (ball.Width) >= brick[i].Margin.Right - (brick[i].Width) &&
            ball.Surface.Margin.Bottom + (ball.Height) >= brick[i].Margin.Bottom - (brick[i].Height) &&
            ball.Surface.Margin.Bottom - (ball.Height) <= brick[i].Margin.Bottom + (brick[i].Height));
        }
        bool PlatformCollision()
        {
            return (ball.Surface.Margin.Right / 2 >= platform.Surface.Margin.Right - ((platform.Surface.ActualWidth / 2) + ball.Surface.Width) &&
            ball.Surface.Margin.Right / 2 <= platform.Surface.Margin.Right + (platform.Surface.ActualWidth / 2) + ball.Surface.Width);
        }
        bool BallLeftWall()
        {
            return -(ball.Surface.Margin.Right - ball.Width) > grid.ActualWidth;
        }
        bool BallRightWall()
        {
            return ball.Surface.Margin.Right + ball.Width > grid.ActualWidth;
        }
        bool BallCelling()
        {
            return ball.Surface.Margin.Bottom + ball.Height + 20 > grid.ActualWidth;
        }
        bool BallFloor()
        {
            return (-ball.Surface.Margin.Bottom + ball.Height + 16) > platform.Surface.Margin.Top;
        }
/*-----------------------------------------------------------------------------------------------------------------------------------------------------*/
        //WZROST PRĘDKOŚCI PIŁECZKI
        void SpeedChanger()
        {
            if (speedCounter == ballSpeedFreq && speedChangeAllowed)
            {
                if (x < 0 && x > -maxBallSpeed) x--;
                else if (x > 0 && x < maxBallSpeed) x++;
                if (y < 0 && y > -maxBallSpeed) y--;
                else if (y > 0 && y < maxBallSpeed) y++;
                speedCounter = 0;
            }
        }

        //TOR LOTU PIŁECZKI
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                grid.Children.Remove(ball.Surface);
                ball.Surface.Height = grid.ActualHeight / ball.Height;
                ball.Surface.Width = grid.ActualWidth / ball.Width;
                ball.Surface.Fill = ball.Color;
                prev.BallX = ball.Surface.Margin.Right;
                prev.BallY = ball.Surface.Margin.Bottom;

                if (BallLeftWall()||BallRightWall()) x = -x;
                if (BallCelling()) y = -y;

                if (BallFloor())
                {
                    if (PlatformCollision()) y = -y;
                    else
                    {
                        Phisics.Stop();
                        ball.Surface.Visibility = Visibility.Hidden;
                        grid.Children.Remove(ball.Surface);
                        CreateBackButton();
                        CreateGameOverTextBlock();
                    }
                }
                for (int i = 0; i < bricksAmount; i++)
                {
                    if (BrickCollision(i))
                    {
                        CreateNewBrick(i);       
                        SpeedChanger();

                        if (BrickXCollision(i)) x = -x;
                        if (BrickYCollision(i)) y = -y;

                        points++;
                        pointsBlock.Text = "Punkty: " + points.ToString();
                    }
                }
                ball.Left += x;
                ball.Up += y;
                ball.Surface.Margin = new Thickness(ball.Right, ball.Down, ball.Left, ball.Up);
                grid.Children.Add(ball.Surface);
            }
            catch(ResourceReferenceKeyNotFoundException ex)
            {
                MessageBox.Show(ex.Message,"Błąd!",MessageBoxButton.OK,MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }


/*-----------------------------------------------------------------------------------------------------------------------------------------------------*/

        /*RENDEROWANIE MAPY*/

        void CreateObjects(bool startingPosition)
        {
            GC.Collect();
            grid.Children.Clear();
            CreatePlatform(startingPosition);
            CreateBall();
            CreateBricks();
            CreatePointsBlock();
            CreateKeyPressTimer();

        }
        void CreateNewBrick(int i)
        {
            if(speedChangeAllowed) speedCounter++;
            prev.BrickX = brick[i].Margin.Right;
            prev.BrickY = brick[i].Margin.Bottom;
            grid.Children.Remove(brick[i]);
            brick[i] = new Rectangle();
            brick[i].Height = 50;
            brick[i].Width = 50;
            brick[i].Fill = brickColor[i];
            brick[i].VerticalAlignment = VerticalAlignment.Center;
            brick[i].HorizontalAlignment = HorizontalAlignment.Center;
            brick[i].Margin = RandPos(i);
            grid.Children.Add(brick[i]);
        }
        void CreateBackButton()
        {
            Button back = new Button();
            back.Style = this.FindResource("StartMenuButtons") as Style;
            back.Background = new ImageBrush(new BitmapImage(new Uri(@backgroundPath, UriKind.RelativeOrAbsolute)));
            back.Content = "Kliknij aby powrócić do menu";
            back.Click += Back_Click;
            back.FontSize = 30;
            back.Margin = new Thickness(0, 0, 0, -250);
            grid.Children.Add(back);
        }
        void CreateGameOverTextBlock()
        {
            TextBlock gameOver = new TextBlock();
            gameOver.MouseDown += GameOver_MouseDown;
            gameOver.FontSize = 48;
            gameOver.HorizontalAlignment = HorizontalAlignment.Center;
            gameOver.VerticalAlignment = VerticalAlignment.Center;
            gameOver.Text = "Koniec Gry! \nIlość punktów: " + points.ToString();
            grid.Children.Add(gameOver);
        }
        void CreatePlatform(bool startingPosition)
        {
            platform.Surface = new Rectangle();
            platform.Surface.VerticalAlignment = VerticalAlignment.Center;
            if (!startingPosition) platform.Surface.Margin = platform.ActualPosition;
            else platform.Surface.HorizontalAlignment = HorizontalAlignment.Center;
            platform.Surface.Margin = new Thickness(0, grid.ActualHeight - platform.Height, 0, 0);
            platform.Surface.Height = grid.ActualHeight / platform.Height;
            platform.Surface.Width = grid.ActualWidth / platform.Width;
            platform.Surface.Fill = platform.Color;
            grid.Children.Add(platform.Surface);
        }
        void CreateBall()
        {
            ball.Surface = new Ellipse();
            ball.Surface.VerticalAlignment = VerticalAlignment.Center;
            ball.Surface.HorizontalAlignment = HorizontalAlignment.Center;
            ball.Surface.Height = grid.ActualHeight / ball.Height;
            ball.Surface.Width = grid.ActualWidth / ball.Width;
            ball.Surface.Fill = ball.Color;
            grid.Children.Add(ball.Surface);
        }
        void CreateBricks()
        {
            for (int i = 0; i < bricksAmount; i++)
            {
                brickColor.Add(new ImageBrush(new BitmapImage(new Uri(@boxPath, UriKind.RelativeOrAbsolute))));
                brick.Add(new Rectangle());
                brick[i].Height = 50;
                brick[i].Width = 50;
                brick[i].Fill = brickColor[i];
                brick[i].VerticalAlignment = VerticalAlignment.Center;
                brick[i].HorizontalAlignment = HorizontalAlignment.Center;
                rand.Add(new Random());
                System.Threading.Thread.Sleep(50);
                brick[i].Margin = RandPos(i);
                grid.Children.Add(brick[i]);
            }
        }
        void CreatePointsBlock()
        {
            pointsBlock = new TextBlock();
            pointsBlock.FontSize = 25;
            pointsBlock.HorizontalAlignment = HorizontalAlignment.Right;
            pointsBlock.VerticalAlignment = VerticalAlignment.Top;
            grid.Children.Add(pointsBlock);
        }
        void CreateKeyPressTimer()
        {
            whileKeyPress = new DispatcherTimer();
            whileKeyPress.Tick += WhileKeyPress_Tick;
            whileKeyPress.Interval = new TimeSpan(0, 0, 0, 0, 10);
        }
/*-----------------------------------------------------------------------------------------------------------------------------------------------------*/

        public void SetParam()
        {
            ball.Up = 0;
            ball.Down = 0;
            ball.Left = 0;
            ball.Right = 0;
            platform.Left = 0;
            platform.Right = 0;
            ball.Color = new ImageBrush(new BitmapImage(new Uri(@ballTexturePath, UriKind.RelativeOrAbsolute)));
            platform.Color = new ImageBrush(new BitmapImage(new Uri(@platformPath, UriKind.RelativeOrAbsolute)));
            platform.Ticks = platformSensitivity;
            platform.Stopper = 0;
            platform.Height = pHeight;
            platform.Width = pWidth;
            ball.Height = bHeight;
            ball.Width = bWidth;
        }

        private void arkanoidWindow_KeyDown(object sender, KeyEventArgs e)
        {
            keyPressed = true;
            if(e.Key.Equals(pauseKey)&&!pausePressed)
            {
                pausePressed = true;
                Phisics.Stop();
            }
            else if (e.Key.Equals(pauseKey) && pausePressed)
            {
                pausePressed = false;
                Phisics.Start();
            }
            else if(keyPressed&&gameStarted&&(e.Key.Equals(Key.Left)|| e.Key.Equals(Key.Right)))
            {
                key = e;
                whileKeyPress.Start();
            }
            else if(e.Key.Equals(slowXKey))
            {
                if(x>0)x -=Math.Abs(x);
                else x += Math.Abs(x);
            }
            else if (e.Key.Equals(slowYKey))
            {
                if (y > 0) y -= 1;
                else y += 1;
            }
            else if (e.Key.Equals(speedXKey))
            {
                if (y > 0) x += 1;
                else x -= 1;
            }
            else if (e.Key.Equals(speedYKey))
            {
                if (y > 0) y += 1;
                else y -= 1;
            }
            else if(e.Key.Equals(slowDownKey))
            {
                if (x > 0) x -= 1;
                else x += 1;
                if (y > 0) y -= 1;
                else y += 1;
            }
            else if (e.Key.Equals(speedUpKey))
            {
                if (x > 0) x += 1;
                else x -= 1;
                if (y > 0) y += 1;
                else y -= 1;
            }

        }

        //STEROWANIE PLATFORMĄ
        private void WhileKeyPress_Tick(object sender, EventArgs e)
        {
            if(keyPressed&&!pausePressed)
            {
                if (key.Key.Equals(Key.Left) && platform.Stopper > -((platform.Ticks / 2) - 1))
                {
                    platform.Left += (int)grid.Width / platform.Ticks;
                    platform.Right -= (int)grid.Width / platform.Ticks;
                    platform.Surface.Margin = new Thickness(platform.Right, grid.ActualHeight - platform.Height, platform.Left, 0);
                    platform.Stopper--;
                }
                else if (key.Key.Equals(Key.Right) && platform.Stopper < (platform.Ticks / 2) - 1)
                {
                    platform.Right += (int)grid.Width / platform.Ticks;
                    platform.Left -= (int)grid.Width / platform.Ticks;
                    platform.Surface.Margin = new Thickness(platform.Right, grid.ActualHeight - platform.Height, platform.Left, 0);
                    platform.Stopper++;
                }
            }
        }
        Thickness RandPos(int i)
        {
            return new Thickness(0, 0, rand[i].Next(-520, 520), rand[i].Next(-350, 500));
        }
        private void arkanoidWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if(gameStarted)
            {
                if((e.Key.Equals(Key.Left) || e.Key.Equals(Key.Right)))
                {
                    if (e.Key.Equals(key.Key))
                    {
                        keyPressed = false;
                        whileKeyPress.Stop();
                    }
                }
            }
        }
        public void StartMoving(bool stop)
        {
            Phisics = new DispatcherTimer();
            Phisics.Tick += dispatcherTimer_Tick;
            Phisics.Interval = new TimeSpan(0, 0, 0, 0, 10);
            Phisics.Start();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetParam();
        }

        private void arkanoidWindow_Closed(object sender, EventArgs e)
        {
            mainWindow.newGame.IsEnabled = true;
        }
        private void startGame_Click(object sender, RoutedEventArgs e)
        {
            gameStarted = true;
            startGame.Visibility = Visibility.Hidden;
            grid.Children.Clear();
            StartMoving(false);
            CreateObjects(true);
        }
        private void GameOver_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public Game(ref Arkanoid.MainWindow main)
        {
            mainWindow = main;
            InitializeComponent();
        }
        public Game()
        {
            InitializeComponent();
        }
    }
}
