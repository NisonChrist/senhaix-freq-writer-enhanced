using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using InTheHand.Bluetooth;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SenhaixFreqWriter.Constants.Common;
using SenhaixFreqWriter.Constants.Shx8x00;
using SenhaixFreqWriter.DataModels.Shx8x00;
using SenhaixFreqWriter.Utils.BLE.Interfaces;
using SenhaixFreqWriter.Utils.BLE.Platforms.Generic;
using SenhaixFreqWriter.Utils.Serial;
using SenhaixFreqWriter.Views.Common;
using SenhaixFreqWriter.Views.Plugin;
#if WINDOWS
using shx.Utils.BLE.Platforms.Windows;
#endif

namespace SenhaixFreqWriter.Views.Shx8x00;

public partial class MainWindow : Window
{
    private ObservableCollection<ChannelData> _listItems = ClassTheRadioData.GetInstance().ChanData;

    private string _savePath = "";

    private ChannelData _tmpChannel;

    private bool _devSwitchFlag = false;

    private IBluetooth _osBle;

    public ObservableCollection<ChannelData> ListItems
    {
        get => _listItems;
        set
        {
            _listItems = value;
            ClassTheRadioData.GetInstance().ChanData = value;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _listItems.CollectionChanged += CollectionChangedHandler;
        Closed += OnWindowClosed;
    }

    private void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action.Equals(NotifyCollectionChangedAction.Add) ||
            e.Action.Equals(NotifyCollectionChangedAction.Remove)) CalcSequence();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Close();
        if (!_devSwitchFlag) Environment.Exit(0);
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
    }

    private void txFreq_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var dataContext = textBox.DataContext as ChannelData;
        var id = dataContext.ChanNum;
        if (string.IsNullOrEmpty(dataContext.TxFreq)) return;

        var parsed = FreqParser(dataContext.TxFreq);
        if (parsed.Equals("-1"))
        {
            dataContext.TxFreq = "";
            ListItems[int.Parse(id)] = dataContext;
            return;
        }

        dataContext.TxFreq = parsed;
        ListItems[int.Parse(id)] = dataContext;
    }

    private void rxFreq_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var dataContext = textBox.DataContext as ChannelData;
        var id = dataContext.ChanNum;
        if (string.IsNullOrEmpty(dataContext.RxFreq)) return;

        var parsed = FreqParser(dataContext.RxFreq);
        if (parsed.Equals("-1"))
        {
            dataContext.RxFreq = "";
            ListItems[int.Parse(id)] = dataContext;
            return;
        }

        // 写入默认值
        if (dataContext.AllEmpty())
        {
            var data = new ChannelData
            {
                RxFreq = parsed,
                TxAllow = "Yes",
                Encrypt = "OFF",
                Pttid = "OFF",
                BandWidth = "W",
                BusyLock = "OFF",
                QtDec = "OFF",
                QtEnc = "OFF",
                ScanAdd = "ON",
                TxPwr = "H",
                SigCode = "1",
                ChanNum = id,
                TxFreq = parsed,
                IsVisable = true
            };
            ListItems[int.Parse(id)] = data;
        }
        else
        {
            dataContext.RxFreq = parsed;
            ListItems[int.Parse(id)] = dataContext;
        }
    }

    private string FreqParser(string freqChk)
    {
        double pFreq;
        // 检查频率范围
        if (!double.TryParse(freqChk, out pFreq))
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "输入频率格式有误").ShowWindowDialogAsync(this);
            // dataContext.RxFreq = "";
            // listItems[int.Parse(id)] = dataContext;
            return "-1";
        }

        if (pFreq < Freq.TheMinFreq || pFreq > Freq.TheMaxFreq)
        {
            MessageBoxManager.GetMessageBoxStandard("注意",
                "频率错误!\n频率范围:" + Freq.TheMinFreq + "--" + Freq.TheMaxFreq).ShowWindowDialogAsync(this);
            return "-1";
        }

        if (freqChk.Length < 9)
        {
            for (var j = freqChk.Length; j < 9; j++)
                freqChk = j != 3 ? freqChk.Insert(j, "0") : freqChk.Insert(j, ".");
        }
        else
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "精度过高！").ShowWindowDialogAsync(this);
            // dataContext.RxFreq = "";
            // listItems[int.Parse(id)] = dataContext;
            return "-1";
        }

        var s = freqChk.Replace(".", "");
        var num5 = uint.Parse(s);
        if (num5 % 125 != 0)
        {
            ushort num7 = 125;
            var num8 = num5 / num7;
            freqChk = (num8 * num7).ToString();
        }

        return freqChk;
    }

    private void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    private async void new_OnClick(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard("注意", "该操作将清空编辑中的信道，确定继续？",
                ButtonEnum.YesNo);

        var result = await box.ShowWindowDialogAsync(this);
        if (result == ButtonResult.No) return;
        // ClassTheRadioData.forceNew();
        // this.listItems = ClassTheRadioData.getInstance().chanData;
        // ClassTheRadioData.getInstance().channeldata.Clear();
        // for (var i = 0; i < listItems.Count; i++)
        // {
        //     var tmp = new ChannelData();
        //     tmp.IsVisable = false;
        //     tmp.ChanNum = i.ToString();
        //     listItems[i] = tmp;
        //     ClassTheRadioData.getInstance().channeldata.Add(tmp);
        // }
        ClassTheRadioData.GetInstance().ForceNewChannel();
        ClassTheRadioData.GetInstance().DtmfData = new DtmfData();
        ClassTheRadioData.GetInstance().FunCfgData = new FunCfgData();
        ClassTheRadioData.GetInstance().OtherImfData = new OtherImfData();
    }

    private async void saveAs_OnClick(object? sender, RoutedEventArgs e)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var topLevel = GetTopLevel(this);
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存配置文件",
            SuggestedFileName = "Backup-SHX8X00-" + ts + ".dat"
        });
        if (file is not null)
        {
            _savePath = new Uri(file.Path.ToString()).LocalPath;
            await using var stream = await file.OpenWriteAsync();
            stream.Seek(0L, SeekOrigin.Begin);
            stream.SetLength(0L);
            ClassTheRadioData.GetInstance().SaveToFile(stream);
            stream.Close();
        }
    }

    private void save_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_savePath))
        {
            Stream stream = new FileStream(_savePath, FileMode.OpenOrCreate);
            stream.Seek(0L, SeekOrigin.Begin);
            stream.SetLength(0L);
            ClassTheRadioData.GetInstance().SaveToFile(stream);
            stream.Close();
        }
        else
        {
            saveAs_OnClick(null, null);
        }
    }

    private void exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
        Environment.Exit(0);
    }

    private void dtmfset_OnClick(object? sender, RoutedEventArgs e)
    {
        var dtmfWindow = new DtmfWindow();
        dtmfWindow.ShowDialog(this);
    }

    private void option_OnClick(object? sender, RoutedEventArgs e)
    {
        new OptionalWindow().ShowDialog(this);
    }

    private async void open_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开备份",
            AllowMultiple = false
        });
        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            ClassTheRadioData.CreatObjFromFile(stream);
        }
    }


    private async void readChannel_OnClick(object? sendser, RoutedEventArgs e)
    {
        var tmp = ClassTheRadioData.GetInstance();
        tmp.Channeldata = tmp.ChanData.ToList();
        await new ProgressBarWindow(OperationType.Read).ShowDialog(this);
    }

    private async void writeChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var flag = false;
        var tmp = ClassTheRadioData.GetInstance();
        tmp.Channeldata = tmp.ChanData.ToList();
        // 检查信道
        for (var a = 0; a < ListItems.Count; a++)
        {
            if (ListItems[a].AllEmpty() || ListItems[a].Filled()) continue;
            // 不写入不完整的信道
            tmp.Channeldata[a] = new ChannelData();
            flag = true;
        }

        if (flag)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("注意", "您有信道未完全填写，写入时将忽略!", ButtonEnum.YesNo);
            var result = await box.ShowWindowDialogAsync(this);
            if (result == ButtonResult.No)
            {
                tmp.Channeldata = tmp.ChanData.ToList();
                return;
            }
        }


        // await new PortSelectionWindow().ShowDialog(this);

        // if (!string.IsNullOrEmpty(MySerialPort.getInstance().TargetPort))
        await new ProgressBarWindow(OperationType.Write).ShowDialog(this);
    }

    private void portSel_OnClick(object? sender, RoutedEventArgs e)
    {
        new PortSelectionWindow().ShowDialog(this);
    }

    private void ForceRefreshUi()
    {
    }

    private void MenuCopyChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        _tmpChannel = ListItems[selected];
    }

    private void MenuCutChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        _tmpChannel = ListItems[selected].DeepCopy();
        ListItems[selected] = new ChannelData();
        CalcSequence();
    }

    private void MenuPasteChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_tmpChannel == null) return;
        var selected = channelDataGrid.SelectedIndex;
        ListItems[selected] = _tmpChannel.DeepCopy();
        CalcSequence();
    }

    private void MenuClrChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = new List<int>();
        foreach (var selectedItem in channelDataGrid.SelectedItems)
            selected.Add(int.Parse(((ChannelData)selectedItem).ChanNum));
        foreach (var o in selected) ListItems[o] = new ChannelData();
        // var selected = channelDataGrid.SelectedIndex;
        // ListItems[selected] = new ChannelData();
        CalcSequence();
    }

    private void MenuDelChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        for (var i = selected; i < 127; i++) ListItems[i] = ListItems[i + 1];
        ListItems[127] = new ChannelData();
        CalcSequence();
    }

    private void MenuInsChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        if (!ListItems[127].AllEmpty())
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "信道127不为空无法插入！").ShowWindowDialogAsync(this);
            return;
        }

        var lastEmp = 0;
        for (var i = 0; i < 127; i++)
            if (!ListItems[i].AllEmpty())
                lastEmp = i;

        for (var i = lastEmp; i > selected; i--) ListItems[i + 1] = ListItems[i];

        ListItems[selected + 1] = new ChannelData();
        CalcSequence();
    }

    private void MenuComChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var cachedChannel = new ObservableCollection<ChannelData>();
        for (var i = 0; i < 128; i++) cachedChannel.Add(new ChannelData());
        var channelCursor = 0;
        for (var i = 0; i < 128; i++)
            //check it via "TxAllow"
            if (!ListItems[i].AllEmpty())
                cachedChannel[channelCursor++] = ListItems[i].DeepCopy();

        for (var i = 0; i < channelCursor; i++) ListItems[i] = cachedChannel[i].DeepCopy();

        for (var i = channelCursor; i < 128; i++) ListItems[i] = new ChannelData();
        CalcSequence();
    }

    private void CalcSequence()
    {
        for (var i = 0; i < ListItems.Count; i++) ListItems[i].ChanNum = i.ToString();
    }

    private async void MenuConnectBT_OnClick(object? sender, RoutedEventArgs e)
    {
        _osBle?.Dispose();
        _osBle = new GenerticShxble();
#if WINDOWS
        _osBle = new WindowsShxble();
#endif
        try
        {
            var available = await _osBle.GetBleAvailabilityAsync();
            // var available = true;
            if (!available)
            {
                MessageBoxManager.GetMessageBoxStandard("注意", "您的系统不受支持或蓝牙未打开！").ShowWindowDialogAsync(this);
                return;
            }
        }
        catch (Exception ed)
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "您的系统不受支持或蓝牙未打开:" + ed.Message).ShowWindowDialogAsync(this);
            return;
        }

        var hint = new HintWindow();
        hint.SetLabelStatus("自动搜索中...");
        hint.SetButtonStatus(false);
        hint.ShowDialog(this);

        if (!await _osBle.ScanForShxAsync())
        {
            hint.SetLabelStatus("未找到设备！\n您可能需要重启软件！");
            hint.SetButtonStatus(true);
            return;
        }

        hint.SetLabelStatus("已找到设备,尝试连接中...");
        // Get Char.....
        try
        {
            await _osBle.ConnectShxDeviceAsync();
        }
#if __LINUX__
        catch (Tmds.DBus.DBusException)
        {
            hint.setLabelStatus("连接失败！\n请在设置-蓝牙中取消对walkie-talkie的连接。\n如果您是初次连接，请在设置中手动配对\nwalkie-talkie并点击配对！");
            hint.setButtonStatus(true);
            return;
        }
#endif
        catch (Exception ea)
        {
            hint.SetLabelStatus("连接失败！" + ea.Message);
            hint.SetButtonStatus(true);
            return;
        }

        // Console.WriteLine("Connected");
        if (!await _osBle.ConnectShxRwServiceAsync())
        {
            hint.SetLabelStatus("未找到写特征\n确认您使用的是8800");
            hint.SetButtonStatus(true);
            return;
        }


        if (!await _osBle.ConnectShxRwCharacteristicAsync())
        {
            hint.SetLabelStatus("未找到写特征\n确认您使用的是8800");
            hint.SetButtonStatus(true);

            return;
        }

        _osBle.RegisterSerial();
        hint.SetLabelStatus("连接成功！\n请点击关闭，并进行读写频");
        hint.SetButtonStatus(true);
        // cable.IsVisible = false;
    }

    private void Characteristic_CharacteristicValueChanged(object sender, GattCharacteristicValueChangedEventArgs e)
    {
        foreach (var b in e.Value) MySerialPort.GetInstance().RxData.Enqueue(b);
    }

    private void Dark_OnClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    private void Light_OnClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Light;
    }

    private void SwitchDevice_OnClick(object? sender, RoutedEventArgs e)
    {
        _devSwitchFlag = true;
        new DeviceSelectWindow().Show();
        Close();
    }

    private async void AdvancedMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        await MessageBoxManager.GetMessageBoxStandard("注意", "请在遵守当地无线电管理相关条例的前提下使用本功能！").ShowWindowDialogAsync(this);
        new OtherFunctionWindow().ShowDialog(this);
    }

    private void BootImageMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new BootImageImportWindow(SHX_DEVICE.SHX8X00).ShowDialog(this);
    }
}