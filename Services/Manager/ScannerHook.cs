using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QA.Business.Manager
{
    public class ScannerHook
    {
        //public delegate void ScannerDelegate(ScannerCodes codes);

        //public event ScannerDelegate ScannerEvent;

        //private const int WM_KEYDOWN = 0x100;//KEYDOWN
        //private const int WM_KEYUP = 0x101;//KEYUP
        //private const int WM_SYSKEYDOWN = 0x104;//SYSKEYDOWN
        //private const int WM_SYSKEYUP = 0x105;//SYSKEYUP
        //private static int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        private static int hKeyboardHook = 0; //声明键盘钩子处理的初始值

        private static ScannerCodes codes = new ScannerCodes(); //13为键盘钩子

        //定义成静态，这样不会抛出回收异常
        private static HookProc hookproc;

        delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)] //设置钩子
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)] //卸载钩子
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)] //继续下个钩子
        private static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        [DllImport("user32", EntryPoint = "GetKeyNameText")]
        private static extern int GetKeyNameText(int IParam, StringBuilder lpBuffer, int nSize);

        [DllImport("user32", EntryPoint = "GetKeyboardState")] //获取按键的状态
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32", EntryPoint = "ToAscii")] //ToAscii职能的转换指定的虚拟键码和键盘状态的相应字符或字符
        private static extern bool ToAscii(int VirtualKey, int ScanCode, byte[] lpKeySate, ref uint lpChar, int uFlags);
        //int VirtualKey //[in] 指定虚拟关键代码进行翻译。
        //int uScanCode, // [in] 指定的硬件扫描码的关键须翻译成英文。高阶位的这个值设定的关键，如果是（不压）
        //byte[] lpbKeyState, // [in] 指针，以256字节数组，包含当前键盘的状态。每个元素（字节）的数组包含状态的一个关键。
        //如果高阶位的字节是一套，关键是下跌（按下）。在低比特，如/果设置表明，关键是对切换。
        //在此功能，只有肘位的CAPS LOCK键是相关的。在切换状态的NUM个锁和滚动锁定键被忽略。
        //byte[] lpwTransKey, // [out] 指针的缓冲区收到翻译字符或字符。
        //uint fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise.


        [DllImport("kernel32.dll")] //使用WINDOWS API函数代替获取当前实例的函数,防止钩子失效
        public static extern IntPtr GetModuleHandle(string name);

        public ScannerHook()
        {
        }

        public static bool Start()
        {
            if (hKeyboardHook == 0)
            {
                hookproc = new HookProc(KeyboardHookProc);
                //GetModuleHandle 函数 替代 Marshal.GetHINSTANCE，防止在 framework4.0中 注册钩子不成功  
                IntPtr modulePtr = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
                //全局钩子 WH_KEYBOARD_LL=13
                hKeyboardHook = SetWindowsHookEx(13, hookproc, modulePtr, 0);
            }
            return (hKeyboardHook != 0);
        }

        public static bool Stop()
        {
            if (hKeyboardHook != 0)
            {
                bool retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
                return retKeyboard;
            }

            return true;
        }

        private static int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            EventMsg msg = (EventMsg)Marshal.PtrToStructure(lParam, typeof(EventMsg));
            codes.Add(msg);
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }

        public class ScannerCodes
        {
            private int ts = 50; // 指定输入间隔为50毫秒以内时为连续输入  
            private List<List<EventMsg>> _keys = new List<List<EventMsg>>();
            private List<int> _keydown = new List<int>(); // 保存组合键状态  
            private List<string> _result = new List<string>(); // 返回结果集  
            private DateTime _last = DateTime.Now;
            private byte[] _state = new byte[256];
            private string _key = string.Empty;
            private string _cur = string.Empty;

            public EventMsg Event
            {
                get
                {
                    if (_keys.Count == 0)
                    {
                        return new EventMsg();
                    }
                    else
                    {
                        return _keys[_keys.Count - 1][_keys[_keys.Count - 1].Count - 1];
                    }
                }
            }

            public List<int> KeyDowns
            {
                get { return _keydown; }
            }

            public DateTime LastInput
            {
                get { return _last; }
            }

            public byte[] KeyboardState
            {
                get { return _state; }
            }

            public int KeyDownCount
            {
                get { return _keydown.Count; }
            }

            public string Result
            {
                get
                {
                    if (_result.Count > 0)
                    {
                        return _result[_result.Count - 1].Trim();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public string CurrentKey
            {
                get { return _key; }
            }

            public string CurrentChar
            {
                get { return _cur; }
            }

            public bool isShift
            {
                get { return _keydown.Contains(160); }
            }

            Task task = null;
            static DateTime time = DateTime.Now;

            public void Add(EventMsg msg)
            {
                #region 记录按键信息

                if (_keys.Count != 0 && (DateTime.Now - _last).TotalMilliseconds < ts)
                {
                    _keys[_keys.Count - 1].Add(msg);
                }
                else
                {
                    _keys.Clear();
                    _result.Clear();
                    _keys.Add(new List<EventMsg>());
                    _result.Add(string.Empty);
                }

                //// 首次按下按键
                //if (_keys.Count == 0)
                //{
                //    _keys.Add(new List<EventMsg>());
                //    _keys[0].Add(msg);
                //    _result.Add(string.Empty);
                //}
                //// 未释放其他按键时按下按键
                //else if (_keydown.Count > 0)
                //{
                //    _keys[_keys.Count - 1].Add(msg);
                //}
                //else if ((DateTime.Now - _last).TotalMilliseconds < ts)
                //{
                //    _keys[_keys.Count - 1].Add(msg);
                //}
                //// 重新记录输入内容
                //else
                //{
                //    _keys.Add(new List<EventMsg>());
                //    _keys[_keys.Count - 1].Add(msg);
                //    _result.Add(string.Empty);
                //}

                #endregion

                _last = DateTime.Now;

                #region 获取键盘状态

                // 记录正在按下的按键  
                if (msg.paramH == 0 && !_keydown.Contains(msg.message))
                {
                    _keydown.Add(msg.message);
                }

                // 清除已松开的按键  
                if (msg.paramH > 0 && _keydown.Contains(msg.message))
                {
                    _keydown.Remove(msg.message);
                }

                #endregion

                #region 计算按键信息

                int v = msg.message & 0xff;
                int c = msg.paramL & 0xff;
                StringBuilder strKeyName = new StringBuilder(500);
                if (GetKeyNameText(c * 65536, strKeyName, 255) > 0)
                {
                    _key = strKeyName.ToString().Trim(new char[] { ' ', '\0' });
                    GetKeyboardState(_state);
                    if (_key.Length == 1 && msg.paramH == 0)
                    {
                        // 根据键盘状态和shift缓存判断输出字符  
                        _cur = ShiftChar(_key, isShift, _state).ToString();
                        _result[_result.Count - 1] += _cur;
                    }
                    _cur = string.Empty;
                }

                #endregion

                if (msg.message == 13 && msg.paramH > 0 && !string.IsNullOrEmpty(Result))
                {
                    time = DateTime.Now.AddSeconds(-1);
                }
                else
                {
                    time = DateTime.Now;
                    if (task == null || task.IsCompleted)
                    {
                        task = Task.Run(() =>
                        {
                            while ((DateTime.Now - time).TotalMilliseconds < 100)
                            {
                                Thread.Sleep(100);
                            }
                            if (!string.IsNullOrEmpty(Result) && Result.Length > 5)
                            {
                                SetLastBarCode(Result.Trim());
                            }
                        });
                    }
                }
            }

            private char ShiftChar(string k, bool isShiftDown, byte[] state)
            {
                bool capslock = state[0x14] == 1;
                bool numlock = state[0x90] == 1;
                bool scrolllock = state[0x91] == 1;
                bool shiftdown = state[0xa0] == 1;
                char chr = (capslock ? k.ToUpper() : k.ToLower()).ToCharArray()[0];
                if (isShiftDown)
                {
                    if (chr >= 'a' && chr <= 'z')
                    {
                        chr = (char)(chr - 32);
                    }
                    else if (chr >= 'A' && chr <= 'Z')
                    {
                        if (chr == 'Z')
                        {
                            string s = "";
                        }

                        chr = (char)(chr + 32);
                    }
                    else
                    {
                        string s = "`1234567890-=[];',./";
                        string u = "~!@#$%^&*()_+{}:\"<>?";
                        if (s.IndexOf(chr) >= 0)
                        {
                            return (u.ToCharArray())[s.IndexOf(chr)];
                        }
                    }
                }

                return chr;
            }
        }

        public struct EventMsg
        {
            public int message;
            public int paramL;
            public int paramH;
            public int Time;
            public int hwnd;
        }

        private static string LastBarCode = null;
        private static object lockObj = new object();

        public static void ClearLastBarCode()
        {
            SetLastBarCode(null);
        }

        private static void SetLastBarCode(string s)
        {
            lock (lockObj)
            {
                LastBarCode = s;
            }
        }

        public static string GetLastBarCode()
        {
            lock (lockObj)
            {
                string s = LastBarCode;
                LastBarCode = null;
                return s;
            }
        }
    }
}
