using DanmakuPlayer.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using Wpf.Ui.Controls;

namespace DanmakuPlayer;

public partial class MainWindow
{
    private string? InputResult { get; set; }

    private void BConfirmClick(object sender, RoutedEventArgs e)
    {
        if (AvRegex().Match(TbInput.Text) is { Success: true } avMatch)
            InputResult = "BV" + Convert.ToInt32(avMatch.Value[2..]).Av2Bv();
        else if (BvRegex().Match(TbInput.Text) is { Success: true } bvMatch)
            InputResult = bvMatch.Value;
        else
        {
            IbMessage.IsOpen = true;
            return;
        }
        _ = ((Dialog)sender).Hide();
    }

    [GeneratedRegex("[aA][vV][0-9]+")]
    private static partial Regex AvRegex();

    [GeneratedRegex("[bB][vV][0-9A-z]+")]
    private static partial Regex BvRegex();
}
