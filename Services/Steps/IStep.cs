using System.Collections.Generic;
using System.Threading.Tasks;
using QA.Business.Define;
using QA.Business.Interfaces;

namespace QA.Business.Steps
{
    public interface IStepStation1
    {
        EN_RunStep RunStep { get; }
        Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components);
    }

    public interface IStepStation2
    {
        EN_RunStep RunStep { get; }
        Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components);
    }
    public interface IStepStation3
    {
        EN_RunStep RunStep { get; }
        Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components);
    }
    public interface IStepStation4
    {
        EN_RunStep RunStep { get; }
        Task<(bool, EN_RunRet, EN_RunStep)> Handle<T>(T t, List<IComponent> components);
    }
}
