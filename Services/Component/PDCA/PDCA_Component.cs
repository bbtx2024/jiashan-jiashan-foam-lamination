/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-10
 * 说明：（PDCA数据上传）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.ObjectModel;
using System.Text;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.Alarm;
using QA_Infrastructure;
using QA_Infrastructure.BaseCtrls;
using QA_Infrastructure.BaseCtrls.ParamEnum;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Component.PDCA
{
    public class PDCA_Component : TcpCtrl, IPDCA
    {
        private PDCAParam _pdcaParam;
        private SocketParam _socketParam = new SocketParam();
        public CacheParamManager _cacheParamManager;
        public IParam Param { get; set; }
        public string ComponentName { get; set; } = "PDCA";
        private object _lockobj = new object();
        public new bool IsConnected { get; set; } = false;

        public PDCA_Component()
        {
            _cacheParamManager = IoC.Get<CacheParamManager>();
            Param = _pdcaParam = IoC.Get<PDCAParam>();
            //_socketParam = IoC.Get<SocketParam>();
        }

        public bool Initial(IParam param)
        {
            try
            {
                Param = _pdcaParam = param as PDCAParam;
                _socketParam.Ip = _pdcaParam.IP;
                _socketParam.Port = _pdcaParam.Port;
                _socketParam.SendTimeout = _pdcaParam.Timeout;
                _socketParam.ReceiveTimeout = _pdcaParam.Timeout;
                _socketParam.SendBuffSize = _pdcaParam.BufferSize;
                _socketParam.ReceiveBuffSize = _pdcaParam.BufferSize;

                base.SetParam(_socketParam);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e + e.StackTrace);
                return false;
            }
            return true;
        }

        public bool Start()
        {
            try
            {
                if (_pdcaParam.BUse)
                {
                    if (!IsConnected)
                    {
                        base.Close();
                        IsConnected = base.Connect(_socketParam);
                    }
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
                return false;
            }

            return true;
        }

        public bool Stop()
        {
            try
            {
                base.Close();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace);
            }
            return true;
        }

        public void GetCurAlarms(ref ObservableCollection<AlarmInfoModel> alarmInfos)
        {
            if (Param.BUse)
            {
                if (!IsConnected)
                {
                    var count = alarmInfos.Count;
                    alarmInfos.Add(new AlarmInfoModel()
                    {
                        AlarmLevel = EN_WARN_LEVEL.Error,
                        AlarmModule = EN_WarnModules.PDCA,
                        AlarmMsg = "无连接",
                        Datetime = DateTime.Now,
                        ErrorCode = 0,
                        Index = count
                    });
                }
            }
        }

        #region PDCA公共代码区

        /// <summary>
        /// PDCA上传起始Start信号：productsn + @start + \n
        /// </summary>
        /// <param name="productsn"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_Start_ToPdca(string productsn, ref StringBuilder packetorder, bool addtopacket = true, bool auditcheck = false)
        {
            StringBuilder startorder = new StringBuilder();

            startorder.Append(productsn);
            startorder.Append("@start");
            if (auditcheck)
            {
                startorder.Append("@Audit");
            }
            startorder.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(startorder.ToString());
                return true;
            }
            string retorder = "";

            //"ok@{true}@\n}"
            if (!SendData(startorder.ToString(), ref retorder, "ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);

                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
            return true;
        }
        /// <summary>
        /// 测试开始时间，时间格式：2020-11-03 12:00:00
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="starttime"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_StartTime_ToPdca(string barcode, string starttime, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            //order.Append("@start");
            //order.Append("\n");
            //order.Append(barcode);
            order.Append("@start_time");
            order.Append("@" + starttime);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                // TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            // TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_StopTime_ToPdca(string barcode, string starttime, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@stop_time");
            order.Append("@" + starttime);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 山东潍坊歌尔dut_pos
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fixtureid"></param>
        /// <param name="carrierid"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_dut_pos_ToPdca(string barcode, string fixtureid, string carrierid, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@dut_pos");
            order.Append("@" + fixtureid);
            order.Append("@" + carrierid);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 上传数据
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="attr"></param>
        /// <param name="data"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_pdata_ToPdca(string barcode, string attr, string data, string min, string max, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@pdata");//@pdata
            order.Append("@" + attr);
            order.Append("@" + data);
            order.Append("@" + min);
            order.Append("@" + max);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_pdata_ToPdca(string barcode, string attr, string data, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@pdata");//@pdata
            order.Append("@" + attr);
            order.Append("@" + data);

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="ip"></param>
        /// <param name="picturepath">图片文件</param>
        /// <param name="filename">图片名称：sn+日期+时间</param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_File_ToPdca(string barcode, string ip, string picturepath, string filename, string user, string pwd, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@log_file@smb://");
            order.Append(ip);//169.254.1.12
            order.Append("/" + picturepath);//Share/AOIWork/ZIP/
            order.Append("/" + filename);
            // order.Append(".zip");
            order.Append("@" + user);//KK
            order.Append("@" + pwd);//123
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 提交 submit@软件版本
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="ip"></param>
        /// <param name="picturepath"></param>
        /// <param name="filename"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_Submit_ToPdca(string barcode, string version, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@submit");
            order.Append("@" + version);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 昆山立讯使用
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="sttr"></param>
        /// <param name="reason"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_dut_pos_ToPdca(string barcode, string fixtureid, bool bleftoright, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@dut_pos");
            order.Append("@" + fixtureid);
            order.Append("@" + (bleftoright == false ? "Left" : "Right"));
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_Operator_ToPdca(string barcode, string operatorid, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@attr");
            order.Append("@OPERATOR");
            order.Append("@" + operatorid);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// </山东潍坊歌尔上传参数>
        /// <param name="barcode"></param>
        /// <param name="key"></param>
        /// <param name="Solderparam"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_SolderParam_ToPdca(string barcode, string key, string Solderparam, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@attr");
            order.Append($"@{key}");
            order.Append("@" + Solderparam);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));
            NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + order.ToString() + " Ret:" + (retorder).Trim('\0'));

            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// UC SN
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="ucsn"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_UCSN_ToPdca(string barcode, string ucsn, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@attr");
            order.Append("@UC SN");
            order.Append("@" + ucsn);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 机器ID
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="machineid"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_MachineID_ToPdca(string barcode, string machineid, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@attr");
            order.Append("@Machine ID");
            order.Append("@" + machineid);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// OP ID
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="opid"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_OPID_ToPdca(string barcode, string opid, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@attr");
            order.Append("@FT130OP ID");
            order.Append("@" + opid);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 测试项NG原因,Wire feed rate,solder tip temperature
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="machineid"></param>
        /// <param name="packetorder"></param>
        /// <param name="addtopacket"></param>
        /// <returns></returns>
        public bool Send_test_pass_ToPdca(string barcode, string sttr, string reason, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@test_pass");
            order.Append("@" + sttr);
            order.Append("@" + reason);
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndOperatorID_ToPdca(string barcode, ref StringBuilder packetorder, bool addtopacket)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@Operator_ID@" + 1);

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndAuditMode_ToPdca(string barcode, AUDIT_MODE mode, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@Mode@" + ((int)mode).ToString());
            order.Append("@NA@NA");

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndAuditModeTestSeriesID_ToPdca(string barcode, DateTime time, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@TestSeriesID@" + time.ToString("yyyyMMddHHmmss"));
            order.Append("@NA@NA");

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndAuditModeOperatorID_ToPdca(string barcode, int opid, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@Operator_ID@" + opid);
            order.Append("@NA@NA");
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndAuditModeOnLine_ToPdca(string barcode, AUDIT_MODE mode, ref StringBuilder packetorder, bool addtopacket)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@Online@" + ((mode == AUDIT_MODE.Normal) ? 0 : 1));

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        public bool Send_EndPriority_ToPdca(string barcode, AUDIT_MODE mode, ref StringBuilder packetorder, bool addtopacket)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);

            order.Append("@pdata@Priority@" + ((mode == AUDIT_MODE.Normal) ? 0 : -2));

            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            //byte[] retorder = new byte[1024];
            //lock (obj)
            //{
            //    if (Send(Encoding.Default.GetBytes(order.ToString())) <= 0)
            //        return false;
            //    if (Recvice(retorder) <= 0)
            //        return false;
            //}
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }

        #endregion

        #region 潍坊歌尔
        public bool WeiFangGoerTek_Send_dut_pos_ToPdca(string barcode, string fixtureid, int cav, ref StringBuilder packetorder, bool addtopacket = true)
        {
            StringBuilder order = new StringBuilder();
            order.Append(barcode);
            order.Append("@dut_pos");
            order.Append("@" + fixtureid);
            order.Append("@" + cav.ToString());
            order.Append("\n");
            if (addtopacket)
            {
                packetorder.Append(order.ToString());
                return true;
            }
            string retorder = "";
            if (!SendData(order.ToString(), ref retorder))
            {
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 获取Station_ID
        /// </summary>
        /// <param name="productsn"></param>
        /// <param name="rev"></param>
        /// <returns></returns>
        public bool WeiFangGoerTek_GetStation_FormPdca(string productsn, ref string rev)
        {
            //string st = "{\nok@{}@\nok@{GTSD_E07-4FT-01_1_STATION62}@\n}\n";
            //string[] sp = st.Split('@');
            //if (sp.Length != 5)
            //{
            //    HistroyLog.WritePdcaLog("GetStation_Geer():Length should be :5 But Actually val ={ sp.Length} ");
            //    return false;
            //}
            //rev = st.Split('@')[3].Replace("{", "").Replace("}", "");

            StringBuilder stationorder = new StringBuilder();
            stationorder.Append("{\n");
            stationorder.Append(productsn);
            stationorder.Append("@start\n");
            stationorder.Append(productsn);
            stationorder.Append("@ghi@STATION_ID");
            stationorder.Append("\n");
            //stationorder.Append(productsn);
            //stationorder.Append("@amiok\n");
            stationorder.Append("}\n");
            string retorder = "";
            //"err@{EINVAL -- no existing session, please create one with 'start' command first}@\n"
            //"ok@{GTSD_E07-4FT-01_1_STATION62}@\n"
            //"{\nok@{}@\nok@{GTSD_E07-4FT-01_1_STATION62}@\n}\n
            //"{\nok@{}@\nok@{GTSD_E07-4FT-01_1_STATION62}@\nok@{true}@\n}\n"
            //LineCode:E07-4FT-01
            if (!SendData(stationorder.ToString(), ref retorder, "ok@"))
            {
                rev = retorder;
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                rev = retorder;
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + retorder);
                return false;
            }
            string[] sp = returnstr.Split('@');
            if (sp.Length != 5)
            {
                HistroyLog.WritePdcaLog("GetStation_Geer():Length should be :5 But Actually val ={ sp.Length} ");
                return false;
            }
            rev = returnstr.Split('@')[3].Replace("{", "").Replace("}", "");
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + retorder);
            return true;
        }
        /// <summary>
        /// 歌尔获取station_id错误  发送cancel
        /// </summary>
        /// <param name="productsn"></param>
        /// <returns></returns>
        public bool WeiFangGoerTek_Send_Cancel_ToPdca(string productsn)
        {
            StringBuilder startorder = new StringBuilder();
            startorder.Append(productsn);
            startorder.Append("@start\n");
            startorder.Append(productsn);
            startorder.Append("@cancel");
            startorder.Append("\n");

            string retorder = "";

            //"ok@{true}@\n}"
            if (!SendData(startorder.ToString(), ref retorder, "ok@{}@"))
            {
                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                return false;
            }
            string returnstr = /*Encoding.Default.GetString*/(retorder).Trim('\0');
            if (!returnstr.Contains("ok@{}@"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);

                HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
                //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
            HistroyLog.WritePdcaLog("PDCA_UOP: Recvice() send:" + startorder.ToString() + " Ret:" + retorder);
            //TraceDebugInfo.TraceOuPut(SixHotBar.WarnLog.EN_WARN_LEVEL.Info, "PDCA: Recvice() send:" + packetorder + " Ret:" + returnstr);
            return true;
        }
        /// <summary>
        /// 潍坊歌尔UOP检查
        /// </summary>
        /// <param name="productsn"></param>
        /// <param name="rev"></param>
        /// <returns></returns>
        public bool WeiFangGoerTek_GetUopCheck_FromPdca(string productsn, ref string rev)
        {
            StringBuilder stationorder = new StringBuilder();
            stationorder.Append("{\n");
            stationorder.Append(productsn);
            stationorder.Append("@start\n");
            stationorder.Append(productsn);
            stationorder.Append("@ghi@STATION_ID");
            stationorder.Append("\n");
            stationorder.Append(productsn);
            stationorder.Append("@amiok\n");
            stationorder.Append("}\n");
            string retorder = "";

            if (!SendData(stationorder.ToString(), ref retorder, "ok@"))//"ok@{true}@"
            {
                rev = retorder;
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + (retorder).Trim('\0'));
                return false;
            }
            string returnstr = retorder.Trim('\0');
            if (!returnstr.Contains("ok@"))
            {
                rev = retorder;
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + retorder);
                return false;
            }
            string[] sp = returnstr.Split('@');
            if (sp.Length != 7)
            {
                HistroyLog.WritePdcaLog($"WeiFangGoerTek_PDCA_UopCheck():Length should be :5 But Actually val ={sp.Length} ");
                return false;
            }
            rev = returnstr.Split('@')[3].Replace("{", "").Replace("}", "");
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, "PDCA_UOP: Recvice() send:" + stationorder.ToString() + " Ret:" + retorder);
            return true;
        }
        #endregion
    }
}
