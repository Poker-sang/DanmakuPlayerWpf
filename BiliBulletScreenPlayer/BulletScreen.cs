using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using static System.Convert;

namespace BiliBulletScreenPlayer
{

	public class BulletScreen
	{
		public int Time { get; }
		public int Mode { get; }
		//public int Size { get; }
		private string Text { get; }
		private Storyboard RollSb { get; set; }
		private TextBlock TextBlock { get; } = new();

		private static MainWindow _window;

		public BulletScreen(XmlNode xmlNode,MainWindow window)
		{
			_window = window;
			var tempInfo = ((XmlElement)xmlNode).GetAttribute("p").Split(",");
			Time = (int)ToDouble(tempInfo[0]);
			Mode = ToInt32(tempInfo[1]);
			//size = Convert.ToInt32(tempInfo[2]);
			_color = ToInt32(tempInfo[3]);
			Text = ((XmlElement)xmlNode).InnerText;
		}
		public void Start(Storyboard storyboard)
		{
			switch (Mode)
			{
				case 3 when App.BottomBulletScreen2 > _window.Height / 33:
					App.BottomBulletScreen2 = 0;
					break;
				case 4 when App.TopBulletScreen2 > _window.Height / 33:
					App.TopBulletScreen2 = 0;
					break;
				case < 4 when App.RollBulletScreen2 > _window.Height / 33:
					App.RollBulletScreen2 = 0;
					break;
			}

			TextBlock.Style = (Style)TextBlock.FindResource("BulletScreenBlock");
			TextBlock.Opacity = App.Opacity;
			TextBlock.FontSize = 25;
			TextBlock.Text = Text;
			_ = _window.Canvas.Children.Add(TextBlock);
			TextBlock.UpdateLayout();
			TextBlock.Foreground = new SolidColorBrush(Color.FromRgb((byte)((_color & 0xFF0000) >> 16), (byte)((_color & 0xFF00) >> 8), (byte)(_color & 0xFF)));
			TextBlock.Visibility = Visibility.Visible;

			switch (Mode)
			{
				case 4: //底部
					Canvas.SetTop(TextBlock, _window.Height - App.BottomBulletScreen2++ * 33 % _window.Height);        //先赋值再自增
					break;
				case 5: //顶部
					Canvas.SetTop(TextBlock, App.TopBulletScreen2++ * 33 % _window.Height);
					break;
				default: //滚动
					Canvas.SetTop(TextBlock, App.RollBulletScreen2++ * 33 % _window.Height);
					RollBulletScreen(storyboard);
					return;     //不执行下面的语句
			}
			Canvas.SetLeft(TextBlock, (_window.Width - TextBlock.ActualWidth) / 2);
			App.TimeCounter.Tick += StaticBulletScreen;
		}
		private void StaticBulletScreen(object sender, EventArgs e)
		{
			if (_delayTime < App.Speed)
				++_delayTime;
			else
			{
				App.TimeCounter.Tick -= StaticBulletScreen;
				if (Mode == 4)
					--App.BottomBulletScreen2;
				else --App.TopBulletScreen2;
				_window.Canvas.Children.Remove(TextBlock);
			}
		}
		private void RollBulletScreen(Storyboard storyboard)
		{
			RollSb = storyboard;
			var rollDa = new DoubleAnimation
			{
				From = _window.Width,
				To = -TextBlock.ActualWidth,
				Duration = TimeSpan.FromSeconds(App.Speed)
			};
			Storyboard.SetTarget(rollDa, TextBlock);
			RollSb.Completed += (_, _) => { _window.Canvas.Children.Remove(TextBlock); };
			RollSb.Children.Add(rollDa);
		}

		private int _delayTime;
		private readonly int _color;
	}
}