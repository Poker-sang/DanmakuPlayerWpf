using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static BiliBulletScreenPlayer.Properties.Settings;

namespace BiliBulletScreenPlayer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static double WindowOpacity { get; set; } = Default.WindowOpacity;
		public static int Speed { get; set; } = Default.Speed; //弹幕显示速度
		public static int FastForward { get; set; } = Default.FastForward; //快进速度
		public static double Opacity { get; set; } = Default.Opacity; //弹幕透明度
		private static double _playSpeed = Default.PlaySpeed;
		public static double PlaySpeed //倍速
		{
			get => _playSpeed;
			set
			{
				if(Equals(_playSpeed,value))return;
				TimeCounter.Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / PlaySpeed));
				_playSpeed = value;
			}
		}
		public static int BottomBulletScreen2 { get; set; } //底部弹幕数
		public static int TopBulletScreen2 { get; set; } //顶部弹幕数
		public static int RollBulletScreen2 { get; set; } //滚动弹幕数（指未碰到窗口右边缘的弹幕）
		public static bool PlayPause { get; set; } //暂停
		public static DispatcherTimer TimeCounter { get; } = new() { Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / PlaySpeed)) };
		public static Queue<Storyboard> SbQueue { get; } = new(Speed);
		public static BulletScreen[] Pool { get; set; } //弹幕池
		public static SortedList<int, int> PoolIndex { get; set; } //总弹幕池键值表

		public static List<int> StaticRoom { get; } = new(); //静止弹幕空间（等到该空间空余时的播放进度）
		public static List<int> RollRoom { get; } = new(); //滚动弹幕空间（等到该空间空余时的播放进度）
	}
}
