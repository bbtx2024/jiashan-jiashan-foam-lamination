namespace QA.Business.Define
{
    public enum EN_TapeLenKind
    {
        Length27 = 27,
        Length29 = 29,
    }

    public enum EN_Vendor
    {
        MARIAN,
        TRIUMPH,
        LYE,
    }

    /// <summary>
    /// 指示SIP板当前MES状态。
    /// 路由有三种情况，ok，非本站，未找到。
    /// MODEL有三种情况，空（当成AB板处理），AB板（需要处理），CD板（无需处理）。
    /// </summary>
    public enum EN_Routing_Result
    {
        /// <summary>
        /// 需要本站绑定
        /// </summary>
        OK = 1,
        /// <summary>
        /// 本站已经绑定
        /// </summary>
        HasUploaded,
        /// <summary>
        /// 未知工站，不应该由这里处理
        /// </summary>
        NotThisStation,
        /// <summary>
        /// 其他情况，例如没有该SIP板的信息等
        /// </summary>
        Other,
    }

    public enum AUDIT_MODE
    {
        Normal,
        CPK,
        GRR,
        SCS,
    }
}
