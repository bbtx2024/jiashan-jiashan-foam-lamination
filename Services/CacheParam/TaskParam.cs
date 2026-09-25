/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-04-01
 * 说明：（制程数据存储）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("标定参数设置")]
    public class TaskParam
    {
        [DisplayName("左制程名称")]
        public string LeftTaskName { get; set; } = "";
        [DisplayName("右制程名称")]
        public string RightTaskName { get; set; } = "";
    }
}
