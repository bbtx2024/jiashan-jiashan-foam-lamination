using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using Caliburn.Micro;
using QA.Business;
using QA.Business.Component.Camera;
using QA.Business.Component.Clean;
using QA.Business.Component.HIVE;
using QA.Business.Component.LaserHeightSensor;
using QA.Business.Component.MES;
using QA.Business.Component.Monitor;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Motion.Robot9075;
using QA.Business.Component.OtherSetting;
using QA.Business.Component.PDCA;
using QA.Business.Component.PLC;
using QA.Business.Component.Scanner;
using QA.Business.Component.ScrewDriver;
using QA.Business.Component.Temp378;
using QA.Business.Helper;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.RunTimeInfo;
using QA.Business.Station;
using QA.Business.Steps;
using QA.IntelligentEquipment.ViewModels;

namespace QA.IntelligentEquipment
{
    //xcopy $(TargetPath) $(SolutionDir)Shell\$(OutDir)Plugins /y
    //xcopy $(TargetDir)$(TargetName).pdb $(SolutionDir)\Shell\$(OutDir) Plugins /y
    public class MyBootstrapper : BootstrapperBase
    {
        private const string ConfigFileName = @"Config\Config.ini";
        private List<Assembly> _priorityAssemblies;
        public static CompositionContainer _compositionContainer { get; private set; }
        public static SimpleContainer _simpleContainer { get; private set; }
        public MyBootstrapper()
        {
            Initialize();
        }
        internal IList<Assembly> PriorityAssemblies
        {
            get { return _priorityAssemblies; }
        }
        public static string ConfigPath
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, ConfigFileName);
            }
        }
        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
            base.OnStartup(sender, e);
        }
        protected virtual void PreInitialize()
        {
            var code = "zh-cn";

            if (!string.IsNullOrWhiteSpace(code))
            {
                var culture = CultureInfo.GetCultureInfo(code);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }
        private void SimpleConfig()
        {
            _simpleContainer = new SimpleContainer();
            _simpleContainer.Instance(_simpleContainer);

            _simpleContainer.Singleton<ParamManager>();
            _simpleContainer.Singleton<CacheParamManager>();
            _simpleContainer.Singleton<TaskManager>();
            _simpleContainer.Singleton<RecipeManager>();
            _simpleContainer.Singleton<StatusManager>();
            _simpleContainer.Singleton<MyUserManager>();
            _simpleContainer.Singleton<GlobalVariable>();

            _simpleContainer.Singleton<RuntimeLogs>();
            _simpleContainer.Singleton<StepFactory>();
            _simpleContainer.Singleton<IDapperHelper, DapperHelper>();
            _simpleContainer.Singleton<StepStatus>();
            _simpleContainer.Singleton<IBaseBiz, BaseBiz>();

            _simpleContainer.Singleton<IPLC, PLC_Component>();
            _simpleContainer.Singleton<PLCParam>();
            _simpleContainer.Singleton<IMGoogol, MotionGoogol_Component>();
            _simpleContainer.Singleton<MotionGoogolParam>();
            _simpleContainer.Singleton<IM9075, Motion9075_Component>();
            _simpleContainer.Singleton<Motion9075Param>();
            _simpleContainer.Singleton<IScanner, Scanner_TcpComponent>();
            _simpleContainer.Singleton<ScannerParam>();
            _simpleContainer.Singleton<ICamera, Camera_Component>();
            _simpleContainer.Singleton<CameraParam>();
            _simpleContainer.Singleton<IMES, MES_Component>();
            _simpleContainer.Singleton<MESParam>();
            _simpleContainer.Singleton<IPDCA, PDCA_Component>();
            _simpleContainer.Singleton<PDCAParam>();
            _simpleContainer.Singleton<IHive, Hive_Component>();
            _simpleContainer.Singleton<HiveParam>();
            _simpleContainer.Singleton<IScrewDriver, ScrewDriver_Component>();
            _simpleContainer.Singleton<ScrewDriverParam>();
            _simpleContainer.Singleton<ILaserHeightSensor, LaserHeightSensor_Component>();
            _simpleContainer.Singleton<LaserHeightSensorParam>();
            _simpleContainer.Singleton<IClean, Clean_Component>();
            _simpleContainer.Singleton<CleanParam>();
            _simpleContainer.Singleton<ITemp378, Temp378_Component>();
            _simpleContainer.Singleton<Temp378Param>();
            _simpleContainer.Singleton<IMonitor, Monitor_SerialComponent>();
            _simpleContainer.Singleton<MonitorParam>();
            _simpleContainer.Singleton<IOtherSetting, OtherSetting_Component>();
            _simpleContainer.Singleton<OtherSettingParam>();
        }
        private void MEFConfig()
        {
            // Add all assemblies to AssemblySource (using a temporary DirectoryCatalog).
            //var directoryCatalog = new DirectoryCatalog(@".//Plugins");//@".//Plugins
            //AssemblySource.Instance.AddRange(
            //    directoryCatalog.Parts
            //        .Select(part => ReflectionModelServices.GetPartType(part).Value.Assembly)
            //        .Where(assembly => !AssemblySource.Instance.Contains(assembly)));//解决VIew在不同程序集无法找到VIew问题

            //直接找View Dll 注意打开执行文件缺少QA.Pages.dll 软件打不开也不报错；
            var envirmentPath = System.IO.Directory.GetCurrentDirectory();
            string pagepath = Path.Combine(envirmentPath, @"QA.Pages.dll");
            string pagepath2 = Path.Combine(envirmentPath, @"QA.UserControls.dll");
            string pagepath3 = Path.Combine(envirmentPath, @"QA.SpotCheckPages.dll");

            AssemblySource.Instance.Add(Assembly.LoadFile(pagepath));
            AssemblySource.Instance.Add(Assembly.LoadFile(pagepath2));
            AssemblySource.Instance.Add(Assembly.LoadFile(pagepath3));

            // Prioritise the executable assembly. This allows the client project to override exports, including IShell.
            // The client project can override SelectAssemblies to choose which assemblies are prioritised.
            _priorityAssemblies = SelectAssemblies().ToList();
            var priorityCatalog = new AggregateCatalog(_priorityAssemblies.Select(x => new AssemblyCatalog(x)));
            var priorityProvider = new CatalogExportProvider(priorityCatalog);

            // Now get all other assemblies (excluding the priority assemblies).
            var mainCatalog = new AggregateCatalog(
                AssemblySource.Instance
                    .Where(assembly => !_priorityAssemblies.Contains(assembly))
                    .Select(x => new AssemblyCatalog(x)));
            var mainProvider = new CatalogExportProvider(mainCatalog);

            _compositionContainer = new CompositionContainer(priorityProvider, mainProvider);
            priorityProvider.SourceProvider = _compositionContainer;
            mainProvider.SourceProvider = _compositionContainer;

            var batch = new CompositionBatch();

            BindServices(batch);
            batch.AddExportedValue(mainCatalog);

            _compositionContainer.Compose(batch);

        }
        protected virtual void BindServices(CompositionBatch batch)
        {
            batch.AddExportedValue<IWindowManager>(new WindowManager());
            batch.AddExportedValue<IEventAggregator>(new EventAggregator());
            batch.AddExportedValue(_compositionContainer);
            batch.AddExportedValue(this);
        }
        protected override void Configure()
        {
            SimpleConfig();
            MEFConfig();
            base.Configure();
        }
        public void ComposeParts(object obj)
        {
            _compositionContainer.ComposeParts(obj);
        }
        private static object obj = new object();
        //从IOC容器中获取对象的方法
        protected override object GetInstance(Type serviceType, string key)
        {
            lock (obj)
            {
                try
                {
                    string contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
                    var exports = _compositionContainer.GetExportedValues<object>(contract);
                    if (exports.Any())
                        return exports.First();
                    else
                        return _simpleContainer.GetInstance(serviceType, key);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("从IOC容器中获取对象失败" + ex.ToString(), "加载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
        }
        //从容器获取所有同类型对象和注入容器的方法
        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            try
            {
                var exports = _compositionContainer.GetExportedValues<object>(AttributedModelServices.GetContractName(serviceType));
                if (exports.Any())
                    return exports;
                else
                    return _simpleContainer.GetAllInstances(serviceType);
            }
            catch (Exception ex)
            {
                MessageBox.Show("从容器获取所有同类型对象和注入容器失败" + ex.ToString(), "加载失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        protected override void BuildUp(object instance)
        {
            _compositionContainer.SatisfyImportsOnce(instance);
            _simpleContainer.BuildUp(instance);
        }
        protected override void OnExit(object sender, EventArgs e)
        {
            base.OnExit(sender, e);
            this.Application.Shutdown();
        }
        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return new[] { Assembly.GetEntryAssembly() };
        }

        //protected override void StartRuntime()
        //{
        //    base.StartRuntime();
        //}
        //protected override IEnumerable<Assembly> SelectAssemblies()
        //{
        //    return new[] {
        //                    Assembly.GetExecutingAssembly()
        //                 };//返回目前正在執行程序集，当View在目前正在执行的程序集中时，可以这样写。
        //}
        //protected override IEnumerable<Assembly> SelectAssemblies()
        //{
        //    List<Assembly> lst = new List<Assembly>();
        //    lst.AddRange(base.SelectAssemblies());
        //    lst.AddRange(
        //        Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\views")
        //            .Where(file => file.EndsWith("dll", true, CultureInfo.CurrentCulture) || file.EndsWith("exe", true, CultureInfo.CurrentCulture))
        //            .Select(Assembly.LoadFrom));
        //    //lst.AddRange(from file in Directory.GetFiles(Environment.CurrentDirectory + @"\views") where file.EndsWith("dll") || file.EndsWith("exe") select Assembly.LoadFrom(file));
        //    return lst;
        //}
    }
}
