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
using System.Globalization;

namespace BiliBulletScreenPlayer
{
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//public string SomeProperty => $"{DateTime.Now}";
		public MainWindow()
		{
			InitializeComponent();

			MouseLeftButtonDown += (_, _) => { DragMove(); };

			var binding = new Binding("Value")
			{
				Source = TimeSlider,
				Mode = BindingMode.TwoWay,
				Converter = new SliderToTextBlock(),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};
			TimeBlock.SetBinding(TextBlock.TextProperty, binding);


			//var binding2 = new Binding("SomeProperty")
			//{
			//	Source = this,
			//	Mode = BindingMode.OneWay,
			//	UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			//};
			//Tb.SetBinding(TextBlock.TextProperty, binding2);

			App.MainWindowStatic = this;
			for (var i = 0; i < App.ActualTime; ++i)
				App.SbQueue.Enqueue(null);
			var propertyPath = new PropertyPath("(Canvas.Left)");
			App.TimeCounter.Tick += (_, _) =>
			{
				if (TimeSlider.Value < TimeSlider.Maximum)
				{
					++TimeSlider.Value;
					App.SbQueue.Dequeue();
					App.SbQueue.Enqueue(new Storyboard());
					var indexBegin = App.PoolIndex.IndexOfValue((int)TimeSlider.Value);
					if (indexBegin != -1)
						while (indexBegin < App.Pool.Length && App.Pool[indexBegin].Time == (int)TimeSlider.Value) 
							App.Pool[indexBegin++].Start(App.SbQueue.Last());
					Storyboard.SetTargetProperty(App.SbQueue.Last(), propertyPath);
					App.SbQueue.Last().Begin();
				}
				else
				{
					Pause();
					TimeSlider.Value = 0;
					ScreenAllClear();
				}
			};
			BBackGround.Opacity = App.WindowOpacity;
			TimeSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseButtonDown), true);
			TimeSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseButtonUp), true);
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
			App.IsFront = !App.IsFront;
			if (App.IsFront)
			{
				Topmost = true;
				BFront.Content = "☑";
				BFront.FontSize = 15;
				TbPath.Text = "总在最前端：开启";
				FadeOut(TbPath, 3000);
			}
			else
			{
				Topmost = false;
				BFront.Content = "☒";
				BFront.FontSize = 24;
				TbPath.Text = "总在最前端：关闭";
				FadeOut(TbPath, 3000);
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
			switch (e.Key)
			{
				case Key.Left:
					{
						if (TimeSlider.Value - App.Range < 0)
							TimeSlider.Value = 0;
						else TimeSlider.Value -= App.Range;
						ScreenAllClear();
						break;
					}
				case Key.Right:
					{
						if (TimeSlider.Value + App.Range > TimeSlider.Maximum)
							TimeSlider.Value = 0;
						else TimeSlider.Value += App.Range; 
						ScreenAllClear();
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
				Speed = App.Speed,
				Range = App.Range,
				Opacity = App.Opacity,
				Ratio = App.Ratio,
				WindowOpacity = App.WindowOpacity
			})
			{
				SaveSettings = saveSettings =>
				{
					App.Speed = saveSettings.Speed;
					App.Range = saveSettings.Range;
					App.Opacity = saveSettings.Opacity;
					App.Ratio = saveSettings.Ratio;
					App.WindowOpacity = saveSettings.WindowOpacity;
					BBackGround.Opacity = App.WindowOpacity;
				},
				Owner = this,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			settingWindow.SaveSettings = save =>
			{
				App.Speed = save.Speed;
				App.Range = save.Range;
				App.Opacity = save.Opacity;
				if (App.Ratio != save.Ratio)
					App.TimeCounter.Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / save.Ratio));
				App.Ratio = save.Ratio;
				App.WindowOpacity = save.WindowOpacity;
				BBackGround.Opacity = App.WindowOpacity;
				TbPath.Text = "设置已更新";
				FadeOut(TbPath, 3000);
			};
			_ = settingWindow.ShowDialog();
		}
		private void FileOpen(string path)
		{
			try
			{
				Pause();
				ScreenAllClear();
				TimeSlider.Maximum = 0;
				TimeSlider.Value = 0;
				App.Pool = null;
				App.PoolIndex = new SortedList<int, int>();
				//Settings.BottomBulletScreen = new List<BulletScreen>();
				//Settings.TopBulletScreen = new List<BulletScreen>();
				//Settings.RollBulletScreen = new List<BulletScreen>();

				if (!path.EndsWith(".xml"))
					throw new Exception();
				var xDoc = new XmlDocument();
				xDoc.Load(path);
				var iXml = xDoc.SelectSingleNode("i");
				var tempPool = iXml.SelectNodes("d");
				var tempList = (from XmlNode each in tempPool select new BulletScreen(each)).ToList();
				for (var i = 0; i < tempList.Count;)
					if (tempList[i].Mode > 5)
						tempList.RemoveAt(i);
					else ++i;
				tempList = tempList.OrderBy(b => b.Time).ToList();
				App.Pool = new BulletScreen[tempList.Count];
				tempList.CopyTo(App.Pool);
				for (var i = 0; i < App.Pool.Length; ++i)
					App.PoolIndex.Add(i, App.Pool[i].Time);
				TimeSlider.Maximum = App.Pool.Last().Time + 10;
				TotalTimeBlock.Text = '/' + App.SecToTime(TimeSlider.Maximum).ToString();
				TimeSlider.Value = 0;
				TbPath.Text = "打开文件\n" + path;
			}
			catch (Exception)
			{
				TbPath.Text = "*不是标准B站弹幕xml文件*\n您可以在 biliplus.com 获取";
			}
			FadeOut(TbPath, 3000);
			if (Tb != null)
				grid.Children.Remove(Tb);
			ControlGrid.Visibility = Visibility.Visible;
		}

		private void WDrag_Enter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
		private void WDrop(object sender, DragEventArgs e) => FileOpen(((Array)e.Data.GetData(DataFormats.FileDrop))!.GetValue(0)!.ToString());

		private void ScreenAllClear()
		{
			Canvas.Children.Clear();
			for (var i = 0; i < App.ActualTime; ++i)
			{
				App.SbQueue.Dequeue();
				App.SbQueue.Enqueue(null);
			}
			App.RollBulletScreen2 = 0;
			App.TopBulletScreen2 = 0;
			App.BottomBulletScreen2 = 0;
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
			TimeSlider.Visibility = Visibility.Visible;
			PlayButton.Visibility = Visibility.Visible;
			TimeBlock.Visibility = Visibility.Visible;
			TotalTimeBlock.Visibility = Visibility.Visible;
		}
		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			TimeSlider.Visibility = Visibility.Hidden;
			PlayButton.Visibility = Visibility.Hidden;
			TimeBlock.Visibility = Visibility.Hidden;
			TotalTimeBlock.Visibility = Visibility.Hidden;
		}

		private void FadeOut(DependencyObject item, int mSec)
		{			
			var fadeOutSb = new Storyboard();
			Resources.Add(Guid.NewGuid().ToString(), fadeOutSb);
			var fadeOutDa = new DoubleAnimation
			{
				From = 1,
				To = 0,
				Duration = TimeSpan.FromMilliseconds(mSec)
			};
			Storyboard.SetTarget(fadeOutDa, item);
			Storyboard.SetTargetProperty(fadeOutDa, new PropertyPath("Opacity"));
			fadeOutSb.Children.Add(fadeOutDa);
			fadeOutSb.Begin();
		}
		private void Slider_MouseButtonDown(object sender, MouseButtonEventArgs e) => Pause();

		private void Slider_MouseButtonUp(object sender, MouseButtonEventArgs e)
		{
			ScreenAllClear();
			Resume();
		}
		private void PlayPause(object sender, RoutedEventArgs e)
		{
			if (App.PlayPause)
				Pause();
			else Resume();
		}
		private void Resume()
		{
			App.PlayPause = true;
			App.TimeCounter.Start();
			PlayButton.Content = "┃┃";
			PlayButton.FontSize = 20;
			if (App.Pool is not null)
				foreach (var each in App.SbQueue)
					each?.Resume();
		}
		private void Pause()
		{
			App.PlayPause = false;
			App.TimeCounter.Stop();
			PlayButton.Content = "▶";
			PlayButton.FontSize = 25;
			if (App.Pool is not null)
				foreach (var each in App.SbQueue)
					each?.Pause();
		}
	}
	
}
