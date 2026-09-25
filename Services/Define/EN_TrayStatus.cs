namespace QA.Business.Define
{
    /// <summary>
    /// 载具上每个穴位状态
    /// </summary>
    public enum EN_TrayStatus
    {
        Empty,
        OK,
        HaveTape,
        MarkFail,//后续错误类型仅占位，未使用
        TapeNotExist2,
        TapeLxzNotExist,
        LxzNotRemoved1,
        LxzNotRemoved2,
        XYOverRange1,
        XYOverRange2,
        TPTapeNotExist,

        Unuse,
        Common,
        NeedWork,
        CameraNoLink,
        TakePhotoOK
    }
}
