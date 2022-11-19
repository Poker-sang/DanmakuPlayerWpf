using ModernWpf.Controls;
using System.Text.RegularExpressions;

namespace BiliBulletScreenPlayer.Controls;

/// <summary>
/// InputNumberDialog.xaml 的交互逻辑
/// </summary>
public partial class InputNumberDialog : ContentDialog
{
    public InputNumberDialog() => InitializeComponent();
    public bool DialogResult { get; private set; }
    public string Number { get; private set; }
    private void BConfirm_Click(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        if (new Regex("[aA][vV][0-9]+").Match(TbInput.Text) is { Success: true } match1)
            Number = match1.Value;
        else if (new Regex("[bB][vV][0-9A-z]+").Match(TbInput.Text) is { Success: true } match2)
            Number = match2.Value;
        else
        {
            e.Cancel = true;
            TbMessage.Text = "未匹配到相应的av或bv号！";
            return;
        }
        DialogResult = true;
        Hide();
    }

    private void BCancel_Click(ContentDialog contentDialog, ContentDialogButtonClickEventArgs e) => Hide();
}
