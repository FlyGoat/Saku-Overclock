﻿using System.Diagnostics;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Saku_Overclock.Helpers;
using Windows.UI.ViewManagement;
using System.Windows.Forms;
using Saku_Overclock.Contracts.Services;
using Saku_Overclock.ViewModels;
using Saku_Overclock.Views;
using System.Runtime.InteropServices;
using Saku_Overclock.SMUEngine;

namespace Saku_Overclock;
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
#pragma warning disable CS8622 // Допустимость значений NULL для ссылочных типов в типе параметра не соответствует целевому объекту делегирования (возможно, из-за атрибутов допустимости значений NULL).
#pragma warning disable CA1041 // Укажите сообщение ObsoleteAttribute

public sealed partial class MainWindow : WindowEx
{
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
    private UISettings settings;
    private Config config = new();
    private Devices devices = new();
    // The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33
    }

    // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
    // what value of the enum to set.
    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }
    public System.Windows.Forms.NotifyIcon ni = new();
    // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
    [LibraryImport("dwmapi.dll", SetLastError = true)]
    private static partial long DwmSetWindowAttribute(IntPtr hwnd,
                                                     DWMWINDOWATTRIBUTE attribute,
                                                     ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                     uint cbAttribute);

    [Obsolete]
    public MainWindow()
    {
        InitializeComponent();
        WindowStateChanged += (sender, e) =>
        {
            if(WindowState == WindowState.Minimized)
            { 
                // Скройте окно
                this.Hide();
            }
        };
        ConfigLoad();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized(); 
        try
        {
            ni.Icon = new System.Drawing.Icon(GetType(), "WindowIcon.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                    delegate (object sender, EventArgs args)
                    {
                        this.Show();
                        WindowState = WindowState.Normal;
                    };
            ni.ContextMenuStrip = new ContextMenuStrip();
            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(ni.ContextMenuStrip.Handle, attribute, ref preference, sizeof(uint));
            System.Drawing.Image bmp = new System.Drawing.Bitmap(GetType(), "show.png");
            System.Drawing.Image bmp1 = new System.Drawing.Bitmap(GetType(), "exit.png");
            System.Drawing.Image bmp2 = new System.Drawing.Bitmap(GetType(), "WindowIcon.ico");
            System.Drawing.Image bmp3 = new System.Drawing.Bitmap(GetType(), "preset.png");
            System.Drawing.Image bmp4 = new System.Drawing.Bitmap(GetType(), "param.png");
            System.Drawing.Image bmp5 = new System.Drawing.Bitmap(GetType(), "info.png");
            ni.ContextMenuStrip.Items.Add("Tray_Saku_Overclock".GetLocalized(), bmp2, Menu_Show1);
            ni.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            ni.ContextMenuStrip.Items.Add("Tray_Presets".GetLocalized(), bmp3, Open_Preset);
            ni.ContextMenuStrip.Items.Add("Tray_Parameters".GetLocalized(), bmp4, Open_Param);
            ni.ContextMenuStrip.Items.Add("Tray_Inforfation".GetLocalized(), bmp5, Open_Info);
            ni.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            ni.ContextMenuStrip.Items.Add("Tray_Show".GetLocalized(), bmp, Menu_Show1);
            ni.ContextMenuStrip.Items.Add("Tray_Quit".GetLocalized(), bmp1, Menu_Exit1);
            ni.ContextMenuStrip.Items[0].Enabled = false;
            ni.ContextMenuStrip.Opacity = 0.89;
            ni.ContextMenuStrip.ForeColor = System.Drawing.Color.Purple;
            ni.ContextMenuStrip.BackColor = System.Drawing.Color.White;
            ni.Text = "Saku Overclock";
        }
        catch
        {

        }
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event   
        DeviceLoad(); 
        Tray_Start();
        Set_Blue();
        Closed += Dispose_Tray;
    }

    private void Dispose_Tray(object sender, WindowEventArgs args)
    {
        try
        {
            ni.Dispose();
        }
        catch
        {
            // ignored
        }
    }
    private async void Set_Blue()
    {
        try
        {
            await Task.Delay(120);
            if (config.bluetheme)
            {
                if (App.MainWindow.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = ElementTheme.Dark;
                    TitleBarHelper.UpdateTitleBar(ElementTheme.Dark);
                }
                var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
                App.MainWindow.SystemBackdrop = micaBackdrop;
            }
        }
        catch
        {
            JsonRepair('c');
        }
    }

    [Obsolete]
    private async void Tray_Start()
    {
        ConfigLoad();
        try
        {
            if (config.autooverclock == true) { var cpu = new ПараметрыPage(); Applyer.Apply(false); /*cpu.Play_Invernate_QuickSMU(1);*/ if (devices.autopstate == true && devices.enableps == true) { cpu.BtnPstateWrite_Click(); }  }
            if (config.traystart == true) { await Task.Delay(700); this.Hide(); }
        }
        catch
        {
            JsonRepair('c');
            JsonRepair('d');
        }
    }
    private void Open_Preset(object sender, EventArgs e)
    {
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ПресетыViewModel).FullName!);
        this.Show();
        BringToFront();
    }
    private void Open_Param(object sender, EventArgs e)
    {
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ПараметрыViewModel).FullName!);
        this.Show();
        BringToFront();
    } 
    private void Open_Info(object sender, EventArgs e)
    {
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ИнформацияViewModel).FullName!);
        this.Show();
        BringToFront();
    } 
    private void Menu_Show1(object sender, EventArgs e)
    {
        ni = new NotifyIcon
        {
            Visible = true
        };
        this.Show();
        BringToFront();
    } 
    private void Menu_Exit1(object sender, EventArgs e)
    {
        ni = new NotifyIcon
        {
            Visible = false
        };
        Close();
    }
    public class Applyer
    {
        public bool execute = false;
        private Config config = new(); 
        [Obsolete]
        private static SendSMUCommand sendSMUCommand;

        [Obsolete]
        public static async void Apply(bool saveinfo)
        {
            try { sendSMUCommand = new SendSMUCommand(); } catch { return; }
            var mc = new Applyer();
            var smu = new ПараметрыPage();
            void ConfigLoad()
            {
                mc.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json"));
            }
            void ConfigSave()
            {
                    Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SakuOverclock"));
                    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json", JsonConvert.SerializeObject(mc.config,Formatting.Indented));
            }

            ConfigLoad();
            

            if (mc.config.reapplytime == true)
            {
                var timer = new DispatcherTimer();
                try
                {
                    timer.Interval = TimeSpan.FromMilliseconds(mc.config.reapplytimer * 1000);
                }
                catch
                {
                    await App.MainWindow.ShowMessageDialogAsync("Время автообновления разгона некорректно и было исправлено на 3000 мс", "Критическая ошибка!");
                    mc.config.reapplytimer = 3000;
                    timer.Interval = TimeSpan.FromMilliseconds(mc.config.reapplytimer);
                }
                if (mc.config.execute == false)
                {
                    mc.config.execute = true;
                    ConfigSave();
                    timer.Tick += async (sender, e) =>
                    {
                        if (mc.config.reapplytime == true)
                        {
                            // Запустите ryzenadj снова
                            await Process(false);
                            sendSMUCommand.Play_Invernate_QuickSMU(1);
                        }
                    };
                    timer.Start();
                }
                else
                {
                    timer.Stop();
                    timer.Tick += async (sender, e) =>
                    {
                        if (mc.config.reapplytime == true)
                        {
                            // Запустите ryzenadj снова
                            await Process(false);
                            sendSMUCommand.Play_Invernate_QuickSMU(1);
                        }
                    };
                    timer.Start();
                }
                
            }
         /*   else
            {*/
                await Process(saveinfo);
         /*   }*/
        }

        [Obsolete]
        private static async Task Process(bool saveinfo)
        {
          //  var sendSMUCommand = new SendSMUCommand();
            var mc = new Applyer
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json"))
            };
            if (mc.config == null) { return; }
            await Task.Run(() =>
            {
                sendSMUCommand.Translate(mc.config.adjline, saveinfo); 
            });
        } 
    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
    //Json
    public void JsonRepair(char file)
    {
        if (file == 'c')
        {
            try
            {
                config = new Config();
            }
            catch
            {
                Close();
            }
            if (config != null)
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SakuOverclock"));
                    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                catch
                {
                    Close();
                }
            }
            else
            {
                try
                {
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json");
                    Close();
                }
                catch
                {
                    Close();
                }
            }
        }
        if (file == 'd')
        {
            try
            {
                for (var j = 0; j < 5; j++)
                {
                    devices = new Devices();
                }
            }
            catch
            {
                Close();
            }
            if (devices != null)
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SakuOverclock"));
                    File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\devices.json", JsonConvert.SerializeObject(devices, Formatting.Indented));
                }
                catch
                {
                    Close();
                }
            }
            else
            {
                try
                {
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\devices.json");
                    Close();
                }
                catch
                {
                    Close();
                }
            }
        } 
    }

    public void ConfigSave()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SakuOverclock"));
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch { }
    }

    public void ConfigLoad()
    {
        try
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\config.json"));
        }
        catch
        {
            JsonRepair('c');
        }
    }

    public void DeviceSave()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SakuOverclock"));
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\devices.json", JsonConvert.SerializeObject(devices, Formatting.Indented));
        }
        catch { }
    }

    public void DeviceLoad()
    {
        try
        {
            devices = JsonConvert.DeserializeObject<Devices>(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SakuOverclock\\devices.json"));
        }
        catch
        {
            JsonRepair('d');
        }
    }
     
}
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
#pragma warning restore CS8622 // Допустимость значений NULL для ссылочных типов в типе параметра не соответствует целевому объекту делегирования (возможно, из-за атрибутов допустимости значений NULL).