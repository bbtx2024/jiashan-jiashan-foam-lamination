using QA.Business.Component.Camera;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PDCA;
using QA.Business.Component.PLC;
using QA.Business.Component.Scanner;

namespace QA.Business.Manager
{
    public class ComponentManager
    {
        public PLC_Component Plc { get; private set; } = new PLC_Component();
        public Scanner_TcpComponent Scanner { get; set; } = new Scanner_TcpComponent();
        public MotionGoogol_Component MotionGoogol { get; set; } = new MotionGoogol_Component();
        public Camera_Component Camera { get; set; } = new Camera_Component();
        public PDCA_Component PDCA { get; set; } = new PDCA_Component();
        public MES_Component MES { get; set; } = new MES_Component();

    }
}
