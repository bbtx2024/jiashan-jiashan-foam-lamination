using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
namespace QA.Business.Model.HttpInterface
{
    public class HTTPInterface
    {
        private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);
        private static HttpWebResponse httpwebresponse = null;

        #region 昆山立讯
        public static bool Http_LuxShare_HiveUnitData(string ip, string cmd, ref string ret)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/v5/capture/machinedata");

            string resp = "";
            if (!Http_LuxShare_SendMachineCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                HistroyLog.WritePdcaLog($"Http_LuxShare_HiveUnitData, Send ={cmd.ToString()}，Rev= {resp}");
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_LuxShare_HiveUnitData, Send ={cmd.ToString()},resp = {resp}");
                return false;
            }
            ret = resp;
            HistroyLog.WritePdcaLog($"Http_LuxShare_HiveUnitData, Send ={cmd.ToString()}，Rev= {resp}");
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_LuxShare_HiveUnitData,Send ={cmd.ToString()},resp = {resp}");
            return true;
        }
        public static bool Http_LuxShare_HiveData(string ip, string cmd, ref string ret)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/v5/capture/machinestate");

            string resp = "";
            if (!Http_LuxShare_SendMachineCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                HistroyLog.WritePdcaLog($"Http_LuxShare_HiveData, Send ={cmd.ToString()}，Rev= {resp}");
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_LuxShare_HiveData, Send ={cmd.ToString()},resp = {resp}");
                return false;
            }
            ret = resp;
            HistroyLog.WritePdcaLog($"Http_LuxShare_HiveData, Send ={cmd.ToString()}，Rev= {resp}");
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_LuxShare_HiveData,Send ={cmd.ToString()},resp = {resp}");
            return true;
        }
        public static bool Http_LuxShare_ErrorData(string ip, string cmd, ref string ret)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/v5/capture/errordata");

            string resp = "";
            if (!Http_LuxShare_SendMachineCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                HistroyLog.WritePdcaLog($"Http_LuxShare_ErrorData, Send ={cmd.ToString()}，Rev= {resp}");
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_LuxShare_ErrorData, Send ={cmd.ToString()},resp = {resp}");
                return false;
            }
            ret = resp;
            HistroyLog.WritePdcaLog($"Http_LuxShare_ErrorData, Send ={cmd.ToString()}，Rev= {resp}");
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_LuxShare_ErrorData,Send ={cmd.ToString()},resp = {resp}");
            return true;
        }
        public static bool Http_LuxShare_DataExchangedFormMes(string ip, string cmd, ref string ret)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/MainWebFrom.aspx");
            //string cmd = "CMD=ATT&P=RFID_GET_SN&SN=" + carriersn;
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";
            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            ret = resp;
            return true;
        }
        public static bool Http_LuxShare_SendMachineCmdToMES(string strURL, string strCmd, out string strResult)
        {
            strResult = string.Empty;
            try
            {
                byte[] datas = Encoding.ASCII.GetBytes(strCmd);
                WebRequest webRequest = (HttpWebRequest)WebRequest.Create(strURL);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                //"text/plain"
                //"application/json"
                //"application/octet-stream"
                //"application/x-www-form-urlencoded;charset=gb2313"
                //"application/x-www-form-urlencoded;charset=utf-8"
                //"multipart/form-data"
                webRequest.ContentLength = strCmd.Length;

                using (var stream = webRequest.GetRequestStream())
                {
                    stream.Write(datas, 0, strCmd.Length);
                }
                var response = (HttpWebResponse)webRequest.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    strResult = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// LuxShare 通过载具码查询产品码
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="carriersn"></param>
        /// <param name="productsn"></param>
        /// <returns></returns>
        public static bool Http_LuxShare_GetProductSnsFromCarrierSn(string ip, string carriersn, string[] productsn)
        {
            //测试UC:KSH27NP1UC010U0100001
            //http://10.33.27.126/MainWebFrom.aspxRequest.Form:CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001

            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/MainWebFrom.aspx");
            string cmd = "CMD=ATT&P=RFID_GET_SN&SN=" + carriersn;
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";
            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            if (resp.Contains("RFID NOT LINK SN"))
            {
                //productsn[0] = "test";
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"RFID NOT LINK SN");
                return false;
            }
            if (!resp.Contains("0 SFC_OK\t"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            string[] productsns = resp.Replace("0 SFC_OK\tSN=", "").Split(';');

            if (productsns.Length != productsn.Length)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,parse num != param.cavitynum");
                return false;
            }
            for (int i = 0; i < productsns.Length; i++)
            {
                productsn[i] = productsns[i];
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,resp = {resp}");
            return true;
        }

        #endregion

        /// <summary>
        /// 这个函数弃用，deprecated
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="carriersn"></param>
        /// <param name="productsn"></param>
        /// <returns></returns>
        public static bool Http_GetProcSNByCarrierSN(string ip, string carriersn, string[] productsn)
        {
            //测试UC:KSH27NP1UC010U0100001
            //http://10.33.27.126/MainWebFrom.aspxRequest.Form:CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001

            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/api/gate/");
            string cmd = "{\r\n\"cmd\":2 ,\r\n\"Hwd\":\"\",\r\n\"Indicator\":null,\r\n\"SerializeData\":\"{\\\"user\\\":\\\"0949435\\\",\\\"pwd\\\":\\\"0949435\\\"}\"\r\n}";
            Console.WriteLine(cmd);
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";
            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            if (resp.Contains("RFID NOT LINK SN"))
            {
                //productsn[0] = "test";
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"RFID NOT LINK SN");
                return false;
            }
            if (!resp.Contains("0 SFC_OK\t"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            string[] productsns = resp.Replace("0 SFC_OK\tSN=", "").Split(';');

            if (productsns.Length != productsn.Length)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,parse num != param.cavitynum");
                return false;
            }
            for (int i = 0; i < productsns.Length; i++)
            {
                productsn[i] = productsns[i];
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,resp = {resp}");
            return true;
        }

        #region 潍坊歌尔
        /// <summary>
        /// 获取令牌
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="hwd"></param>
        /// <returns></returns>
        public static bool Http_WeiFangGoerTek_GetHwd(string ip, string Opid, ref string hwd)
        {
            StringBuilder url = new StringBuilder();
            string cmd = "";
            string resp = "";
            string _hwd = "";
            try
            {
                //获取口令,同一个账号密码获取的HWD每次都不一样？？？？
                //每次都不一样，没有影响                
                url.Append("http://" + ip + "/api/gate/");
                cmd = "{\r\n\"cmd\":2 ,\r\n\"Hwd\":\"\",\r\n\"Indicator\":null,\r\n\"SerializeData\":\"{\\\"user\\\":\\\"" + Opid + "\\\",\\\"pwd\\\":\\\"" + Opid + "\\\"}\"\r\n}";
                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_GetHwd()_GetHwd,Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_GetHwd()_GetHwd,Err,Send={cmd}; Rev= {resp}");
                    hwd = "Error";
                    return false;
                }
                //resp:"{\"cmd\":4,\"Hwd\":\"ac2f438a-5ce9-4991-90db-14ecc0c39672\",\"Indicator\":null,\"SerializeData\":null,\"Display\":null,\"BindInfo\":null}"
                //resp:"{\"cmd\":4,\"Hwd\":\"665485e8-e1aa-489e-80f2-25ebd207e5aa\",\"Indicator\":null,\"SerializeData\":null,\"Display\":null,\"BindInfo\":null}"
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetHwd()_GetHwd,Send={cmd}; Rev= {resp}");
                HistroyLog.WritePdcaLog($"Http_GetHwd()_GetHwd,Send={cmd}; Rev= {resp}");
                if (!resp.Contains("\"cmd\":4"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetHwd()_GetHwd,Login Failure");
                    HistroyLog.WritePdcaLog($"Http_GetHwd()_GetHwd,Login Failure");
                    hwd = "Error";
                    return false;
                }
                _hwd = resp.Replace("{\"cmd\":4,\"Hwd\":\"", "").Replace("\",\"Indicator\":null,\"SerializeData\":null,\"Display\":null,\"BindInfo\":null}", "");
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetHwd()_GetHwd,Hwd={_hwd}");
                HistroyLog.WritePdcaLog($"Http_GetHwd()_GetHwd,Hwd={_hwd}");
                hwd = _hwd;
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message);
                HistroyLog.WritePdcaLog($"Http_GetHwd()_Catch,{ex.Message}");
                hwd = "Error";
                return false;
            }
        }
        public static bool Http_WeiFangGoerTek_BindingStation(string ip, string lineCode, string stationCode, string sectionCode, string serverVersion, string hwd)
        {//我们这一站不需要绑定工站
            StringBuilder url = new StringBuilder();
            string cmd = "";
            string resp = "";
            url.Append("http://" + ip + "/api/gate/");

            //注意:请求参数传递时"ServerVersion\":\" \"为空值，系统自动以默认版本返回给客户端，如响应参数中绑定成功可以获取到当前使用的服务端版
            cmd = "{\r\n\"cmd\":10 ,\r\n\"Hwd\":\"" + hwd + "\",\r\n\"Indicator\":null,\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                    "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":\\\"" + stationCode + "\\\",\\\"ServerVersion\\\":\\\"" + serverVersion + "\\\"}\"\r\n}";

            if (!SendCmdToMES(url.ToString(), cmd, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_BindingStation()_BindingStation,Send={cmd}; Rev= {resp}");
                HistroyLog.WritePdcaLog($"Http_BindingStation()__BindingStation,Err,Send={cmd}; Rev= {resp}");
                return false;
            }
            if (!resp.Contains("\"cmd\":11"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_BindingStation()_BindingStation,Send={cmd}; Rev= {resp}");
                HistroyLog.WritePdcaLog($"Http_BindingStation()_Err_BindingStation,Send={cmd}; Rev= {resp}");
                return false;
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_BindingStation()_BindingStation,Send={cmd}; Rev= {resp}");
            HistroyLog.WritePdcaLog($"Http_BindingStation()_BindingStation,Send={cmd}; Rev= {resp}");
            return true;
        }

        /// </潍坊歌尔获取产品码>
        /// <param name="ip"></param>
        /// <param name="lineCode"></param>
        /// <param name="carriersn"></param>
        /// <param name="stationCode"></param>
        /// <param name="sectionCode"></param>
        /// <param name="SoftVersion"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="productsn"></param>
        /// <returns></returns>
        public static bool Http_WeiFangGoerTek_GetProductSnsFromCarrierSn(string ip, string lineCode, string carriersn, string stationCode, string sectionCode, string hwd, ref string productsn)
        {
            StringBuilder url = new StringBuilder();
            string cmd = "";
            string resp = "";

            //获取sn 
            url.Append("http://" + ip + "/Api/AutoGate");
            cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                 "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"GET_SN_BY_SN_FIXTURE\\\",\\\"OPRequestInfo\\\":{\\\"REF_VALUE\\\":\\\"" + carriersn +
                "\\\",\\\"REF_TYPE\\\":\\\"SN_FIXTURE\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";

            //cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
            //     "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"GET_SNLIST_BY_FIXTURESN\\\",\\\"OPRequestInfo\\\":{\\\"REF_VALUE\\\":\\\"" + carriersn +
            //     "\\\",\\\"REF_TYPE\\\":\\\"SN_FIXTURE\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";

            if (!SendCmdToMES(url.ToString(), cmd, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_GetProcSNByCarrierSNgr()_GetProductSN,Send={cmd}; Rev= {resp}");
                HistroyLog.WritePdcaLog($"Http_GetProcSNByCarrierSNgr()_GetProductSN,Err,Send={cmd}; Rev= {resp}");
                return false;
            }
            if (resp.Contains("\"cmd\":16"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetProcSNByCarrierSNgr()_GetProductSN,Send={cmd}; Rev= {resp}");
                HistroyLog.WritePdcaLog($"Http_GetProcSNByCarrierSNgr()_GetProductSN,Send={cmd}; Rev= {resp}");
                string[] sArray = resp.Split(new string[] { "\\\\\\\"" }, StringSplitOptions.RemoveEmptyEntries);
                if (sArray.Length > 2)
                {
                    productsn = sArray[1];
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetProcSNByCarrierSNgr()_GetProductSN,ProductSN={productsn}");
                    HistroyLog.WritePdcaLog($"Http_GetProcSNByCarrierSNgr()_GetProductSN,ProductSN={productsn}");
                    return true;
                }
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,Cmd={cmd};resp = {resp}");
                return false;
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,Cmd={cmd};resp = {resp}");
                return false;
            }
        }

        /// <summary>
        /// 山东潍坊GoerTek检查MES过站状态
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="lineCode"></param>
        /// <param name="carriersn"></param>
        /// <param name="stationCode"></param>
        /// <param name="sectionCode"></param>
        /// <param name="productsn"></param>
        /// <param name="hwd">令牌</param>
        /// <returns></returns>
        public static bool Http_WeiFangGoerTek_GetMesCheckRouteStatus(string ip, string lineCode, string stationCode, string sectionCode, string hwd, string carriersn, string productsn)
        {
            StringBuilder url = new StringBuilder();
            string cmd = "";
            string resp = "";
            //接口3: 依据SN检查路由，确认产品是否允许过站
            url.Append("http://" + ip + "/Api/AutoGate");
            cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                  "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"UNIT_PROCESS_CHECK\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                  "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";
            try
            {
                //NG情况"{\"cmd\":16,\"Hwd\":\"94b31ed9-b294-455d-987e-0a5004e068cd\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UNIT_PROCESS_CHECK\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"C04NP1UC010A0000004\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"NO WO\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                //NG情况"{\"cmd\":16,\"Hwd\":\"57623218-c3f9-4f2a-baaa-379d3187183d\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UNIT_PROCESS_CHECK\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"C04NP1UC02000000011\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"NO WO\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_GetMESCheckState(),Send={cmd}; Rev= {resp}", En_Logout_Type.Mes);
                    return false;
                }
                //OK情况:"{\"cmd\":16,\"Hwd\":\"7f0b11e9-379e-466d-971a-e533c3cc5894\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UNIT_PROCESS_CHECK\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetMESCheckState(),Send={cmd}; Rev= {resp}", En_Logout_Type.Mes);
                    return true;
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_GetMESCheckState(),Send={cmd}; Rev= {resp}", En_Logout_Type.Mes);
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message, En_Logout_Type.Mes);
                return false;
            }
        }

        /// <summary>
        /// 潍坊歌尔上传MES数据
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="lineCode"></param>
        /// <param name="stationCode"></param>
        /// <param name="sectionCode"></param>
        /// <param name="wireFeedRate"></param>
        /// <param name="soldTemp"></param>
        /// <param name="productsn"></param>
        /// <param name="Hwd"></param>       
        /// <returns></returns>
        public static bool Http_WeiFangGoerTek_UpLoadInfoByProcSN(string ip, string lineCode, string stationCode, string sectionCode, float wireFeedRate,
            float soldTemp, float WireretentionT, string productsn, string Hwd, bool markPass, string wiretype, string soldertiptype, string TotalSolderCount, string errcode)
        {

            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/Api/AutoGate");
            string cmd = "";
            string resp = "";
            string errcode1 = markPass ? "NA" : errcode;
            try
            {

                cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"ADD_ATTR\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                 "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                 "\\\",\\\"Wire_Sold_Temp\\\":\\\"" + soldTemp + "\\\",\\\"Wire_Feed_Rate\\\":\\\"" + wireFeedRate + "\\\",\\\"Solder_wire_time\\\":\\\"" + WireretentionT + "\\\",\\\"wiretype\\\":\\\"" +
                 wiretype + "\\\",\\\"soldertiptype\\\":\\\"" + soldertiptype + "\\\",\\\"TotalSolderCount\\\":\\\"" +
                 TotalSolderCount + "\\\",\\\"ERROR_CODE\\\":\\\"" + errcode1 + "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";


                #region 接口4: 依据SN，上传设备参数信息
                //cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"ADD_ATTR\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                //      "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                //      "\\\",\\\"Wire_Sold_Temp\\\":\\\"" + soldTemp + "\\\",\\\"Wire_Feed_Rate\\\":\\\"" + wireFeedRate + "\\\",\\\"Solder_wire_time\\\":\\\"" + WireretentionT + "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";
                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_WeiFangGoerTek_UpLoadInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp},MarkPass={markPass}", En_Logout_Type.Mes);
                    //HistroyLog.WritePdcaLog($"Http_WeiFangGoerTek_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp},MarkPass={markPass}");
                    return false;
                }
                //"{\"cmd\":16,\"Hwd\":\"a4bfefe4-495f-4a1e-9158-82023ca4d05c\",\"Indicator\":\"ADD_ATTR\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\",\\\"Wire_Sold_Temp\\\":\\\"315\\\",\\\"Wire_Feed_Rate\\\":\\\"10\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                {
                    //正确回复:"{\"cmd\":16,\"Hwd\":\"a7db1e29-bec2-452f-bbbe-5c643e6fa7b2\",\"Indicator\":\"ADD_ATTR\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"C04NP1UC010A0000004\\\",\\\"Wire_Sold_Temp\\\":\\\"315\\\",\\\"Wire_Feed_Rate\\\":\\\"10\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_WeiFangGoerTek_UpLoadInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp},MarkPass={markPass}", En_Logout_Type.Mes);
                    //HistroyLog.WritePdcaLog($"Http_WeiFangGoerTek_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp},MarkPass={markPass}");
                    return true;
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_WeiFangGoerTek_UpLoadInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp},MarkPass={markPass}", En_Logout_Type.Mes);
                    //HistroyLog.WritePdcaLog($"Http_WeiFangGoerTek_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp},MarkPass={markPass}");
                    return false;
                }
                #endregion

                #region 检查信息完整度

                //cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"ADD_ATTR\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                //      "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"GET_INPUT_DATA\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                //      "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";
                //if (!SendCmdToMES(url.ToString(), cmd, out resp))
                //{
                //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                //    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                //    return false;
                //}
                ////"{\"cmd\":16,\"Hwd\":\"a4bfefe4-495f-4a1e-9158-82023ca4d05c\",\"Indicator\":\"ADD_ATTR\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\",\\\"Wire_Sold_Temp\\\":\\\"315\\\",\\\"Wire_Feed_Rate\\\":\\\"10\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                //if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                //{
                //    //正确回复:"{\"cmd\":16,\"Hwd\":\"a7db1e29-bec2-452f-bbbe-5c643e6fa7b2\",\"Indicator\":\"ADD_ATTR\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UPLOAD_INFOS\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"C04NP1UC010A0000004\\\",\\\"Wire_Sold_Temp\\\":\\\"315\\\",\\\"Wire_Feed_Rate\\\":\\\"10\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                //    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                //    return true;
                //}
                //else
                //{
                //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                //    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                //    return false;
                //}

                #endregion

                #region 提交过站
                //
                //
                //    cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"ADD_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                //          "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"UNIT_PROCESS_COMMIT\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                //          "\\\"},\\\"OPResponseInfo\\\":{}}\r\n}";

                //    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,CMD5 = {cmd}");
                //    if (!SendCmdToMES(url.ToString(), cmd, out resp))
                //    {
                //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp6 = {resp}");
                //        return false;
                //    }
                //    else if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                //    {
                //        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,resp5 = {resp}");

                //        messtep++;
                //        break;

                //    }
                //    else
                //    {
                //        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp5 = {resp}");
                //        return false;
                //    }              
                #endregion
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message, En_Logout_Type.Mes);
                //HistroyLog.WritePdcaLog($"Http_WeiFangGoerTek_UpSodeInfoByProcSN()_Catch,{ex.Message}");
                return false;
            }
        }

        //潍坊检查保养信息
        public static bool Http_WeiFangGoerTek_checkkeepfit(string ip, string lineCode, string stationCode, string sectionCode, string Hwd, string EQUIP_ID, ref string keepfitinfo)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/Api/AutoGate");
            string cmd = "";
            string resp = "";

            try
            {
                cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                 "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode +
                 ",\\\"OPCategory\\\":\\\"CHECK_AUTOMATION_EQUIPMENT_STATUS\\\",\\\"OPRequestInfo\\\":{\\\"EQUIP_ID\\\":\\\"" + EQUIP_ID + "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";


                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                    return false;
                }
                if (resp.Contains("\"cmd\":16"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_GetProcSNByCarrierSNgr()_GetProductSN,Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_GetProcSNByCarrierSNgr()_GetProductSN,Send={cmd}; Rev= {resp}");
                    string[] sArray = resp.Split(new string[] { "message" }, StringSplitOptions.RemoveEmptyEntries);
                    keepfitinfo = sArray[1];
                    return true;
                }


                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message);
                HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_Catch,{ex.Message}");
                return false;
            }
        }
        //潍坊歌尔上传保养信息
        public static bool Http_WeiFangGoerTek_upkeepfit(string ip, string lineCode, string stationCode, string sectionCode, string Hwd, string maintainer, string detail, string EQUIP_ID)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/Api/AutoGate");
            string cmd = "";
            string resp = "";

            try
            {
                cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + Hwd + "\",\r\n\"Indicator\":\"ADD_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                 "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode +
                 ",\\\"OPCategory\\\":\\\"MAINTAIN_COMMIT\\\",\\\"OPRequestInfo\\\":{\\\"EQUIP_ID\\\":\\\"" + EQUIP_ID +
                 "\\\",\\\"MAINTAINER\\\":\\\"" + maintainer + "\\\",\\\"DETAIL\\\":\\\"" + detail + "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";


                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                    return false;
                }

                if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    return true;
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSodeInfoByProcSN()_ADD_ATTR,Send={cmd},Rev={resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_ADD_ATTR,Err,Send={cmd},Rev={resp}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message);
                HistroyLog.WritePdcaLog($"Http_UpSodeInfoByProcSN()_Catch,{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据产品码，LineCode,SectionCode,StationCode查询当前站设备参数信息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="lineCode"></param>
        /// <param name="sectionCode"></param>
        /// <param name="stationCode"></param>
        /// <param name="productsn"></param>
        /// <param name="hwd"></param>
        /// <param name="zt"></param>
        /// <param name="sArray"></param>
        /// <returns></returns>
        public static bool Http_WeiFangGoerTek_QueryUpLoadDevInfo(string ip, string lineCode, string stationCode, string sectionCode, string productsn, string hwd, ref int zt, ref string[] sArray)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + "/Api/AutoGate");
            string cmd = "";
            string resp = "";
            //接口11: 依据SN获得当前工站工装/设备参数信息
            cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                  "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"GET_INPUT_DATA\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"" + productsn +
                  "\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";
            //错误情况"{\"cmd\":-1,\"Hwd\":\"f38fda5a-c3fe-4cee-bb27-de05f59068db\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"NG,107LN6T2FK:没有Wire_Feed_Rate参数信息，1请核对!\",\"Display\":null,\"BindInfo\":null}"
            try
            {
                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_QueryUpLoadDevInfo(),Send={cmd},Rev={resp}");
                    HistroyLog.WritePdcaLog($"Http_QueryUpLoadDevInfo,Err,Send={cmd},Rev={resp}");
                    return false;
                }
                NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_QueryUpLoadDevInfo,Send={cmd},Rev={resp}");
                HistroyLog.WritePdcaLog($"Http_QueryUpLoadDevInfo,Send={cmd},Rev={resp}");
                if (resp.Contains("\"cmd\":16"))
                {//正确返回:"{\"cmd\":16,\"Hwd\":\"a4bfefe4-495f-4a1e-9158-82023ca4d05c\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"GET_INPUT_DATA\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\"},\\\"OPResponseInfo\\\":{\\\"Data\\\":\\\"{\\\\\\\"FIXTURE-UC020\\\\\\\":\\\\\\\"C04NP1UC02000000050\\\\\\\",\\\\\\\"Wire_Sold_Temp\\\\\\\":\\\\\\\"315\\\\\\\",\\\\\\\"Wire_Feed_Rate\\\\\\\":\\\\\\\"10\\\\\\\"}\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                 //{\\\"Data\\\":\\\"{\\\\\\\"FIXTURE-UC020\\\\\\\":\\\\\\\"C04NP1UC02000000050\\\\\\\",\\\\\\\"Wire_Sold_Temp\\\\\\\":\\\\\\\"315\\\\\\\",\\\\\\\"Wire_Feed_Rate\\\\\\\":\\\\\\\"10\\\\\\\"}\\\"}}\"

                    //[0]->"{\"cmd\":16,\"Hwd\":\"a4bfefe4-495f-4a1e-9158-82023ca4d05c\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"GET_INPUT_DATA\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\"},\\\"OPResponseInfo\\\":{\\\"Data\\\":\\\"{"
                    //[1]->"FIXTURE-UC020"//uc010
                    //[2]->":"
                    //[3]->"C04NP1UC02000000050"
                    //[4]->","
                    //[5]->"Wire_Sold_Temp"
                    //[6]->":"
                    //[7]->"315"
                    //[8]->","
                    //[9]->"Wire_Feed_Rate"
                    //[10]->":"
                    //[11]->"10"
                    //[12]->"}\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                    if (resp.Contains("FIXTURE") && resp.Contains("CARRIER"))
                    {
                        zt = 1;
                        sArray = resp.Split(new string[] { "\\\\\\\"" }, StringSplitOptions.RemoveEmptyEntries);
                        return true;
                    }
                    else if (resp.Contains("FIXTURE"))
                    {//目前进入这里：FIXTURE-UC020
                        zt = 2;
                        sArray = resp.Split(new string[] { "\\\\\\\"" }, StringSplitOptions.RemoveEmptyEntries);
                        return true;
                    }
                    else if (resp.Contains("CARRIER"))
                    {
                        zt = 3;
                        sArray = resp.Split(new string[] { "\\\\\\\"" }, StringSplitOptions.RemoveEmptyEntries);
                        return true;
                    }
                    else
                    {
                        zt = 4;
                        sArray = resp.Split(new string[] { "\\\\\\\"" }, StringSplitOptions.RemoveEmptyEntries);
                        return true;
                    }
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_QueryUpLoadDevInfo()_QUERY_RECORD,Resp Not Contain:cmd:16");
                    HistroyLog.WritePdcaLog($"Http_QueryUpLoadDevInfo()_QUERY_RECORD,Resp Not Contain:cmd:16");
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message);
                HistroyLog.WritePdcaLog($"Http_QueryUpLoadDevInfo()_Catch,{ex.Message}");
                return false;
            }
        }
        //潍坊歌尔上传供应商版本
        public static bool Http_WeiFangGoerTek_UpSubmit(string ip, string lineCode, string stationCode, string sectionCode, string hwd, string SoftVersion)
        {
            StringBuilder url = new StringBuilder();
            string cmd = "";
            string resp = "";
            //接口3: 依据SN检查路由，确认产品是否允许过站
            url.Append("http://" + ip + "/Api/AutoGate");
            cmd = "{\r\n\"cmd\":16,\r\n\"Hwd\":\"" + hwd + "\",\r\n\"Indicator\":\"QUERY_RECORD\",\r\n\"SerializeData\":\"{\\\"LineCode\\\":\\\"" + lineCode +
                  "\\\",\\\"SectionCode\\\":\\\"" + sectionCode + "\\\",\\\"StationCode\\\":" + stationCode + ",\\\"OPCategory\\\":\\\"CHECK_SECTION_SOFT_VERSION\\\",\\\"OPRequestInfo\\\":{\\\"SOFT_VERSION\\\":\\\"" + SoftVersion +
                  "\\\",\\\"VENDOR_CODE\\\":\\\"QUICK\\\"},\\\"OPResponseInfo\\\":{}}\"\r\n}";
            try
            {

                if (!SendCmdToMES(url.ToString(), cmd, out resp))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSubmit(),Err,Send={cmd}; Rev= {resp}");
                    return false;
                }
                //OK情况:"{\"cmd\":16,\"Hwd\":\"7f0b11e9-379e-466d-971a-e533c3cc5894\",\"Indicator\":\"QUERY_RECORD\",\"SerializeData\":\"{\\\"LineCode\\\":\\\"E07-4FT-01\\\",\\\"SectionCode\\\":\\\"T04-STATION62\\\",\\\"StationCode\\\":3103,\\\"OPCategory\\\":\\\"UNIT_PROCESS_CHECK\\\",\\\"OPRequestInfo\\\":{\\\"SN\\\":\\\"107MDTCC6K\\\"},\\\"OPResponseInfo\\\":{\\\"Result\\\":\\\"OK\\\"}}\",\"Display\":null,\"BindInfo\":null}"
                if (resp.Contains("{\\\"Result\\\":\\\"OK\\\"}"))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    return true;
                }
                else
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    HistroyLog.WritePdcaLog($"Http_UpSubmit(),Send={cmd}; Rev= {resp}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, ex.Message);
                HistroyLog.WritePdcaLog($"Http_UpSubmit()_Catch,{ex.Message}");
                return false;
            }
        }

        #endregion

        #region 越南歌尔
        /// <summary>
        /// GoerTek 通过载具码查询产品码 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="carriersn"></param>
        /// <param name="productsn"></param>
        /// <returns></returns>
        public static bool Http_YueNanGoerTek_GetProcSNByCarrierSN(string ip, string carriersn, string[] productsn)
        {
            //测试UC:KSH27NP1UC010U0100001
            //http://10.33.27.126/MainWebFrom.aspxRequest.Form:CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001

            //http://10.191.242.179:80/api/bobat
            //Content - Type:application / x - www - form - urlencoded; charset = UTF - 8
            //QUERY_RECORD & raw_sn = GTK - H020 - 01 - 0002 & P = Get_SN_BY_RAW_SN
            // 0 SFC_OK sn=

            StringBuilder url = new StringBuilder();
            // url.Append("http://" + ip + ":80/api/bobat Content-Type:application/x-www-form-urlencoded;charset=UTF-8");
            url.Append("http://" + ip + ":80/api/bobcat");
            // string cmd = "CMD=QUERY_RECORD&raw_sn= " + carriersn + "&P=Get_SN_BY_RAW_SN";
            string cmd = "c=QUERY_RECORD&raw_sn=" + carriersn + "&p=GET_SN_BY_RAW_SN";
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";
            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            if (resp.Contains("RFID NOT LINK SN"))
            {
                //productsn[0] = "test";
                return false;
            }
            if (!resp.Contains("0 SFC_OK"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            string[] productsns = resp.Replace("0 SFC_OK\nsn=", "").Split(';');

            if (productsns.Length != productsn.Length)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,parse num != param.cavitynum");
                return false;
            }
            for (int i = 0; i < productsns.Length; i++)
            {
                productsn[i] = productsns[i];
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,resp = {resp}");
            return true;
        }
        /// <summary>
        /// 越南歌尔数据上传
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="cmd"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static bool Http_YueNanGoerTek_DataExchangedFormMes(string ip, string cmd, ref string ret)
        {
            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + ":80/api/bobcat");
            //string cmd = "CMD=ATT&P=RFID_GET_SN&SN=" + carriersn;
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";
            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                return false;
            }
            ret = resp;
            return true;
        }
        #endregion

        /// <summary>
        /// demo获取mes信息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="carriersn"></param>
        /// <param name="productsn"></param>
        /// <param name="cmd"></param>
        /// <param name="intf"></param>
        /// <param name="strRes"></param>
        /// <returns></returns>
        public static bool Http_GetMesInfo(string ip, string carriersn, string[] productsn, string cmd, string intf, out string strRes)
        {
            //测试UC:KSH27NP1UC010U0100001
            //http://10.33.27.126/MainWebFrom.aspxRequest.Form:CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001

            StringBuilder url = new StringBuilder();
            url.Append("http://" + ip + intf/*"/api/gate/"*/);
            // cmd = "{\r\n\"cmd\":2 ,\r\n\"Hwd\":\"\",\r\n\"Indicator\":null,\r\n\"SerializeData\":\"{\\\"user\\\":\\\"0949435\\\",\\\"pwd\\\":\\\"0949435\\\"}\"\r\n}";
            Console.WriteLine(cmd);
            //string url = "http://10.33.27.126/MainWebFrom.aspx";//CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001";
            string resp = "";

            if (!SendCmdToMES(url.ToString(), cmd/*"CMD=ATT&P=RFID_GET_SN&SN=KSH27NP1UC010U0100001"*/, out resp))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                strRes = resp;
                return false;
            }
            if (resp.Contains("RFID NOT LINK SN"))
            {
                //productsn[0] = "test";
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"RFID NOT LINK SN");
                strRes = resp;
                return false;
            }
            if (!resp.Contains("0 SFC_OK\t"))
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,resp = {resp}");
                strRes = resp;
                return false;
            }
            string[] productsns = resp.Replace("0 SFC_OK\tSN=", "").Split(';');

            if (productsns.Length != productsn.Length)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Http_Post failure,parse num != param.cavitynum");
                strRes = resp;
                return false;
            }
            for (int i = 0; i < productsns.Length; i++)
            {
                productsn[i] = productsns[i];
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"Http_Post OK,resp = {resp}");

            strRes = resp;
            return true;
        }

        public static bool SendCmdToMES(string strURL, string strCmd, out string strResult)
        {
            strResult = string.Empty;
            try
            {
                //普通文本 "text/plain"
                //JSON字符串 "application/json"
                //"application/octet-stream"
                //表单数据(键值对) "application/x-www-form-urlencoded"
                //"application/x-www-form-urlencoded;charset=gb2312"
                //"application/x-www-form-urlencoded;charset=utf-8"
                //"multipart/form-data"
                byte[] datas = Encoding.ASCII.GetBytes(strCmd);
                WebRequest webRequest = (HttpWebRequest)WebRequest.Create(strURL);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = strCmd.Length;

                using (var stream = webRequest.GetRequestStream())
                {
                    stream.Write(datas, 0, strCmd.Length);
                }
                var response = (HttpWebResponse)webRequest.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    strResult = sr.ReadToEnd();


                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, e.Message);
                //MessageBox.Show(e.Message);
                //WarningMgr.GetInstance().Warning(e.Message);
                return false;
            }
            return true;
        }

        public static WebHeaderCollection Http_Post(string url, string method, byte[] postData, Encoding responseEncoding, out string ret, string contentType, ref string errinfo)
        {
            //Encoding.UTF8.GetBytes
            //Encoding.UTF8
            //普通文本 "text/plain"
            //JSON字符串 "application/json"
            //"application/octet-stream"
            //表单数据(键值对) "application/x-www-form-urlencoded"
            //"application/x-www-form-urlencoded;charset=gb2312"
            //"application/x-www-form-urlencoded;charset=utf-8"
            //"multipart/form-data"
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Accept = "application/json,text/javascript,*/*;q=0.01";
            request.ContentType = contentType;
            request.Method = method;
            request.CookieContainer = new CookieContainer();
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            request.ContinueTimeout = 10000;
            if (postData != null)
            {
                var stream = request.GetRequestStream();
                stream.Write(postData, 0, postData.Length);
                stream.Close();
            }
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                if (responseEncoding != null)
                {
                    using (var sr = new StreamReader(response.GetResponseStream(), responseEncoding))
                    {
                        ret = sr.ReadToEnd();
                    }
                }
                else
                {
                    ret = string.Empty;
                }
                response.Close();
                return response.Headers;
            }
            catch (Exception ex)
            {

            }
            ret = string.Empty;
            return response.Headers;
        }

        public static string Http_Get(string url, int timeout)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(/*"http://www.baidu.com"*/url);
                req.Timeout = timeout;
                //req.Method = "Get";
                req.BeginGetResponse(new AsyncCallback(RespCallback), req);
                if (TimeoutObject.WaitOne(timeout, false))
                {
                    if (httpwebresponse != null)
                    {
                        Stream stream = httpwebresponse.GetResponseStream();
                        StreamReader reader = new StreamReader(stream, Encoding.Default);
                        return reader.ReadToEnd().ToString();
                    }
                    return "";
                }
                else
                {//超时
                    return "";
                }
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.CriticalError, "Http_Get()" + ex.Message + ex.StackTrace);
            }
            return "";
        }

        private static void RespCallback(IAsyncResult ar)
        {
            try
            {
                HttpWebRequest req = ar.AsyncState as HttpWebRequest;
                httpwebresponse = (HttpWebResponse)req.EndGetResponse(ar);
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.CriticalError, "RespCallback()" + ex.Message + ex.StackTrace);
            }
            finally
            {
                TimeoutObject.Set();
            }
        }
    }
}
