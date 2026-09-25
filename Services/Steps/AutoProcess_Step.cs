using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using HandyControl.Data;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;

namespace QA.Business.Steps
{
    public class AutoProcess_StepNo3 : IStepStation3
    {
        private PLC_Component _plc_Component;
        private StepStatus _stepStatus;

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoProcess;

        public AutoProcess_StepNo3()
        {
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _stepStatus = IoC.Get<StepStatus>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            if (_plc_Component.IsNotNeedProcessMode())
            {
                await Task.Delay(500);
                return (false, EN_RunRet.TaskOk, RunStep);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.PressurizeStep);
        }
    }
    public class AutoProcess_Step : IStepStation2
    {
        private PLC_Component _plc_Component;
        private StepStatus _stepStatus;

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoProcess;

        public AutoProcess_Step()
        {
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _stepStatus = IoC.Get<StepStatus>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            if (_plc_Component.IsNotNeedProcessMode())
            {
                await Task.Delay(500);
                return (false, EN_RunRet.TaskOk, RunStep);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.PickMaterialFromFeederStep);
        }
    }
    public class AutoProcess_StepNo1 : IStepStation1
    {
        private PLC_Component _plc_Component;

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoProcess;

        public AutoProcess_StepNo1()
        {
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            if (_plc_Component.IsNotNeedProcessMode())
            {
                await Task.Delay(500);
                return (false, EN_RunRet.TaskOk, RunStep);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.ScannerCarrierSnStep);
        }
    }
}
