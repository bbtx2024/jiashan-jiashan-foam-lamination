using System.Collections.Generic;
using System.Threading.Tasks;
using QA.Business.Define;
using QA.Business.Interfaces;

namespace QA.Business.Steps
{
    public class Pause_Step : IStepStation2
    {
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.Pause;

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);
            return (false, EN_RunRet.TaskOk, EN_RunStep.Stop);
        }
    }
}
