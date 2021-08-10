﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml;
using ModernWpf.Controls;

namespace BiliBulletScreenPlayer
{

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			BulletScreen.Window = this;
			BBackGround.Opacity = App.WindowOpacity;
			MouseLeftButtonDown += (_, _) => DragMove();
			TimeSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseButtonDown), true);
			TimeSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseButtonUp), true);

			for (var i = 0; i < App.Speed; ++i)
				App.SbQueue.Enqueue(null);
			var propertyPath = new PropertyPath("(Canvas.Left)");
			App.TimeCounter.Tick += (_, _) =>
			{
				if (TimeSlider.Value < TimeSlider.Maximum)
				{
					++TimeSlider.Value;
					var indexBegin = App.PoolIndex.IndexOfValue((int)TimeSlider.Value);
					if (indexBegin == -1) return;
					var storyBoard = new Storyboard();
					do App.Pool[indexBegin++].Start(storyBoard, (int)TimeSlider.Value);
					while (indexBegin < App.Pool.Length && App.Pool[indexBegin].Time == (int)TimeSlider.Value);
					if (storyBoard.Children.Count is 0) return;
					storyBoard.Completed += (_, _) => App.SbQueue.Dequeue();
					Storyboard.SetTargetProperty(storyBoard, propertyPath);
					App.SbQueue.Enqueue(storyBoard);
					storyBoard.Begin();
				}
				else
				{
					Pause();
					TimeSlider.Value = 0;
					ScreenAllClear();
				}
			};
		}
		private void FileClick(object sender, RoutedEventArgs e)
		{
			var fileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = "选择xml弹幕文件",
				Filter = "弹幕文件|*.xml",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				ValidateNames = true,
				CheckPathExists = true,
				CheckFileExists = true
			};
			fileDialog.ShowDialog();
			if (fileDialog.FileName is not "")
				FileOpen(fileDialog.FileName);
		}
		private void FrontClick(object sender, RoutedEventArgs e)
		{
			if (Topmost)
			{
				Topmost = false;
				BFront.Content = new FontIcon { Glyph = "\uE718" };
				FadeOut("总在最前端：关闭", 3000);
			}
			else
			{
				Topmost = true;
				BFront.Content = new FontIcon { Glyph = "\uE840" };
				FadeOut("总在最前端：关闭", 3000);
			}
		}
		private void SettingClick(object sender, RoutedEventArgs e) => SettingOpen();
		private void CloseClick(object sender, RoutedEventArgs e) => Close();
		private void DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Left:
					{
						if (TimeSlider.Value - App.FastForward < 0)
							TimeSlider.Value = 0;
						else TimeSlider.Value -= App.FastForward;
						ScreenAllClear();
						break;
					}
				case Key.Right:
					{
						if (TimeSlider.Value + App.FastForward > TimeSlider.Maximum)
							TimeSlider.Value = 0;
						else TimeSlider.Value += App.FastForward;
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
			var settingWindow = new SettingWindow(this);
			_ = settingWindow.ShowDialog();
			BBackGround.Opacity = App.WindowOpacity;
			FadeOut("设置已更新", 3000);
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
				
				var xDoc = new XmlDocument();
				xDoc.Load(path);
				var tempPool = xDoc.SelectSingleNode("i")!.SelectNodes("d");
				var tempList = tempPool!.Cast<XmlNode>().Select(each => new BulletScreen(each)).ToList();
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
				TotalTimeBlock.Text = '/' + TimeSlider.Maximum.ToTime();
				TimeSlider.Value = 0;
				FadeOut("打开文件\n" + path, 3000);
			}
			catch (Exception)
			{
				FadeOut("*不是标准B站弹幕xml文件*\n您可以在 biliplus.com 获取", 3000);
			}
			if (Tb is not null)
				Grid.Children.Remove(Tb);
			BControl.IsHitTestVisible = true;
		}

		private void WDrag_Enter(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
		private void WDrop(object sender, DragEventArgs e) => FileOpen(((Array)e.Data.GetData(DataFormats.FileDrop))!.GetValue(0)!.ToString());

		private void ScreenAllClear()
		{
			Canvas.Children.Clear();
			App.SbQueue.Clear();
			App.RollRoom.Clear();
			App.StaticRoom.Clear();
		}


		private void BFile_MouseEnter(object sender, MouseEventArgs e) => BFile.Visibility = Visibility.Visible;
		private void BFile_MouseLeave(object sender, MouseEventArgs e) => BFile.Visibility = Visibility.Hidden;
		private void BButtons_MouseEnter(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Visible;
		private void BButtons_MouseLeave(object sender, MouseEventArgs e) => SpButtons.Visibility = Visibility.Hidden;
		private void BControl_MouseEnter(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Visible;
		private void BControl_MouseLeave(object sender, MouseEventArgs e) => SpControl.Visibility = Visibility.Hidden;
		private void FadeOut(string message, int mSec)
		{
			TbPath.Text = message;
			TbPath.BeginAnimation(OpacityProperty, new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(mSec) });
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
			PlayButton.Content = new FontIcon { Glyph = "\uEDB4" };
			if (App.Pool is not null)
				foreach (var each in App.SbQueue)
					each?.Resume();
		}
		private void Pause()
		{
			App.PlayPause = false;
			App.TimeCounter.Stop();
			PlayButton.Content = new FontIcon { Glyph = "\uEDB5" };
			if (App.Pool is not null)
				foreach (var each in App.SbQueue)
					each?.Pause();
		}
	}

}