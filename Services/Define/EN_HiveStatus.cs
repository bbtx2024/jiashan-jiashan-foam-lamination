namespace QA.Business.Define
{
    /// <summary>
    /// Hive状态
    /// </summary>
    public enum EN_HiveStatus
    {
        /// <summary>
        /// 生产需要上传MES的物料，且当前时间距机台有载具时间小于90s，且没有暂停
        /// </summary>
        Running = 1,
        /// <summary>
        /// 在1、3状态下超过90s无物料（机器需断电时也是从1切为2才能断电 ）；在1、3、4状态下按下暂停/停止按钮
        /// </summary>
        Idle = 2,
        /// <summary>
        /// 如果要设置、切软件，要先手动切换至3。如果90s后未启动，自动切为2；启动则根据90s判定
        /// </summary>
        Engineering = 3,
        /// <summary>
        /// 日常点检；换料、换吸嘴等
        /// </summary>
        PlannedDowntime = 4,
        /// <summary>
        /// 机台报警
        /// </summary>
        Downtime = 5,
    }
}
