using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Business.Steps
{
    public class AutoRecheck_Step : IStepStation2
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        #endregion

        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoRecheck;

        public AutoRecheck_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            //if (_paramManager.PDCAParam.BUse)
            //{
            //    return (true, EN_RunRet.HeightOK, EN_RunStep.AutoPDCAUpLoad);
            //}
            //else
            //{
            return (true, EN_RunRet.HeightOK, EN_RunStep.AutoEnd);
            //}
        }
    }
}
