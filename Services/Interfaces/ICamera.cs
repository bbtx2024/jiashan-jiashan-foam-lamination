namespace QA.Business.Interfaces
{
    public interface ICamera : IComponent
    {
        ////复检(同步处理4#相机)
        //bool ReCheck(CameraNo cameraNo, int pointNo, string productSN, string carrierSN);
        ////多复检(同步处理4#相机)
        //bool MultiRecheck(CameraNo cameraNo, ref List<(int, bool)> checkResulTuples);
        ////料盘单定位(上视觉1#相机)
        //bool FitStationMark(CameraNo cameraNo, int cavityNo, string productSN, string carrierSN, ref (float, float, float) xyrValueTuple);
        ////料盘多定位(同步处理上视觉1#相机)
        //bool FitStationMultiMark(CameraNo cameraNo, int pointNo, string productSN, string carrierSN);
        ////料盘多定位(上视觉1#相机)
        //bool FitStationMultiMark(CameraNo cameraNo, ref List<(bool, int, float, float, float)> xyrValueTuples);
        ////吸料单定位(下视觉2#相机)
        //bool MaterialGrabMark(CameraNo cameraNo, int grabNo, int cavityNo, ref (int, int, float, float, float) xyrValueTuple);
        ////吸料单定位(同步处理下视觉2#相机)
        //bool MaterialGrabMultiMark(CameraNo cameraNo, int grabNo, int cavityNo);
        ////吸料多定位(下视觉2#相机)
        //bool MaterialGrabMultiMark(CameraNo cameraNo, ref List<(bool, int, int, float, float, float)> xyrValueTuples);
        ////贴合料盘检测(3#相机)
        //bool MaterialCheck(CameraNo cameraNo, ref List<(int, int)> xyrValueTuples);
        ////　9点标定(1#相机和2#相机)
        //bool Calibration(CameraNo cameraNo, int point, float x, float y);
    }
}
