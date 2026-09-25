using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    /// <summary>
    /// 连接卡机 -> 获取卡片ID -> 读写信息
    /// </summary>
    public class BC750
    {
        #region DLL引入

        [DllImport("kernel32.dll")]
        private static extern void Sleep(int dwMilliseconds);

        //=========================== System Function =============================
        [DllImport("hfrdapi.dll")]
        private static extern int Sys_GetDeviceNum(UInt16 vid, UInt16 pid, ref UInt32 pNum);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_GetHidSerialNumberStr(UInt32 deviceIndex,
                                                    UInt16 vid,
                                                    UInt16 pid,
                                                    IntPtr deviceString,
                                                    UInt32 deviceStringLength);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_Open(ref IntPtr device,
                                   UInt32 index,
                                   UInt16 vid,
                                   UInt16 pid);

        [DllImport("hfrdapi.dll")]
        private static extern bool Sys_IsOpen(IntPtr device);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_Close(ref IntPtr device);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_SetLight(IntPtr device, byte color);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_SetBuzzer(IntPtr device, byte msec);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_SetAntenna(IntPtr device, byte mode);

        [DllImport("hfrdapi.dll")]
        private static extern int Sys_InitType(IntPtr device, byte type);

        //=========================== M1 Card Function =============================
        [DllImport("hfrdapi.dll")]
        private static extern int TyA_Request(IntPtr device, byte mode, ref UInt16 pTagType);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_Anticollision(IntPtr device,
                                            byte bcnt,
                                            byte[] pSnr,
                                            ref byte pLen);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_Select(IntPtr device,
                                     byte[] pSnr,
                                     byte snrLen,
                                     ref byte pSak);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_Halt(IntPtr device);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Authentication2(IntPtr device,
                                                 byte mode,
                                                 byte block,
                                                 byte[] pKey);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Read(IntPtr device,
                                      byte block,
                                      byte[] pData,
                                      ref byte pLen);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Write(IntPtr device, byte block, byte[] pData);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_InitValue(IntPtr device, byte block, Int32 value);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_ReadValue(IntPtr device, byte block, ref Int32 pValue);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Decrement(IntPtr device, byte block, Int32 value);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Increment(IntPtr device, byte block, Int32 value);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Restore(IntPtr device, byte block);

        [DllImport("hfrdapi.dll")]
        private static extern int TyA_CS_Transfer(IntPtr device, byte block);

        #endregion

        public static IntPtr g_hDevice = (IntPtr)(-1);//必须以-1进行初始化
        private static readonly byte[] bytesKey = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };//初始密钥为12个F
        private static readonly char[] Digit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F' };

        /// <summary>
        /// 返回g_hDevice代表的卡机是否曾经使用Sys_Open连接过
        /// </summary>
        /// <returns></returns>
        public static bool IsOpen()
        {
            return Sys_IsOpen(g_hDevice);
        }

        /// <summary>
        /// 连接卡机。如果未连接，则会自动连接后返回连接结果；如果已连接，将断开连接后重连。
        /// </summary>
        /// <returns></returns>
        public static bool Open()
        {
            uint readerNum = 0;//读卡器个数
            if (Sys_GetDeviceNum(0x416, 0x8020, ref readerNum) != 0)
            {
                return false;
            }
            if (readerNum <= 0)
            {
                return false;
            }
            if (Sys_IsOpen(g_hDevice))
            {
                Sys_Close(ref g_hDevice);
                g_hDevice = (IntPtr)(-1);
            }
            return Sys_Open(ref g_hDevice, 0, 0x0416, 0x8020) == 0
              && Sys_SetAntenna(g_hDevice, 0) == 0
              && Sys_InitType(g_hDevice, (byte)'A') == 0
              && Sys_SetAntenna(g_hDevice, 1) == 0;
        }

        /// <summary>
        /// 使卡机蜂鸣器响一段时间
        /// </summary>
        /// <param name="time10Millis">要响的时间，单位10ms</param>
        public static void TipSuccess(int time10Millis = 15, bool retry = true)
        {
            //至多响一秒
            byte msec = (byte)Math.Min(time10Millis, 100);
            if (!IsOpen() || Sys_SetBuzzer(g_hDevice, msec) != 0)
            {
                if (retry && Open())
                {
                    TipSuccess(time10Millis, false);
                }
            }
        }

        /// <summary>
        /// 返回当前卡片的ID。如果无卡，返回空字符串。
        /// </summary>
        /// <returns></returns>
        public static string GetCardID(bool retry = true)
        {
            ushort TagType = 0;
            byte[] data = new byte[16];
            byte len = (byte)data.Length;
            byte sak = 0;
            //搜寻所有的卡，返回卡的序列号，锁定一张ISO14443-3 TYPE_A 卡
            if (!IsOpen()
                || TyA_Request(g_hDevice, 0x52, ref TagType) != 0
                || TyA_Anticollision(g_hDevice, 0, data, ref len) != 0
                || TyA_Select(g_hDevice, data, len, ref sak) != 0)
            {
                if (retry && Open())
                {
                    return GetCardID(false);
                }
                else
                {
                    return "";
                }
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                //一个byte可以转为两个十六进制字母，因为高位可能有0所以要高低位分别转换
                sb.Append(Digit[(data[i] >> 4) & 0X0F]).Append(Digit[data[i] & 0X0F]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 返回密钥是否正确。读取数据前必须确保密钥正确。
        /// </summary>
        /// <param name="secnr"></param>
        /// <returns></returns>
        private static bool IsSecretKeyValid(int secnr)
        {
            if (TyA_CS_Authentication2(g_hDevice, 0x60, (byte)(secnr * 4), bytesKey) == 0)
            {
                return true;
            }
            return GetCardID() != "" && TyA_CS_Authentication2(g_hDevice, 0x60, (byte)(secnr * 4), bytesKey) == 0;
        }

        /// <summary>
        /// 从扇区读取数据，返回长度为16的byte数组
        /// </summary>
        /// <param name="secnr">扇区索引，只能是2-15</param>
        /// <returns></returns>
        public static byte[] ReadSectionAsBytes(int secnr)
        {
            byte[] ret = new byte[16];
            if (secnr < 2 || secnr > 15 || !IsSecretKeyValid(secnr))
            {
                return ret;
            }
            byte[] data = new byte[16];
            byte len = 0;
            if (TyA_CS_Read(g_hDevice, (byte)(secnr * 4), data, ref len) != 0)
            {
                return ret;
            }
            ret = new byte[len];
            for (int i = 0; i < len; i++)
            {
                ret[i] = data[i];
            }
            return ret;
        }

        /// <summary>
        /// 从扇区读取数据，返回byte数组转换得到的字符串
        /// </summary>
        /// <param name="secnr">扇区索引，只能是2-15</param>
        /// <returns></returns>
        public static string ReadSectionAsString(int secnr)
        {
            return Encoding.UTF8.GetString(ReadSectionAsBytes(secnr)).Trim('\0');
        }

        /// <summary>
        /// 向扇区写入数据，注意数据至多为16个byte，超出部分不会写入
        /// </summary>
        /// <param name="secnr"></param>
        /// <param name="info"></param>
        public static void WriteBytesToSection(int secnr, byte[] data)
        {
            if (!IsSecretKeyValid(secnr))
            {
                return;
            }
            if (data.Length > 16)
            {
                byte[] data2 = new byte[16];
                for (int i = 0; i < data2.Length; i++)
                {
                    data2[i] = data[i];
                }
                data = data2;
            }
            TyA_CS_Write(g_hDevice, (byte)(secnr * 4), data);
        }

        /// <summary>
        /// 向扇区写入数据，注意数据至多为16个字母或16个数字或8个汉字，超出部分不会写入
        /// </summary>
        /// <param name="secnr"></param>
        /// <param name="info"></param>
        public static void WriteStringToSection(int secnr, string info)
        {
            WriteBytesToSection(secnr, Encoding.UTF8.GetBytes(info.Trim()));
        }

        public static CardInfo currCardInfo = new CardInfo();

        private static bool runFlag = false;

        public static async void Start()
        {
            await Task.Factory.StartNew(async () =>
            {
                runFlag = true;
                while (runFlag)
                {
                    try
                    {
                        await Task.Delay(IsOpen() ? 200 : 1000);
                        string cardID = GetCardID();
                        if (string.IsNullOrEmpty(cardID))
                        {
                            currCardInfo.havingCard = false;
                            continue;
                        }
                        string name = ReadSectionAsString(2);
                        string str3 = ReadSectionAsString(3);
                        string str4 = ReadSectionAsString(4);
                        byte[] sec5 = ReadSectionAsBytes(5);
                        byte userTypeByte = sec5[0];
                        if (userTypeByte != (byte)MyUserManager.userTypeOperator
                        && userTypeByte != (byte)MyUserManager.userTypeManager
                        && userTypeByte != (byte)MyUserManager.userTypeEngineer)
                        {
                            sec5[0] = (byte)MyUserManager.userTypeOperator;
                        }
                        //二次读取，防止没读完卡片信息就拿走了
                        cardID = GetCardID();
                        if (string.IsNullOrEmpty(cardID))
                        {
                            currCardInfo.havingCard = false;
                            continue;
                        }
                        if (!currCardInfo.havingCard)
                        {
                            currCardInfo.havingCard = true;
                            TipSuccess();
                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"{name}已刷卡，权限{MyUserManager.TypeToString((EN_UserType)sec5[0])}", En_Logout_Type.SystemParam, true);
                            //将信息存入currCardInfo
                            currCardInfo.cardID = cardID;
                            currCardInfo.name = name;
                            currCardInfo.str3 = str3;
                            currCardInfo.str4 = str4;
                            currCardInfo.userType = (EN_UserType)sec5[0];
                        }
                    }
                    catch (Exception ex)
                    {
                        //dll可能不匹配，是32位的
                    }
                }
            });
        }

        public static void Stop()
        {
            runFlag = false;
        }
    }

    public class CardInfo
    {
        public bool havingCard;
        public string cardID;//section0，只读

        public string name;//section2,str
        public string str3;//section3,str
        public string str4;//section4,str
        public EN_UserType userType;//section5,byte[0]
    }
}
