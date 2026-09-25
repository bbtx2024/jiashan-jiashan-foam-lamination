using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("QA.IntelligentEquipment")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("QA.IntelligentEquipment")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//将 ComVisible 设置为 false 将使此程序集中的类型
//对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

//若要开始生成可本地化的应用程序，请
//<PropertyGroup> 中的 .csproj 文件中
//例如，如果您在源文件中使用的是美国英语，
//使用的是美国英语，请将 <UICulture> 设置为 en-US。  然后取消
//对以下 NeutralResourceLanguage 特性的注释。  更新
//以下行中的“en-US”以匹配项目文件中的 UICulture 设置。

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //主题特定资源词典所处位置
                                     //(当资源未在页面
                                     //或应用程序资源字典中找到时使用)
    ResourceDictionaryLocation.SourceAssembly //常规资源词典所处位置
                                              //(当资源未在页面
                                              //、应用程序或任何主题专用资源字典中找到时使用)
)]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
//可以指定所有这些值，也可以使用“生成号”和“修订号”的默认值，
// 方法是按如下所示使用“*”: :
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.1.*")]
[assembly: AssemblyFileVersion("1.0.1")]

#region [QY 2024-04-02 V1.0.1]
/*
 * 1.移动output_time_CH至data内，规范部分上传项名称
 */
#endregion

#region [QY 2024-03-27 V1.0.0]
/*
 * 1.上传errordata时，修改hive_state强制传5，并增加previous_state强制传1
 */
#endregion
