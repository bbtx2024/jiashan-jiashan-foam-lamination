using QA.Business.Component.Motion.Googol;
using QA.Business.Component.Motion.Robot9075;
using QA.Business.Interfaces;

namespace QA.Business.Component.Motion
{
    public enum En_Motion
    {
        Motion_9075,
        Motion_Google,
    }
    public class MotionFactory_Component
    {
        public MotionFactory_Component()
        {

        }

        IMotion InitMotion(En_Motion motion)
        {
            IMotion _imotion = null;

            switch (motion)
            {
                case En_Motion.Motion_9075:
                    _imotion = new Motion9075_Component();
                    break;
                case En_Motion.Motion_Google:
                    _imotion = new MotionGoogol_Component();
                    break;
            }
            return _imotion;
        }
    }
}
