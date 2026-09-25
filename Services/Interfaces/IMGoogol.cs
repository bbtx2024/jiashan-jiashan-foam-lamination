using System;
using QA.Business.Message;

namespace QA.Business.Interfaces
{
    public interface IMGoogol : IComponent
    {
        event Action<RobotRealStateInfo> RobotValueRefresh;
    }
}
