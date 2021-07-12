using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static System.Convert;

namespace BiliBulletScreenPlayer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static BulletScreen[] Pool { get; set; } //弹幕池
		public static SortedList<int, int> PoolIndex { get; set; } //总弹幕池键值表
		public static Queue<Storyboard> SbQueue { get; } = new(ActualTime);
		public static int ActualTime => 20 - Speed;      //弹幕速度
		public static int Speed { get; set; } = ToInt32(ConfigurationManager.AppSettings[0]); //弹幕显示速度
		public static int Range { get; set; } = ToInt32(ConfigurationManager.AppSettings[1]); //快进速度
		public static double Opacity { get; set; } = ToDouble(ConfigurationManager.AppSettings[2]); //弹幕透明度
		public static double Ratio { get; set; } = ToDouble(ConfigurationManager.AppSettings[3]); //倍速
		public static double WindowOpacity { get; set; } = ToDouble(ConfigurationManager.AppSettings[4]);
		public static int BottomBulletScreen2 { get; set; } //底部弹幕数
		public static int TopBulletScreen2 { get; set; } = 0; //顶部弹幕数
		public static int RollBulletScreen2 { get; set; } = 0; //滚动弹幕数（指未碰到窗口右边缘的弹幕）
		public static bool IsFront { get; set; } //固定最前
		public static bool PlayPause { get; set; } //暂停
		public static DispatcherTimer TimeCounter { get; } = new() { Interval = new TimeSpan(0, 0, 0, 0, (int)(1000 / Ratio)) };
		public static MainWindow MainWindowStatic { get; set; }

		public static string SecToTime(double sec) => ((int)sec / 60).ToString().PadLeft(2, '0') + ':' + ((int)sec % 60).ToString().PadLeft(2, '0');

		//public static List<BulletScreen> BottomBulletScreen { get; set; } = null; //底部弹幕数
		//public static List<BulletScreen> TopBulletScreen { get; set; } = null; //顶部弹幕数
		//public static List<BulletScreen> RollBulletScreen { get; set; } = null; //滚动弹幕数（指未碰到窗口右边缘的弹幕）
	}
}
