using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using HandyControl.Controls;
using QA.Business.Procedure;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    public class TaskManager : PropertyChangedBase
    {
        //public string TaskDir = ConfigurationManager.AppSettings["taskPath"].Replace("{BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
        
        public string TaskDir = @"D:\QKProject\Setting\Config\Task";
        public string FileSuffix { private set; get; } = @".wrk";

        public List<LaserSprayProcedure> Tasks { get; set; } = new List<LaserSprayProcedure>();

        public TaskManager()
        {
            if (!Directory.Exists(TaskDir))
            {
                try
                {
                    Directory.CreateDirectory(TaskDir);
                }
                catch (Exception e)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, "未能创建制程文件夹！\n" + e.ToString());
                    return;
                }
            }
            if (!LoadTasks())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未能加载全部制程！", En_Logout_Type.Default, true);
            }
        }

        /// <summary>
        /// 加载制程文件夹内的所有制程。如果名称不符，则修改制程文件内的文件名。
        /// </summary>
        /// <returns></returns>
        public bool LoadTasks()
        {
            Tasks.Clear();
            try
            {
                var taskFiles = Directory.GetFiles(TaskDir).Where(t => new FileInfo(t).Extension.Equals(FileSuffix));
                foreach (var taskFile in taskFiles)
                {
                    var task = SerializerByJson.LoadFromJson(taskFile, typeof(LaserSprayProcedure)) as LaserSprayProcedure;
                    if (task != null)
                    {
                        string taskFileName = new FileInfo(taskFile).Name.Replace(FileSuffix, "");
                        //如果文件名与文件内制程名不一致，以文件名为准
                        if (task.ProcedureName != taskFileName)
                        {
                            task.ProcedureName = taskFileName;
                            SaveTask(null, task, out string info);
                        }
                        Tasks.Add(task);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载制程文件异常！\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 删除指定制程。可用于删除制程，以及保存制程前的删除原本制程。
        /// </summary>
        /// <param name="task"></param>
        public void DeleteTask(LaserSprayProcedure task)
        {
            Tasks.Remove(task);
            if (File.Exists($"{TaskDir}\\{task.ProcedureName}{FileSuffix}"))
            {
                File.Delete($"{TaskDir}\\{task.ProcedureName}{FileSuffix}");
            }
        }

        /// <summary>
        /// 保存指定制程。该制程必须与现有制程不同名。
        /// </summary>
        /// <param name="originProcedure"></param>
        /// <param name="currentProcedure"></param>
        /// <returns></returns>
        public bool SaveTask(LaserSprayProcedure originProcedure, LaserSprayProcedure currentProcedure, out string resultInfo)
        {
            //移除旧制程
            if (originProcedure != null)
            {
                Tasks.Remove(originProcedure);
                if (File.Exists($"{TaskDir}\\{originProcedure.ProcedureName}{FileSuffix}"))
                {
                    File.Delete($"{TaskDir}\\{originProcedure.ProcedureName}{FileSuffix}");
                }
            }
            //检测是否有同名制程
            foreach (LaserSprayProcedure task in Tasks)
            {
                if (task.ProcedureName == currentProcedure.ProcedureName)
                {
                    if (originProcedure != null)
                    {
                        Tasks.Add(originProcedure);
                    }
                    resultInfo = "该制程与现有制程同名，无法保存！";
                    return false;
                }
            }
            string s = $"新建制程：{currentProcedure.ProcedureName}";
            if (originProcedure != null)
            {
                //记录变化项，默认制程有12个穴位，每个穴位有四个点位
                StringBuilder sb;
                if (originProcedure.ProcedureName == currentProcedure.ProcedureName)
                {
                    sb = new StringBuilder($"修改制程：{originProcedure.ProcedureName}\n");
                }
                else
                {
                    sb = new StringBuilder($"修改制程：{originProcedure.ProcedureName}->{currentProcedure.ProcedureName}\n");
                }
                for (int cavity = 1; cavity <= 12; cavity++)
                {
                    var originVisionPoint = originProcedure.GetVisionPointByCavityNum(cavity);
                    var currentVisionPoint = currentProcedure.GetVisionPointByCavityNum(cavity);
                    if (originVisionPoint == null || currentVisionPoint == null)
                    {
                        continue;
                    }
                    if (originVisionPoint.IsUsed != currentVisionPoint.IsUsed)
                    {
                        sb.Append($"{cavity}穴启用：{originVisionPoint.IsUsed}->{currentVisionPoint.IsUsed}\n");
                    }
                    if (originVisionPoint.VisionPointX != currentVisionPoint.VisionPointX)
                    {
                        sb.Append($"{cavity}穴拍照X：{originVisionPoint.VisionPointX.ToString("F3")}->{currentVisionPoint.VisionPointX.ToString("F3")}\n");
                    }
                    if (originVisionPoint.VisionPointY != currentVisionPoint.VisionPointY)
                    {
                        sb.Append($"{cavity}穴拍照Y：{originVisionPoint.VisionPointY.ToString("F3")}->{currentVisionPoint.VisionPointY.ToString("F3")}\n");
                    }
                    if (originVisionPoint.VisionPointXOffset != currentVisionPoint.VisionPointXOffset)
                    {
                        sb.Append($"{cavity}穴拍照X贴合补偿：{originVisionPoint.VisionPointXOffset.ToString("F3")}->{currentVisionPoint.VisionPointXOffset.ToString("F3")}\n");
                    }
                    if (originVisionPoint.VisionPointYOffset != currentVisionPoint.VisionPointYOffset)
                    {
                        sb.Append($"{cavity}穴拍照Y贴合补偿：{originVisionPoint.VisionPointYOffset.ToString("F3")}->{currentVisionPoint.VisionPointYOffset.ToString("F3")}\n");
                    }
                    for (int nozzleNo = 1; nozzleNo <= 4; nozzleNo++)
                    {
                        var originSolderPoint = originVisionPoint.GetSolderPointByNozzleNo(nozzleNo);
                        var currentSolderPoint = currentVisionPoint.GetSolderPointByNozzleNo(nozzleNo);
                        if (originSolderPoint == null || currentSolderPoint == null)
                        {
                            continue;
                        }
                        if (originSolderPoint.IsUsed != currentSolderPoint.IsUsed)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴启用：{originSolderPoint.IsUsed}->{currentSolderPoint.IsUsed}\n");
                        }
                        if (originSolderPoint.SolderRecipeName != currentSolderPoint.SolderRecipeName)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合配方：{originSolderPoint.SolderRecipeName}->{currentSolderPoint.SolderRecipeName}\n");
                        }
                        if (originSolderPoint.SolderPointX != currentSolderPoint.SolderPointX)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合X：{originSolderPoint.SolderPointX.ToString("F3")}->{currentSolderPoint.SolderPointX.ToString("F3")}\n");
                        }
                        if (originSolderPoint.SolderPointY != currentSolderPoint.SolderPointY)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合Y：{originSolderPoint.SolderPointY.ToString("F3")}->{currentSolderPoint.SolderPointY.ToString("F3")}\n");
                        }
                        if (originSolderPoint.SolderPointZ != currentSolderPoint.SolderPointZ)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合Z：{originSolderPoint.SolderPointZ.ToString("F3")}->{currentSolderPoint.SolderPointZ.ToString("F3")}\n");
                        }
                        if (originSolderPoint.SolderPointXOffset != currentSolderPoint.SolderPointXOffset)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合X贴合补偿：{originSolderPoint.SolderPointXOffset.ToString("F3")}->{currentSolderPoint.SolderPointXOffset.ToString("F3")}\n");
                        }
                        if (originSolderPoint.SolderPointYOffset != currentSolderPoint.SolderPointYOffset)
                        {
                            sb.Append($"{cavity}穴{nozzleNo}号吸嘴贴合Y贴合补偿：{originSolderPoint.SolderPointYOffset.ToString("F3")}->{currentSolderPoint.SolderPointYOffset.ToString("F3")}\n");
                        }
                        //判断单次补偿值不能大于0.1
                        if (Math.Abs(currentSolderPoint.SolderPointXOffset - originSolderPoint.SolderPointXOffset) >= 0.09)
                        {
                            MessageBox.Warning($"{cavity}穴X单次补偿值大于0.1，请重新补偿再保存！", "操作提示");
                        }
                        if (Math.Abs(currentSolderPoint.SolderPointYOffset - originSolderPoint.SolderPointYOffset) >= 0.09)
                        {
                            MessageBox.Warning($"{cavity}穴Y单次补偿值大于0.1，请重新补偿再保存！", "操作提示");
                        }
                    }
                }
                s = sb.ToString();
                if (s.Split('\n').Length <= 2)
                {
                    s += "未发现点位变动！";
                }
                else
                {
                    s = s.Substring(0, s.Length - 1);
                }
            }
            //保存制程
            Tasks.Add(currentProcedure);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, s, En_Logout_Type.SystemParam);
            try
            {
                SerializerByJson.SaveToJson($"{TaskDir}\\{currentProcedure.ProcedureName}{FileSuffix}", currentProcedure);
                resultInfo = s;
                return true;
            }
            catch (Exception e)
            {
                resultInfo = $"保存制程到文件异常！\n{e.ToString()}";
                return false;
            }
        }

        public LaserSprayProcedure GetTaskByName(string name)
        {
            foreach (var task in Tasks)
            {
                if (task.ProcedureName == name)
                {
                    return task;
                }
            }
            return null;
        }
    }
}
