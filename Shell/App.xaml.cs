using System;
using System.Threading;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.IntelligentEquipment
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
        Mutex mutex;//这个必须放在OnStartup外面
        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, "FA06-005", out bool ret);//这里填写程序名称
            if (!ret)
            {
                MessageBox.Warning("软件已经启动！");
                Environment.Exit(0);
            }
            base.OnStartup(e);
        }
    }
}
