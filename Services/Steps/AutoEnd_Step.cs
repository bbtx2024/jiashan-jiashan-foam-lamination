using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Business.Steps
{
    public class AutoEnd_Step : IStepStation2
    {
        #region
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        #endregion
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoEnd;

        public AutoEnd_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            var motion = components.FirstOrDefault(s => s is MotionGoogol_Component) as MotionGoogol_Component;
            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            if (!plc.SetPcMachineStatus(PcToPlcMachineStatus.工作完成))
            {
                return (false, EN_RunRet.PLCError, EN_RunStep.Err);
            }
            await Task.Delay(100);
            if (!plc.SetPcMachineStatus(PcToPlcMachineStatus.空闲中))
            {
                return (false, EN_RunRet.PLCError, EN_RunStep.Err);
            }

            if (_stepStatus.NextStep2 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoRunIdle);
        }
    }
}
