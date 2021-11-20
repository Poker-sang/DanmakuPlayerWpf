using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using static System.Convert;

namespace BiliBulletScreenPlayer.Model
{

    public class BulletScreen
    {
        public readonly int Time;
        public readonly int Mode;
        //public readonly int Size;
        private readonly string _text;
        private readonly int _color;
        private readonly TextBlock _textBlock = new();
        private const int Height = 32; //实际数据约31.75

        public static MainWindow Window;

        public BulletScreen(XmlNode xmlNode)
        {
            var tempInfo = ((XmlElement)xmlNode).GetAttribute("p").Split(",");
            Time = (int)ToDouble(tempInfo[0]);
            Mode = ToInt32(tempInfo[1]);
            //size = Convert.ToInt32(tempInfo[2]);
            _color = ToInt32(tempInfo[3]);
            _text = ((XmlElement)xmlNode).InnerText;
        }
        public void Start(Storyboard storyboard, int timeNow)
        {
            _textBlock.Style = (Style)_textBlock.FindResource("BulletScreenBlock");
            _textBlock.Opacity = App.Opacity;
            _textBlock.Text = _text;
            _textBlock.Foreground = new SolidColorBrush(Color.FromRgb((byte)((_color & 0xFF0000) >> 0x10), (byte)((_color & 0xFF00) >> 8), (byte)(_color & 0xFF)));
            _ = Window.Canvas.Children.Add(_textBlock);
            _textBlock.UpdateLayout();

            var smallestIndex = 0;
            switch (Mode)
            {
                case 4: //底部
                    for (var i = (int)(Window.ActualHeight / Height - 1); i >= 0; i--)
                        if (i >= App.StaticRoom.Count)
                        {
                            smallestIndex = i;
                            while (i >= App.StaticRoom.Count)
                                App.StaticRoom.Add(App.Speed + timeNow);
                            break;
                        }
                        else if (timeNow >= App.StaticRoom[i])
                        {
                            smallestIndex = i;
                            break;
                        }
                        else if (App.StaticRoom[smallestIndex] > App.StaticRoom[i])
                            smallestIndex = i;
                    break;
                case 5: //顶部
                    for (var i = 0; i < Window.ActualHeight / Height; i++)
                        if (i >= App.StaticRoom.Count)
                        {
                            App.StaticRoom.Add(App.Speed + timeNow);
                            smallestIndex = i;
                            break;
                        }
                        else if (timeNow >= App.StaticRoom[i])
                        {
                            smallestIndex = i;
                            break;
                        }
                        else if (App.StaticRoom[smallestIndex] > App.StaticRoom[i])
                            smallestIndex = i;
                    break;
                default: //滚动
                    var rollTimeSpan = (int)(_textBlock.ActualWidth * App.Speed / (Window.ActualWidth + _textBlock.ActualWidth) + 1);
                    for (var i = 0; i < Window.ActualHeight / Height; i++)
                        if (i >= App.RollRoom.Count)
                        {
                            App.RollRoom.Add(rollTimeSpan + timeNow);
                            smallestIndex = i;
                            break;
                        }
                        else if (timeNow >= App.RollRoom[i])
                        {
                            smallestIndex = i;
                            break;
                        }
                        else if (App.RollRoom[smallestIndex] > App.RollRoom[i])
                            smallestIndex = i;
                    App.RollRoom[smallestIndex] = rollTimeSpan + timeNow;
                    Canvas.SetTop(_textBlock, smallestIndex * Height);
                    RollBulletScreen(storyboard);
                    return;
            }
            App.StaticRoom[smallestIndex] = App.Speed + timeNow;
            Canvas.SetTop(_textBlock, smallestIndex * Height);
            Canvas.SetLeft(_textBlock, (Window.ActualWidth - _textBlock.ActualWidth) / 2);
            App.TimeCounter.Tick += StaticBulletScreen;
        }
        private int _delayTime;
        private void StaticBulletScreen(object sender, EventArgs e)
        {
            if (_delayTime < App.Speed)
                ++_delayTime;
            else
            {
                App.TimeCounter.Tick -= StaticBulletScreen;
                Window.Canvas.Children.Remove(_textBlock);
            }
        }
        private void RollBulletScreen(Storyboard storyboard)
        {
            var rollDa = new DoubleAnimation
            {
                From = Window.ActualWidth,
                To = -_textBlock.ActualWidth,
                Duration = TimeSpan.FromSeconds(App.Speed)
            };
            Storyboard.SetTarget(rollDa, _textBlock);
            storyboard.Children.Add(rollDa);
            storyboard.Completed += (_, _) => Window.Canvas.Children.Remove(_textBlock);
        }

    }
}