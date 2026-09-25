using System;
using System.Collections.Generic;
using System.Reflection;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;

namespace QA.Business.Steps
{
    public class StepFactory
    {
        private IDapperHelper _dataHelper;
        private Dictionary<EN_RunStep, IStepStation4> _stepHandlerDictionaryNo4 = new Dictionary<EN_RunStep, IStepStation4>();
        private Dictionary<EN_RunStep, IStepStation3> _stepHandlerDictionaryNo3 = new Dictionary<EN_RunStep, IStepStation3>();
        private Dictionary<EN_RunStep, IStepStation2> _stepHandlerDictionary = new Dictionary<EN_RunStep, IStepStation2>();
        private Dictionary<EN_RunStep, IStepStation1> _stepHandlerDictionaryNo1 = new Dictionary<EN_RunStep, IStepStation1>();

        public StepFactory()
        {
            _dataHelper = IoC.Get<IDapperHelper>();

            var name = Assembly.GetExecutingAssembly().ToString();
            var stepTypeListNo4 = _dataHelper?.GenerateManager<IStepStation4>(name);
            var stepTypeListNo3 = _dataHelper?.GenerateManager<IStepStation3>(name);
            var stepTypeList = _dataHelper?.GenerateManager<IStepStation2>(name);
            var stepTypeListNo1 = _dataHelper?.GenerateManager<IStepStation1>(name);
            //如果下面循环堆栈越界，可能是循环调用BaseBiz
            foreach (var stepType in stepTypeListNo4)
            {
                if (Activator.CreateInstance(stepType) is IStepStation4 instance)
                    _stepHandlerDictionaryNo4.Add(instance.RunStep, instance);
            }
            foreach (var stepType in stepTypeListNo3)
            {
                if (Activator.CreateInstance(stepType) is IStepStation3 instance)
                    _stepHandlerDictionaryNo3.Add(instance.RunStep, instance);
            }
            foreach (var stepType in stepTypeList)
            {
                if (Activator.CreateInstance(stepType) is IStepStation2 instance)
                    _stepHandlerDictionary.Add(instance.RunStep, instance);
            }
            foreach (var stepTypeNo1 in stepTypeListNo1)
            {
                if (Activator.CreateInstance(stepTypeNo1) is IStepStation1 instance1)
                    _stepHandlerDictionaryNo1.Add(instance1.RunStep, instance1);
            }
        }
        public Dictionary<EN_RunStep, IStepStation4> GetStepHandlerDic4()
        {
            return _stepHandlerDictionaryNo4;
        }
        public Dictionary<EN_RunStep, IStepStation3> GetStepHandlerDic3()
        {
            return _stepHandlerDictionaryNo3;
        }
        public Dictionary<EN_RunStep, IStepStation2> GetStepHandlerDic2()
        {
            return _stepHandlerDictionary;
        }
        public Dictionary<EN_RunStep, IStepStation1> GetStepHandlerDic1()
        {
            return _stepHandlerDictionaryNo1;
        }
    }
}
