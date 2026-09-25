using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Component.PDCA;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;

namespace QA.Business.Steps
{
    public class AutoPDCAUpLoad_Step : IStepStation2
    {
        #region Field
        private StepStatus _stepStatus;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        #endregion
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.AutoPDCAUpLoad;

        public AutoPDCAUpLoad_Step()
        {
            _stepStatus = IoC.Get<StepStatus>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
        }

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            var pdca = components.FirstOrDefault(s => s is PDCA_Component) as PDCA_Component;

            return (false, EN_RunRet.TaskOk, EN_RunStep.AutoEnd);
        }
    }
}
