using System;
using System.Collections.Generic;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class VisionTemplateParam
    {
        /// <summary>
        /// 所有视觉模板名称
        /// </summary>
        public List<string> TemplateNames { get; set; } = new List<string>();

    }
}
