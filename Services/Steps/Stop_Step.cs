using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Steps
{
    public class Stop_Step : IStepStation2
    {
        private StepStatus _stepStatus;
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Stop;

        public Stop_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"二站进入Stop步骤", En_Logout_Type.Run, true);
            //while (_stepStatus.NextStep2 == RunStep)
            //{
            //    await Task.Delay(200);
            //}
            //var motion = IoC.Get<IMGoogol>() as MotionGoogol_Component;
            //motion.SetExitSts(false);
            //return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        }
    }
    public class Stop_StepNo1 : IStepStation1
    {
        private StepStatus _stepStatus;
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Stop;
        public Stop_StepNo1()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"一站进入Stop步骤", En_Logout_Type.Run, true);
            //while (_stepStatus.NextStep1 == RunStep)
            //{
            //    await Task.Delay(200);
            //}
            //var motion = IoC.Get<IMGoogol>() as MotionGoogol_Component;
            //motion.SetExitSts(false);
            //return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        }
    }
    public class Stop_StepNo3 : IStepStation3
    {
        private StepStatus _stepStatus;
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Stop;
        public Stop_StepNo3()
        {
            _stepStatus = IoC.Get<StepStatus>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"三站进入Stop步骤", En_Logout_Type.Run, true);
            //while (_stepStatus.NextStep1 == RunStep)
            //{
            //    await Task.Delay(200);
            //}
            //var motion = IoC.Get<IMGoogol>() as MotionGoogol_Component;
            //motion.SetExitSts(false);
            //return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            return (false, EN_RunRet.MotionErr, EN_RunStep.Err);
        }
    }
}
