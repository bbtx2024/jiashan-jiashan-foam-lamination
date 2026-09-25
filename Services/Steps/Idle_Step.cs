using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Business.Steps
{
    public class Idle_StepNo3 : IStepStation3
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        private IEventAggregator _eventAggregator;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Idle;
        #endregion

        public Idle_StepNo3()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);
            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            //if (plc.GetPlcStartStatus())
            //{
            //    plc.SetPlcStartStatus(false);
            //    //return (false, EN_RunRet.TaskOk, EN_RunStep.AutoStart);
            //}

            if (_stepStatus.NextStep3 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep3);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.Idle);
        }
    }
    public class Idle_Step : IStepStation2
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        private IEventAggregator _eventAggregator;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Idle;
        #endregion

        public Idle_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);
            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            //if (plc.GetPlcStartStatus())
            //{
            //    plc.SetPlcStartStatus(false);
            //    //return (false, EN_RunRet.TaskOk, EN_RunStep.AutoStart);
            //}

            if (_stepStatus.NextStep2 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep2);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.Idle);
        }
    }
    public class Idle_StepNo1 : IStepStation1
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        #endregion

        #region Property
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Idle;
        #endregion

        public Idle_StepNo1()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }
        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(10);
            var plc = components.FirstOrDefault(s => s is PLC_Component) as PLC_Component;

            if (_stepStatus.NextStep1 != RunStep)
            {
                return (false, EN_RunRet.TaskOk, _stepStatus.NextStep1);
            }

            return (false, EN_RunRet.TaskOk, EN_RunStep.Idle);
        }
    }
}
