using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Steps
{
    public class AutoRunIdle_StepNo3 : IStepStation3
    {
        #region Field
        private StepStatus _stepStatus;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoRunIdle;
        #endregion

        #region Constructor
        public AutoRunIdle_StepNo3()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion


        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            if (plc.IsSafeDoorAlarm())
            {
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }
            if (!plc.IsFeederLocked())
            {
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }

            if (_stepStatus.NextStep3 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoProcess);
        }
    }
    public class AutoRunIdle_Step : IStepStation2
    {
        #region Field
        private StepStatus _stepStatus;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoRunIdle;
        #endregion

        #region Constructor
        public AutoRunIdle_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            if (plc.IsSafeDoorAlarm())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AutoRun->检测到安全门报警", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }
            if (!plc.IsFeederLocked())
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "AutoRun->检测到飞达未到位报警", En_Logout_Type.Alarm, true);
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }

            if (_stepStatus.NextStep2 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoProcess);
        }
    }
    public class AutoRunIdle_StepNo1 : IStepStation1
    {
        #region Field
        private StepStatus _stepStatus;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoRunIdle;
        #endregion

        #region Constructor
        public AutoRunIdle_StepNo1()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        #endregion


        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            if (plc.IsSafeDoorAlarm())
            {
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }
            if (!plc.IsFeederLocked())
            {
                return (false, EN_RunRet.TaskOk, EN_RunStep.Err);
            }

            if (_stepStatus.NextStep1 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoProcess);
        }
    }
}
