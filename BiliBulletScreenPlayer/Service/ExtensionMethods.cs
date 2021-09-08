
namespace BiliBulletScreenPlayer
{
	public static class ExtensionMethods
	{
		public static string ToTime(this double sec) => $"{((int)sec / 60).ToString().PadLeft(2, '0')}:{((int)sec % 60).ToString().PadLeft(2, '0')}";
	}
}
