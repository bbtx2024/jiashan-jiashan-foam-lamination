using System;
using System.Collections.Generic;
using Caliburn.Micro;

namespace QA.UserControls.ViewModels
{
    public struct PointInfo
    {
        public float x;
        public float y;
        //public int addIdx;//添加的顺序在key里面，不需要存在这里
        public int idx;
    }

    public enum EnumArrayType
    {
        Row,            //横向
        Column,         //纵向
        RowChain,       //横向链式
        ColumnChain,    //纵向链式
    }

    public class ArrayPointsViewModel : Screen
    {
        public override string DisplayName { get; set; } = "添加点位阵列";

        public bool AddTypeRow { get; set; } = true;
        public bool AddTypeColumn { get; set; } = false;
        public bool AddTypeRowChain { get; set; } = false;
        public bool AddTypeColumnChain { get; set; } = false;

        private EnumArrayType _arrayTypeOfAdd = EnumArrayType.Row;
        /// <summary>
        /// 生成顺序（即条目的顺序，如视觉点会按照这个顺序拍照）对应的阵列类型
        /// </summary>
        public EnumArrayType ArrayTypeOfAdd
        {
            get => _arrayTypeOfAdd;
            set
            {
                _arrayTypeOfAdd = value;
                AddTypeRow = value == EnumArrayType.Row;
                AddTypeColumn = value == EnumArrayType.Column;
                AddTypeRowChain = value == EnumArrayType.RowChain;
                AddTypeColumnChain = value == EnumArrayType.ColumnChain;
            }
        }

        public bool IdxTypeRow { get; set; } = true;
        public bool IdxTypeColumn { get; set; } = false;
        public bool IdxTypeRowChain { get; set; } = false;
        public bool IdxTypeColumnChain { get; set; } = false;

        private EnumArrayType _arrayTypeOfIdx = EnumArrayType.Row;
        /// <summary>
        /// 序号顺序（即穴位号、吸嘴号等的顺序）对应的阵列类型
        /// </summary>
        public EnumArrayType ArrayTypeOfIdx
        {
            get => _arrayTypeOfIdx;
            set
            {
                _arrayTypeOfIdx = value;
                IdxTypeRow = value == EnumArrayType.Row;
                IdxTypeColumn = value == EnumArrayType.Column;
                IdxTypeRowChain = value == EnumArrayType.RowChain;
                IdxTypeColumnChain = value == EnumArrayType.ColumnChain;
            }
        }

        public int RowCount { get; set; } = 1;
        public int ColumnCount { get; set; } = 1;
        public float StartX { get; set; } = 0.0f;
        public float StartY { get; set; } = 0.0f;
        public float EndX { get; set; } = 0.0f;
        public float EndY { get; set; } = 0.0f;

        /// <summary>
        /// 最终阵列信息存放的位置，已经经过排序
        /// </summary>
        public List<PointInfo> pointInfoList = new List<PointInfo>();

        /// <summary>
        /// 切换拍照顺序阵列类型
        /// </summary>
        /// <param name="arrayType"></param>
        public void ChangeAddArrayType(string arrayType)
        {
            ArrayTypeOfAdd = (EnumArrayType)Enum.Parse(typeof(EnumArrayType), arrayType);
        }

        /// <summary>
        /// 切换序号顺序阵列类型
        /// </summary>
        /// <param name="arrayType"></param>
        public void ChangeIdxArrayType(string arrayType)
        {
            ArrayTypeOfIdx = (EnumArrayType)Enum.Parse(typeof(EnumArrayType), arrayType);
        }

        /// <summary>
        /// 点击确定
        /// </summary>
        public void ConfirmClick()
        {
            CreatePoints();
            TryClose(true);
        }

        /// <summary>
        /// 点击取消
        /// </summary>
        public void CancelClick()
        {
            TryClose(false);
        }

        /// <summary>
        /// 自动生成点位列表
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="arrayType"></param>
        /// <returns></returns>
        public void CreatePoints()
        {
            Dictionary<int, PointInfo> dic = new Dictionary<int, PointInfo>();
            //计算行间距、列间距
            float rowSpace = RowCount == 1 ? 0 : (EndY - StartY) / (RowCount - 1);
            float colSpace = ColumnCount == 1 ? 0 : (EndX - StartX) / (ColumnCount - 1);
            //添加点位信息
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    dic.Add(GetIdxOfArrayType(i, j, ArrayTypeOfAdd), new PointInfo()
                    {
                        x = StartX + j * colSpace,
                        y = StartY + i * rowSpace,
                        idx = GetIdxOfArrayType(i, j, ArrayTypeOfIdx),
                    });
                }
            }
            //按顺序添加到 pointInfoList 中
            pointInfoList = new List<PointInfo>();
            for (int i = 0; i < dic.Count; i++)
            {
                if (dic.ContainsKey(i))
                {
                    pointInfoList.Add(dic[i]);
                }
            }
        }

        /// <summary>
        /// 根据某个点位的行列位置，以及阵列类型，计算出对应的序号（从0开始）
        /// </summary>
        /// <param name="rowIdx">点位对应的行号（从0开始）</param>
        /// <param name="colIdx">点位对应的列号（从0开始）</param>
        /// <param name="type">阵列类型</param>
        /// <returns></returns>
        public int GetIdxOfArrayType(int rowIdx, int colIdx, EnumArrayType type)
        {
            //下面以 RowCount=2，ColumnCount=3，左上角起点，右下角终点 举例
            if (type == EnumArrayType.Row)
            {
                //     col0 col1 col2
                //row0    0    1    2
                //row1    3    4    5
                return rowIdx * ColumnCount + colIdx;
            }
            else if (type == EnumArrayType.Column)
            {
                //     col0 col1 col2
                //row0    0    2    4
                //row1    1    3    5
                return colIdx * RowCount + rowIdx;
            }
            else if (type == EnumArrayType.RowChain)
            {
                //     col0 col1 col2
                //row0    0    1    2
                //row1    5    4    3
                return rowIdx % 2 == 0
                    //奇数行，从小到大
                    ? rowIdx * ColumnCount + colIdx
                    //偶数行，从大到小
                    : (rowIdx + 1) * ColumnCount - colIdx - 1;
            }
            else if (type == EnumArrayType.ColumnChain)
            {
                //     col0 col1 col2
                //row0    0    3    4
                //row1    1    2    5
                return colIdx % 2 == 0
                    //奇数列，从小到大
                    ? colIdx * RowCount + rowIdx
                    //偶数列，从大到小
                    : (colIdx + 1) * RowCount - rowIdx - 1;
            }
            else
            {
                return -1;
            }
        }
    }
}
