using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Xml;
using System.Timers;
using System.Threading;
using System.Windows.Threading;
using System.Configuration;

namespace DanmuXml
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			grid.MouseLeftButtonDown += (o, e) => { DragMove(); };

			Settings.Mainwindow = this;
			Settings.TimeCounter.Tick += new EventHandler((object sender, EventArgs e) =>
			{
				if (Settings.Time < Settings.TotalTime)
				{
					++Settings.Time;
					if (!slider.IsFocused)
						slider.Value = Settings.Time;
					if (!PlayTime.IsFocused)
						PlayTime.Text= SecToTime(Settings.Time) + '/' + SecToTime(Settings.TotalTime);
					var IndexBegin = Settings.PoolIndex.IndexOfValue(Settings.Time);        //静止弹幕
					if (IndexBegin != -1)
						while (Settings.Pool[IndexBegin].Time == Settings.Time && IndexBegin < Settings.Pool.Length) 
							Settings.Pool[IndexBegin++].Start();
				}
				else
				{
					PlayPause(null, null);
					Settings.Time = 0;
				}
			});
			BBackGround.Opacity = Settings.WindowOpacity;
			slider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(SliderPressed), true);
		}
		private void FileClick(object sender, RoutedEventArgs e)
		{
			var fileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = "选择xml弹幕文件",
				Filter = "弹幕|*.xml",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				ValidateNames = true,
				CheckPathExists = true,
				CheckFileExists = true
			};
			fileDialog.ShowDialog();
			if (fileDialog.FileName != string.Empty) 
				FileOpen(fileDialog.FileName);
		}
		private void FrontClick(object sender, RoutedEventArgs e)
		{
			Settings.IsFront = !Settings.IsFront;
			if (Settings.IsFront)
			{
				Topmost = true;
				BFront.Content = "☑";
				BFront.FontSize = 15;
				Pathtextblock.Text = "总在最前端：开启";
				FadeOut(Pathtextblock, 3000);
			}
			else
			{
				Topmost = false;
				BFront.Content = "☒";
				BFront.FontSize = 24;
				Pathtextblock.Text = "总在最前端：关闭";
				FadeOut(Pathtextblock, 3000);
			}
		}
		private void SettingClick(object sender, RoutedEventArgs e) => SettingOpen();
		private void CloseClick(object sender, RoutedEventArgs e) => Close();
		private void DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				WindowState = WindowState == WindowState.Maximized ?
							  WindowState.Normal : WindowState.Maximized;
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.Left:
					{
						if (slider.Value - Settings.Range < 0)
							slider.Value = 0;
						else slider.Value -= Settings.Range;
						ScreenAllClear();
						SliderChanged();
						break;
					}
				case Key.Right:
					{
						if (slider.Value + Settings.Range > Settings.TotalTime)
							slider.Value = 0;
						else slider.Value += Settings.Range; 
						ScreenAllClear();
						SliderChanged();
						break;
					}
				case Key.Space:
					{
						PlayPause(null, null);
						break;
					}
				case Key.Tab:
					{
						SettingOpen();
						break;
					}
			}
		}
		private void SettingOpen()
		{
			var settingWindow = new SettingWindow(new SaveSettings()
			{
				Speed = Settings.Speed,
				Range = Settings.Range,
				Opacity = Settings.Opacity,
				Ratio = Settings.Ratio,
				WindowOpacity = Settings.WindowOpacity
			})
			{
				saveSettings = (SaveSettings saveSettings) =>
				{
					Settings.Speed = saveSettings.Speed;
					Settings.Range = saveSettings.Range;
					Settings.Opacity = saveSettings.Opacity;
					Settings.Ratio = saveSettings.Ratio;
					Settings.WindowOpacity = saveSettings.WindowOpacity;
					BBackGround.Opacity = Settings.WindowOpacity;
				},
				Owner = this,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			settingWindow.saveSettings = (SaveSettings save) =>
			{
				Settings.Speed = save.Speed;
				Settings.Range = save.Range;
				Settings.Opacity = save.Opacity;
				if (Settings.Ratio != save.Ratio)
					Settings.TimeCounter.Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / save.Ratio));
				Settings.Ratio = save.Ratio;
				Settings.WindowOpacity = save.WindowOpacity;
				BBackGround.Opacity = Settings.WindowOpacity;
				Pathtextblock.Text = "设置已更新";
				FadeOut(Pathtextblock, 3000);
			};
			settingWindow.ShowDialog();
		}
		private void FileOpen(string path)
		{
			try
			{
				Settings.PlayPause = true;     //初始化
				PlayPause(null, null);
				ScreenAllClear();
				slider.Maximum = 0;
				slider.Value = 0;
				Settings.Time = 0;
				Settings.TotalTime = 0;
				PlayTime.Text = "00:00/00:00";
				Settings.Pool = null;
				Settings.PoolIndex = new SortedList<int, int>();

				if (!path.EndsWith(".xml"))
					throw new Exception();
				var xDoc = new XmlDocument();
				xDoc.Load(path);
				var iXml = xDoc.SelectSingleNode("i");
				var MaxDanmuLimit = Convert.ToInt32(iXml.SelectNodes("maxlimit")[0].InnerText);
				var tempPool = iXml.SelectNodes("d");
				var tempList = new List<DanMu>();
				foreach (XmlNode each in tempPool)
					tempList.Add(new DanMu(each));
				for (int i = 0; i < tempList.Count;)
					if (tempList[i].Mode > 5)
						tempList.RemoveAt(i);
					else ++i;
				tempList = tempList.OrderBy(b => b.Time).ToList();
				Settings.Pool = new DanMu[tempList.Count];
				tempList.CopyTo(Settings.Pool);
				for (int i = 0; i < Settings.Pool.Length; ++i)
					Settings.PoolIndex.Add(i, Settings.Pool[i].Time);
				Settings.TotalTime = Settings.Pool.Last().Time + 10;
				slider.Maximum = Settings.TotalTime;
				PlayTime.Text = "00:00/" + SecToTime(Convert.ToInt32(Settings.TotalTime));
				Pathtextblock.Text = "打开文件\n" + path;
			}
			catch (Exception)
			{
				Pathtextblock.Text = "*不是标准B站弹幕xml文件*\n您可以在 biliplus.com 获取";
			}
			FadeOut(Pathtextblock, 3000);
			if (textblock != null)
				grid.Children.Remove(textblock);
			ControlGrid.Visibility = Visibility.Visible;
		}

		private void WDrag_Enter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ?
																				DragDropEffects.Link : DragDropEffects.None;
		private void WDrop(object sender, DragEventArgs e) => FileOpen(((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());

		private void ScreenAllClear()
		{
			canvas.Children.Clear();
			Settings.RollDanmu = 0;
			Settings.TopDanmu = 0;
			Settings.ButtomDanmu = 0;
		}


		private void BFile_MouseEnter(object sender, MouseEventArgs e) => BFile.Visibility = Visibility.Visible;
		private void BFile_MouseLeave(object sender, MouseEventArgs e) => BFile.Visibility = Visibility.Hidden;
		private void Border_MouseEnter(object sender, MouseEventArgs e)
		{
			BFront.Visibility = Visibility.Visible;
			BSetting.Visibility = Visibility.Visible;
			BClose.Visibility = Visibility.Visible;
		}
		private void Border_MouseLeave(object sender, MouseEventArgs e)
		{
			BFront.Visibility = Visibility.Hidden;
			BSetting.Visibility = Visibility.Hidden;
			BClose.Visibility = Visibility.Hidden;
		}
		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			slider.Visibility = Visibility.Visible;
			Play.Visibility = Visibility.Visible;
			PlayTime.Visibility = Visibility.Visible;
		}
		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			slider.Visibility = Visibility.Hidden;
			Play.Visibility = Visibility.Hidden;
			PlayTime.Visibility = Visibility.Hidden;
		}

		private void FadeOut(DependencyObject item, int MilliSec)
		{			
			var FadeOutSB = new Storyboard();
			Resources.Add(Guid.NewGuid().ToString(), FadeOutSB);
			var FadeOutDA = new DoubleAnimation
			{
				From = 1,
				To = 0,
				Duration = TimeSpan.FromMilliseconds(MilliSec)
			};
			Storyboard.SetTarget(FadeOutDA, item);
			Storyboard.SetTargetProperty(FadeOutDA, new PropertyPath("Opacity"));
			FadeOutSB.Children.Add(FadeOutDA);
			FadeOutSB.Begin();
		}

		private void SliderPressed(object sender, MouseButtonEventArgs e)
		{
			if (slider.Value != Settings.Time)
			{
				ScreenAllClear();
				SliderChanged();
			}
		}

		private void SliderChanged()
		{
			Settings.Time = Convert.ToInt32(slider.Value);
			PlayTime.Text = SecToTime(Settings.Time) + '/' + SecToTime(Settings.TotalTime);
		}
		private void TextBoxChanged()
		{
			try
			{
				Settings.Time = TimeToSec(PlayTime.Text.Split('/')[0]);
				slider.Value = Settings.Time;
			}
			catch (Exception) { SliderChanged(); }
		}

		private void PlayTime_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Space)
				TextBoxChanged();
		}
		private void PlayTime_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => PlayTime.Text = "";
		private void PlayTime_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => TextBoxChanged();
		private void PlayPause(object sender, RoutedEventArgs e)
		{
			Settings.PlayPause = !Settings.PlayPause;
			if (Settings.PlayPause)
			{
				Settings.TimeCounter.Start();
				Play.Content = "┃┃";
				Play.FontSize = 20;
				if (Settings.Pool != null) 
					foreach (var each in Settings.Pool)
						if (each.RollSB != null)
							each.RollSB.Resume();
			}
			else
			{
				Settings.TimeCounter.Stop();
				Play.Content = "▶";
				Play.FontSize = 25;
				if (Settings.Pool != null)
					foreach (var each in Settings.Pool)
						if (each.RollSB != null)
							each.RollSB.Pause();
			}
		}

		private static string SecToTime(double Sec) => (Convert.ToInt32(Sec) / 60).ToString().PadLeft(2, '0') + ':' + (Convert.ToInt32(Sec) % 60).ToString().PadLeft(2, '0');
		private static int TimeToSec(string Time)
		{
			string[] temp = new string[1];
			if (Time.Contains(':'))
				temp = Time.Split(':');
			else if (Time.Contains('.'))
				temp = Time.Split('.');
			else temp[0] = Time;

			if (temp.Length == 2)
			{
				if (int.TryParse(temp[0], out int High) && int.TryParse(temp[1], out int Low))
					return 60 * High + Low;
			}
			else if (temp.Length == 1)
				if (int.TryParse(temp[0], out int Out))
					return Out;
			throw new Exception();
		}
	}
	public static class Settings
	{
		public static int Time { get => time; set => time = value; }
		public static double TotalTime { get => totalTime; set => totalTime = value; }      //推测视频总长度
		public static DanMu[] Pool { get => pool; set => pool = value; }        //弹幕池
		public static SortedList<int, int> PoolIndex { get => poolIndex; set => poolIndex = value; }        //总弹幕池键值表


		public static int ActualTime => 20 - speed;      //弹幕速度
		public static int Speed { get => speed; set => speed = value; }     //弹幕显示速度
		public static int Range { get => range; set => range = value; }     //快进速度
		public static double Opacity { get => opacity; set => opacity = value; }       //弹幕透明度
		public static double Ratio { get => ratio; set => ratio = value; }       //倍速
		public static double WindowOpacity { get => windowOpacity; set => windowOpacity = value; }

		public static int ButtomDanmu { get => buttomDanmu; set => buttomDanmu = value; }       //底部弹幕数
		public static int TopDanmu { get => topDanmu; set => topDanmu = value; }       //顶部弹幕数
		public static int RollDanmu { get => rollDanmu; set => rollDanmu = value; }       //滚动弹幕数（指未碰到窗口右边缘的弹幕）

		public static bool IsFront { get => isFront; set => isFront = value; }        //固定最前
		public static bool PlayPause { get => playPause; set => playPause = value; }        //暂停
		public static DispatcherTimer TimeCounter { get => timeCounter; set => timeCounter = value; }
		public static MainWindow Mainwindow { get => mainWindow; set => mainWindow = value; }

		private static int time;
		private static double totalTime = 0;
		private static DanMu[] pool;
		private static SortedList<int, int> poolIndex = null;

		private static int speed = Convert.ToInt32(ConfigurationManager.AppSettings[0]);       //1-10
		private static int range = Convert.ToInt32(ConfigurationManager.AppSettings[1]);       //1-20
		private static double opacity = Convert.ToDouble(ConfigurationManager.AppSettings[2]);
		private static double ratio = Convert.ToDouble(ConfigurationManager.AppSettings[3]);       //0.5/0.75/1/1.5/2
		private static double windowOpacity = Convert.ToDouble(ConfigurationManager.AppSettings[4]);

		private static int buttomDanmu = 0;
		private static int topDanmu = 0;
		private static int rollDanmu = 0;

		private static bool isFront = false;
		private static bool playPause = false;
		private static DispatcherTimer timeCounter = new DispatcherTimer
		{ Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / ratio)) };
		private static MainWindow mainWindow;
	}
	public class DanMu
	{
		public int Time => time;
		public int Mode => mode;
		public int Size => size;		//暂时不使用
		public int Color => color;
		public string Text => text;
		public Storyboard RollSB { get => rollSB; set => rollSB = value; }
		public TextBlock TextBlock => textBlock;

		public DanMu(XmlNode xmlNode)
		{
			var tempInfo = ((XmlElement)xmlNode).GetAttribute("p").Split(",");
			time = (int)Convert.ToDouble(tempInfo[0]);
			mode = Convert.ToInt32(tempInfo[1]);
			//size = Convert.ToInt32(tempInfo[2]);
			color = Convert.ToInt32(tempInfo[3]);
			text = ((XmlElement)xmlNode).InnerText;
		}
		public void Start()
		{
			if (mode == 3 && Settings.ButtomDanmu > Settings.Mainwindow.Height / 33)
				Settings.ButtomDanmu = 0;
			else if (mode == 4 && Settings.TopDanmu > Settings.Mainwindow.Height / 33)
				Settings.TopDanmu = 0;
			else if (mode < 4 && Settings.RollDanmu > Settings.Mainwindow.Height / 33)
				Settings.RollDanmu = 0;

			TextBlock.Style = (Style)TextBlock.FindResource("DanMuBlock");
			TextBlock.Opacity = Settings.Opacity;
			TextBlock.Text = text;
			Settings.Mainwindow.canvas.Children.Add(TextBlock);
			TextBlock.UpdateLayout();
			//textBlock.Foreground = ;
			TextBlock.Visibility = Visibility.Visible;

			if (mode == 4)      //底部
				Canvas.SetTop(TextBlock, Settings.Mainwindow.Height - (Settings.ButtomDanmu++ * 33 % Settings.Mainwindow.Height));        //先赋值再自增
			else if (mode == 5)     //顶部
				Canvas.SetTop(TextBlock, Settings.TopDanmu++ * 33 % Settings.Mainwindow.Height);
			else           //滚动
			{
				Canvas.SetTop(TextBlock, Settings.RollDanmu++ * 33 % Settings.Mainwindow.Height);
				RollDanmu();
				return;     //不执行下面的语句
			}
			Canvas.SetLeft(TextBlock, (Settings.Mainwindow.Width - TextBlock.ActualWidth) / 2);
			Settings.TimeCounter.Tick += StaticDanmu;
		}
		private void StaticDanmu(object sender, EventArgs e)
		{
			if (delayTime < Settings.ActualTime)
				++delayTime;
			else
			{
				Settings.TimeCounter.Tick -= StaticDanmu;
				if (mode == 4)
					--Settings.ButtomDanmu;
				else --Settings.TopDanmu;
				Settings.Mainwindow.canvas.Children.Remove(TextBlock);
			}
		}
		private void RollDanmu()
		{
			RollSB = new Storyboard();
			Settings.Mainwindow.Resources.Add(Guid.NewGuid().ToString(), RollSB);
			var RollDA = new DoubleAnimation
			{
				From = Settings.Mainwindow.Width,
				To = -TextBlock.ActualWidth,
				Duration = TimeSpan.FromSeconds(Settings.ActualTime)
			};
			Storyboard.SetTarget(RollDA, TextBlock);
			Storyboard.SetTargetProperty(RollDA, new PropertyPath("(Canvas.Left)"));
			RollSB.Completed += (object sender, EventArgs e) => { Settings.Mainwindow.canvas.Children.Remove(TextBlock); };
			RollSB.Children.Add(RollDA);
			RollSB.Begin();
		}

		private int delayTime = 0;
		private readonly int time = 0;
		private readonly int mode = 0;
		private readonly int size = 0;
		private readonly int color = 0;
		private readonly string text = "";
		private Storyboard rollSB = null;
		private readonly TextBlock textBlock = new TextBlock();
	}
}
