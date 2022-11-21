using ModernWpf.Controls;
using System.Text.RegularExpressions;

namespace DanmakuPlayer.Controls;

/// <summary>
/// InputNumberDialog.xaml 的交互逻辑
/// </summary>
public partial class InputNumberDialog : ContentDialog
{
    public InputNumberDialog() => InitializeComponent();

    public bool DialogResult { get; private set; }

    public string Number { get; private set; } = "";

    private void BConfirmClick(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        if (AvRegex().Match(TbInput.Text) is { Success: true } match1)
            Number = match1.Value;
        else if (BvRegex().Match(TbInput.Text) is { Success: true } match2)
            Number = match2.Value;
        else
        {
            e.Cancel = true;
            TbMessage.Text = "未匹配到相应的av或BV号！";
            return;
        }
        DialogResult = true;
        Hide();
    }

    private void BCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs e) => Hide();

    [GeneratedRegex("[aA][vV][0-9]+")]
    private static partial Regex AvRegex();
    [GeneratedRegex("[bB][vV][0-9A-z]+")]
    private static partial Regex BvRegex();
}
