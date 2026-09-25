using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Business.Steps
{
    public class Err_StepNo3 : IStepStation3
    {
        #region Field
        StepStatus _stepStatus;
        private ComponentManager _componentManager;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Err;
        #endregion

        #region Constructor
        public Err_StepNo3()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);
            //var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;
            //plc.SetAlarm(En_Plc_MasterError.Error);
            if (_stepStatus.NextStep3 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
        }
    }
    public class Err_Step : IStepStation2
    {
        #region Field
        StepStatus _stepStatus;
        private ComponentManager _componentManager;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Err;
        #endregion

        #region Constructor
        public Err_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);
            //var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;
            //plc.SetAlarm(En_Plc_MasterError.Error);
            if (_stepStatus.NextStep2 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
        }
    }
    public class Err_StepNo1 : IStepStation1
    {
        #region Field
        StepStatus _stepStatus;
        private ComponentManager _componentManager;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Err;
        #endregion

        #region Constructor
        public Err_StepNo1()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);

            if (_stepStatus.NextStep1 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            }
            return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
        }
    }
}
