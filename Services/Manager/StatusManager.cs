/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-15
 * 说明：（产品数据在各个工站传递）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using QA.Business.Status;

namespace QA.Business.Manager
{
    public class StatusManager
    {
        /// <summary>
        /// 工站传递的产品数据
        /// </summary>
        private StationStatus _stationStatus = new StationStatus();
        public StationStatus StationStatus
        {
            get { return _stationStatus; }
            set { _stationStatus = value; }
        }

        public StatusManager()
        {

        }
    }
}
