using DanmakuPlayer.Models;
using DanmakuPlayer.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace DanmakuPlayer.Controls;

public partial class InputDialog : UserControl
{
    public InputDialog() => InitializeComponent();

    private BiliHelper.CodeType _result;
    private int _cId;
    private Action<int> _callback = null!;

    public async void ShowAsync(Action<int> callback)
    {
        _callback = callback;
        _ = await Dialog.ShowAndWaitAsync();
    }

    private void CancelDialogClick(object sender, RoutedEventArgs e) => ((Dialog)sender).Hide();

    private async void ConfirmClick(object sender, RoutedEventArgs e)
    {
        _result = TbInput.Text.Match(out var match);
        var code = 0;
        if (_result is not BiliHelper.CodeType.BvId and not BiliHelper.CodeType.Error)
            code = int.Parse(match);
        switch (_result)
        {
            case BiliHelper.CodeType.Error:
                IbMessage.Message = "未匹配到相应的视频！";
                IbMessage.Severity = InfoBarSeverity.Error;
                IbMessage.IsOpen = true;
                break;
            case BiliHelper.CodeType.AvId:
                ItemsSource = (await BiliHelper.Av2CIds(code)).ToArray();
                CheckItemsSource();
                break;
            case BiliHelper.CodeType.BvId:
                ItemsSource = (await BiliHelper.Bv2CIds(match)).ToArray();
                CheckItemsSource();
                break;
            case BiliHelper.CodeType.CId:
                _cId = code;
                _ = ((Dialog)sender).Hide();
                _callback(_cId);
                break;
            case BiliHelper.CodeType.MediaId:
                code = await BiliHelper.Md2Ss(code);
                goto case BiliHelper.CodeType.SeasonId;
            case BiliHelper.CodeType.SeasonId:
                ItemsSource = (await BiliHelper.Ss2CIds(code)).ToArray();
                CheckItemsSource();
                break;
            case BiliHelper.CodeType.EpisodeId:
                _cId = await BiliHelper.Ep2CId(code);
                _ = ((Dialog)sender).Hide();
                _callback(_cId);
                break;
            default:
                ThrowHelper.ArgumentOutOfRange(_result);
                break;
        }
    }

    private VideoPage[] ItemsSource { get; set; } = Array.Empty<VideoPage>();

    private void CheckItemsSource()
    {
        switch (ItemsSource.Length)
        {
            case 0:
                IbMessage.Message = "未匹配到相应的视频！";
                IbMessage.Severity = InfoBarSeverity.Error;
                IbMessage.IsOpen = true;
                break;
            case 1:
                _cId = ItemsSource[0].CId;
                _ = Dialog.Hide();
                _callback(_cId);
                break;
            case > 1:
                DgPage.ItemsSource = ItemsSource;
                Dialog.DialogHeight = 800;
                Dialog.DialogWidth = 600;
                IbMessage.Message = "请双击选择一个视频：";
                IbMessage.Severity = InfoBarSeverity.Informational;
                IbMessage.IsOpen = true;
                break;
        }
    }

    private void PageMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var index = ((ListBox)sender).SelectedIndex;
        _cId = ItemsSource[index].CId;
        _ = Dialog.Hide();
        _callback(_cId);
    }

    private void DgPagePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        DgPage.RaiseEvent(args);
    }
}
