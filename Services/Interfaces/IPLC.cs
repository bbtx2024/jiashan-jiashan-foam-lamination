using System;

namespace QA.Business.Interfaces
{
    public interface IPLC : IComponent
    {
        ushort[] ReadUshorts { get; set; }
        //ushort[] WriteUshorts { get; set; }
        event Action<ushort[]> ReadValueRefresh;
        //event Action<ushort[]> WriteValueRefresh;

        //bool GetAddrValue(byte id, ushort addr, ref ushort value);
        //bool GetAddrMutiValue(byte id, ushort addr, ushort num, ref ushort[] valueUshorts);
        //bool GetAddrStringValue(byte id, ushort addr, ushort num, ref string str);
        //bool SetAddrValue(byte id,ushort addr, ushort value);
        //bool SetAddrMultiValue(byte id,ushort addr, ushort[] value);
        //bool SetAddrStringValue(byte id, ushort addr, ushort num, string str);

    }
}
