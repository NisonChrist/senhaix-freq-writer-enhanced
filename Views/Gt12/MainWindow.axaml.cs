using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SenhaixFreqWriter.Constants.Gt12;
using SenhaixFreqWriter.DataModels.Gt12;
using SenhaixFreqWriter.Utils.BLE.Interfaces;
using SenhaixFreqWriter.Utils.BLE.Platforms.Generic;
using SenhaixFreqWriter.Utils.HID;
using SenhaixFreqWriter.Views.Common;

namespace SenhaixFreqWriter.Views.Gt12;

public partial class MainWindow : Window
{
    private ObservableCollection<Channel> _listItems = new();

    public ObservableCollection<Channel> listItems
    {
        get => _listItems;
        set => _listItems = value;
    }

    public int currentArea = 0;

    private bool devSwitchFlag = false;

    private string filePath = "";

    private Channel copiedChannel;

    private IBluetooth osBLE;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        setArea(0);
        _listItems.CollectionChanged += CollectionChangedHandler;
        Closed += OnWindowClosed;
        if (HIDTools.getInstance().isDeviceConnected)
        {
            statusLabel.Content = "连接状态：已连接";
        }
        else
        {
            statusLabel.Content = "连接状态：未连接";
        }
        HIDTools.getInstance().updateLabel = connected =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (connected)
                {
                    statusLabel.Content = "连接状态：已连接";
                }
                else
                {
                    statusLabel.Content = "连接状态：未连接";
                }
            });
        };
        HIDTools.getInstance().findAndConnect();
    }
    
    

    private void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Close();
        if (!devSwitchFlag) Environment.Exit(0);
    }

    private void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
    {
        // if (e.Action.Equals(NotifyCollectionChangedAction.Add) ||
        //     e.Action.Equals(NotifyCollectionChangedAction.Remove))
        // {
        calcSeq();
        AppData.getInstance().channelList[currentArea] = listItems.ToArray();
        // }
    }

    private void calcSeq()
    {
        for (var i = 0; i < listItems.Count; i++) listItems[i].Id = i + 1;
    }

    private string calcNameSize(string name)
    {
        var num = 0;
        var num2 = 0;
        var text = name;
        var bytes = Encoding.GetEncoding("gb2312").GetBytes(text);
        if (bytes.Length > 12)
        {
            var num3 = 0;
            while (num3 < 12)
                if (bytes[num3] >= 47 && bytes[num3] < 127)
                {
                    num++;
                    num3++;
                }
                else
                {
                    num2++;
                    num3 += 2;
                }

            text = num % 2 == 0
                ? Encoding.GetEncoding("gb2312").GetString(bytes, 0, 12)
                : Encoding.GetEncoding("gb2312").GetString(bytes, 0, 11);
        }

        return text;
    }

    private void setArea(int area)
    {
        currentArea = area;
        var tmpChannel = AppData.getInstance().channelList[area];
        listItems.Clear();
        for (var i = 0; i < tmpChannel.Length; i++) listItems.Add(tmpChannel[i]);
        areaName.Text = AppData.getInstance().bankName[area];
        areaLabel.Content = $"{area + 1}/30";
    }

    private async void readChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        await new ProgressBarWindow(OP_TYPE.READ).ShowDialog(this);
        setArea(0);
    }

    private async void writeChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        await new ProgressBarWindow(OP_TYPE.WRITE).ShowDialog(this);
        setArea(0);
    }

    private void foreChan_OnClick(object? sender, RoutedEventArgs e)
    {
        if (currentArea > 0) setArea(currentArea - 1);
    }

    private void nextChan_OnClick(object? sender, RoutedEventArgs e)
    {
        if (currentArea < 29) setArea(currentArea + 1);
    }

    private void jumpChan_OnClick(object? sender, RoutedEventArgs e)
    {
        var tarChan = jumpTextBox.Text;
        int res;
        if (int.TryParse(tarChan, out res))
            if (res is > 0 and < 31)
                setArea(res - 1);

        jumpTextBox.Text = "";
    }

    private void Light_OnClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Light;
    }

    private void Dark_OnClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    private void AreaName_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(areaName.Text))
        {
            areaName.Text = AppData.getInstance().bankName[currentArea];
            return;
        }

        areaName.Text = calcNameSize(areaName.Text);
        AppData.getInstance().bankName[currentArea] = calcNameSize(areaName.Text);
    }

    private string freqChecker(string text)
    {
        var num = 0;
        // 检查频率范围
        if (!double.TryParse(text, out _))
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "输入频率格式有误").ShowWindowDialogAsync(this);
            return "-1";
        }

        var array = text.Split('.');
        var list = new List<int>();
        for (var j = 0; j < array.Length; j++) list.Add(int.Parse(array[j]));

        if (list[0] < FREQ.minFreq || list[0] >= FREQ.maxFreq)
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "输入频率格式有误").ShowWindowDialogAsync(this);
            return "-1";
        }

        num = list[0] * 100000;
        if (list.Count > 1)
        {
            var num5 = 5 - array[1].Length;
            if (num5 > 0)
                for (var k = 0; k < num5; k++)
                    list[1] *= 10;

            num += list[1];
        }

        if (num % 125 != 0)
        {
            num /= 125;
            num *= 125;
        }

        return num.ToString().Insert(3, ".");
    }


    private void txFreq_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var dataContext = textBox.DataContext as Channel;
        var id = dataContext.Id;
        if (string.IsNullOrEmpty(dataContext.TxFreq)) return;

        var parsed = freqChecker(dataContext.TxFreq);
        if (parsed.Equals("-1"))
        {
            dataContext.TxFreq = "";
            listItems[id - 1] = dataContext;
            return;
        }

        dataContext.TxFreq = parsed;
        listItems[id - 1] = dataContext;
    }

    private void rxfreq_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var dataContext = textBox.DataContext as Channel;
        var id = dataContext.Id;
        if (string.IsNullOrEmpty(dataContext.RxFreq)) return;

        var parsed = freqChecker(dataContext.RxFreq);
        if (parsed.Equals("-1"))
        {
            dataContext.RxFreq = "";
            listItems[id - 1] = dataContext;
            return;
        }

        // 写入默认值
        if (!dataContext.IsVisable)
        {
            var data = new Channel
            {
                Id = id,
                RxFreq = parsed,
                StrRxCtsDcs = "OFF",
                TxFreq = parsed,
                StrTxCtsDcs = "OFF",
                TxPower = 0,
                Bandwide = 0,
                ScanAdd = 0,
                SignalGroup = 0,
                SqMode = 0,
                Pttid = 0,
                SignalSystem = 0,
                IsVisable = true
            };
            listItems[id - 1] = data;
        }
        else
        {
            dataContext.RxFreq = parsed;
            listItems[id - 1] = dataContext;
        }
    }

    private void SwitchDevice_OnClick(object? sender, RoutedEventArgs e)
    {
        devSwitchFlag = true;
        new DeviceSelectWindow().Show();
        Close();
    }

    private void MenuCopyChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        copiedChannel = listItems[selected];
    }

    private void MenuCutChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        copiedChannel = listItems[selected].DeepCopy();
        listItems[selected] = new Channel();
        calcSeq();
    }

    private void MenuPasteChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (copiedChannel == null) return;
        var selected = channelDataGrid.SelectedIndex;
        listItems[selected] = copiedChannel.DeepCopy();
        calcSeq();
    }

    private void MenuClrChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        listItems[selected] = new Channel();
        calcSeq();
    }

    private void MenuDelChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        for (var i = selected; i < 31; i++) listItems[i] = listItems[i + 1];
        listItems[31] = new Channel();
        calcSeq();
    }

    private void MenuInsChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = channelDataGrid.SelectedIndex;
        if (listItems[31].IsVisable)
        {
            MessageBoxManager.GetMessageBoxStandard("注意", "信道32不为空无法插入！").ShowWindowDialogAsync(this);
            return;
        }

        var lastEmp = 0;
        for (var i = 0; i < 31; i++)
            if (listItems[i].IsVisable)
                lastEmp = i;

        for (var i = lastEmp; i > selected; i--) listItems[i + 1] = listItems[i];

        listItems[selected + 1] = new Channel();
        calcSeq();
    }

    private void MenuComChannel_OnClick(object? sender, RoutedEventArgs e)
    {
        var cached_channel = new ObservableCollection<Channel>();
        for (var i = 0; i < 32; i++) cached_channel.Add(new Channel());
        var channel_cursor = 0;
        for (var i = 0; i < 32; i++)
            if (listItems[i].IsVisable)
                cached_channel[channel_cursor++] = listItems[i].DeepCopy();

        for (var i = 0; i < channel_cursor; i++) listItems[i] = cached_channel[i].DeepCopy();

        for (var i = channel_cursor; i < 32; i++) listItems[i] = new Channel();
        calcSeq();
    }

    private void VfoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new VfoModeWindow().ShowDialog(this);
    }


    private void OptionalMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new OptionalWindow().ShowDialog(this);
    }

    private void FMMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new FMWindow().ShowDialog(this);
    }

    private void DTMFMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new DTMFWindow().ShowDialog(this);
    }

    private async void NewFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard("注意", "该操作将清空编辑中的信道，确定继续？",
                ButtonEnum.YesNo);

        var result = await box.ShowWindowDialogAsync(this);
        if (result == ButtonResult.No) return;
        AppData.forceNewInstance();
        setArea(0);
    }

    private void SaveFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            Stream stream = new FileStream(filePath, FileMode.OpenOrCreate);
            stream.Seek(0L, SeekOrigin.Begin);
            stream.SetLength(0L);
            AppData.getInstance().SaveToFile(stream);
            stream.Close();
        }
        else
        {
            SaveAsMenuItem_OnClick(null, null);
        }
    }

    private async void SaveAsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var topLevel = GetTopLevel(this);
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存配置文件",
            SuggestedFileName = "Backup-GT12-" + ts + ".dat"
        });
        if (file is not null)
        {
            filePath = new Uri(file.Path.ToString()).LocalPath;
            await using var stream = await file.OpenWriteAsync();
            stream.Seek(0L, SeekOrigin.Begin);
            stream.SetLength(0L);
            AppData.getInstance().SaveToFile(stream);
            stream.Close();
        }
    }

    private void ExitMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
        Environment.Exit(0);
    }

    private async void OpenFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
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
            AppData.CreatObjFromFile(stream);
            setArea(0);
        }
    }

    private void ConnectMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var hint = new HintWindow();
        hint.setLabelStatus("(此为调试功能，正常情况软件会自动连接GT12)");
        hint.setButtonStatus(false);
        hint.ShowDialog(this);
        
        hint.setLabelStatus("-------所有HID设备-------");
        foreach (var hidDevice in HIDTools.getAllHIDDevices())
        {
            hint.setLabelStatus($"设备名：{hidDevice.GetProductName()}");
            // hint.setLabelStatus($"序列号：{hidDevice.GetSerialNumber()}");
            hint.setLabelStatus($"VID：{hidDevice.VendorID}");
            hint.setLabelStatus($"PID：{hidDevice.ProductID}");
            hint.setLabelStatus($"路径：{hidDevice.DevicePath}");
            hint.setLabelStatus("----------------");
        }
        
        if (HIDTools.getInstance().findAndConnect()==HID_STATUS.SUCCESS)
        {
            hint.setLabelStatus("连接成功!");
            hint.setLabelStatus("-------设备信息-------");
            var gt12 = HIDTools.getInstance().Gt12Device;
            hint.setLabelStatus($"最大输入长度：{gt12.GetMaxInputReportLength()}");
            hint.setLabelStatus($"最大输出长度：{gt12.GetMaxOutputReportLength()}");
            hint.setLabelStatus($"PID：{gt12.ProductID}");
            hint.setLabelStatus($"VID：{gt12.VendorID}");
            hint.setLabelStatus($"设备路径：{gt12.DevicePath}");
            // hint.setLabelStatus($"序列号：{gt12.GetSerialNumber()}");
            hint.setLabelStatus($"设备名：{gt12.GetProductName()}");
            // hint.setLabelStatus($"文件系统名：{gt12.GetFileSystemName()}");
            // hint.setLabelStatus($"ReNumber：{gt12.ReleaseNumber}");
        }
        else
        {
            hint.setLabelStatus("连接失败！");
        }
        hint.setButtonStatus(true);
    }

    private async void BTMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        osBLE?.Dispose();
        osBLE = new GenerticSHXBLE();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var res = await MessageBoxManager.GetMessageBoxStandard("注意", "macOS和Linux的写频支持不完整！您要继续吗？",ButtonEnum.YesNo).ShowWindowDialogAsync(this);
            if (res.Equals(ButtonResult.No))
            {
                return;
            }
        }
#if WINDOWS
        osBLE = new WindowsSHXBLE();
#endif
        osBLE.setStatusUpdater((status =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (status)
                {
                    statusLabel.Content = "连接状态：蓝牙已连接";
                }
                else
                {
                    statusLabel.Content = "连接状态：蓝牙未连接";
                }
            });
        } ));
        await MessageBoxManager.GetMessageBoxStandard("注意", "蓝牙写频速度真的超级慢...如非紧急情况建议使用写频线").ShowWindowDialogAsync(this);
        // Console.WriteLine("Requesting Bluetooth Device...");
        // for windows and macoos
        try
        {
            var available = await osBLE.GetBLEAvailabilityAsync();
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
        hint.setLabelStatus("自动搜索中...");
        hint.setButtonStatus(false);
        hint.ShowDialog(this);

        if (!await osBLE.ScanForSHXAsync())
        {
            hint.setLabelStatus("未找到设备！\n您可能需要重启软件！");
            hint.setButtonStatus(true);
            return;
        }

        hint.setLabelStatus("已找到设备,尝试连接中...");
        // Get Char.....
        try
        {
            await osBLE.ConnectSHXDeviceAsync();
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
            hint.setLabelStatus("连接失败！" + ea.Message);
            hint.setButtonStatus(true);
            return;
        }

        // Console.WriteLine("Connected");
        if (!await osBLE.ConnectSHXRWServiceAsync())
        {
            hint.setLabelStatus("未找到写特征\n确认您使用的是GT12");
            hint.setButtonStatus(true);
            return;
        }


        if (!await osBLE.ConnectSHXRWCharacteristicAsync())
        {
            hint.setLabelStatus("未找到写特征\n确认您使用的是GT12");
            hint.setButtonStatus(true);
            return;
        }

        osBLE.RegisterHID();
        hint.setLabelStatus("连接成功！\n请点击关闭，并进行读写频");
        hint.setButtonStatus(true);
    }
}