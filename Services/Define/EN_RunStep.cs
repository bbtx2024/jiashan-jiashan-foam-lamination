namespace QA.Business.Define
{
    public enum EN_RunStep
    {
        Reset,
        Idle,
        Err,
        Pause,
        Stop,
        AutoStart,
        AutoRunIdle,
        AutoVision,
        NormalVision,
        AutoSolder,
        NormalSolder,
        AutoPDCAUpLoad,
        AutoEnd,
        AutoProcess,//开始加工
        AutoClearBeforeSolder,//焊接前清洗
        AutoReCheckAfterSolder,//焊接后复检
        NormalRecheck,
        AutoRecheck,//复检
        AutoLaserHeight,
        NormalLaserHeight,
        ScannerCarrierSnStep,
        UpCameraIdentifyProductStep,
        PickMaterialFromFeederStep,
        DownCameraIdentifyMaterialStep,
        PlaceMaterialToCarrierStep,
        PressurizeStep,



        TestStep1,
        TestStep2,
        TestStep3,
    }
}
