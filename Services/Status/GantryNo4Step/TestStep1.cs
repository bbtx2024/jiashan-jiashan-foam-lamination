using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QA.Business.Interfaces;
using QA.Business.Define;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Steps
{
    public class TestStep1 : IStepStation4
    {
        public EN_RunStep RunStep
        {
            get;
        } = EN_RunStep.TestStep1;

        public async Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components)
        {
            await Task.Delay(0);

            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "测试工位4，步骤一执行", En_Logout_Type.Run, true);

            return(true, EN_RunRet.TaskOk, EN_RunStep.TestStep2);
        }
    }
}
