using System;

namespace QA.Business.Interfaces
{
    public interface IMonitor : IComponent
    {
        event Action<string, float[]> ReadValueRefresh;
        event Action<string, string[]> ReadStrValueRefresh;
        bool GetMonitorValue(out float[] floatVals, out string[] stringVals);
    }
}
