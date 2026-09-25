using Caliburn.Micro;

namespace QA.Business.Message
{
    public class RobotRealStateInfo : PropertyChangedBase
    {
        private float _x1axis;
        public float X1axis
        {
            get => _x1axis;
            set
            {
                _x1axis = value;
                NotifyOfPropertyChange(() => X1axis);
            }
        }

        private float _y1axis;
        public float Y1axis
        {
            get => _y1axis;
            set
            {
                _y1axis = value;
                NotifyOfPropertyChange(() => Y1axis);
            }
        }

        private float _x2axis;
        public float X2axis
        {
            get => _x2axis;
            set
            {
                _x2axis = value;
                NotifyOfPropertyChange(() => X2axis);
            }
        }

        private float _y2axis;
        public float Y2axis
        {
            get => _y2axis;
            set
            {
                _y2axis = value;
                NotifyOfPropertyChange(() => Y2axis);
            }
        }

        private float _z2axis;
        public float Z2axis
        {
            get => _z2axis;
            set
            {
                _z2axis = value;
                NotifyOfPropertyChange(() => Z2axis);
            }
        }

        private float _r1axis;
        public float R1axis
        {
            get => _r1axis;
            set
            {
                _r1axis = value;
                NotifyOfPropertyChange(() => R1axis);
            }
        }

        private float _r2axis;
        public float R2axis
        {
            get => _r2axis;
            set
            {
                _r2axis = value;
                NotifyOfPropertyChange(() => R2axis);
            }
        }

        private float _r3axis;
        public float R3axis
        {
            get => _r3axis;
            set
            {
                _r3axis = value;
                NotifyOfPropertyChange(() => R3axis);
            }
        }

        private float _r4axis;
        public float R4axis
        {
            get => _r4axis;
            set
            {
                _r4axis = value;
                NotifyOfPropertyChange(() => R4axis);
            }
        }

        private ushort _eoutput;
        public ushort EOutput
        {
            get => _eoutput;
            set
            {
                _eoutput = value;
                NotifyOfPropertyChange(() => EOutput);
            }
        }

        private ushort _einput;
        public ushort Einput
        {
            get => _einput;
            set
            {
                _einput = value;
                NotifyOfPropertyChange(() => Einput);
            }
        }


        private byte _einput0;
        public byte Einput0
        {
            get => _einput0;
            set
            {
                _einput0 = value;
                NotifyOfPropertyChange(() => Einput0);
            }
        }

        private byte _einput1;
        public byte Einput1
        {
            get => _einput1;
            set
            {
                _einput1 = value;
                NotifyOfPropertyChange(() => Einput1);
            }
        }

        private byte _mInput;
        public byte Minput
        {
            get => _mInput;
            set
            {
                _mInput = value;
                NotifyOfPropertyChange(() => Minput);
            }
        }
        private bool _isNeedResetAlarm = false;
        public bool IsNeedResetAlarm
        {
            get { return _isNeedResetAlarm; }
            set
            {
                _isNeedResetAlarm = value;
                NotifyOfPropertyChange(() => IsNeedResetAlarm);
            }
        }

        private bool _isServoAlarm = false;
        public bool IsServoAlarm
        {
            get { return _isServoAlarm; }
            set
            {
                _isServoAlarm = value;
                NotifyOfPropertyChange(() => IsServoAlarm);
            }
        }

        private bool _isOtherAlarm = false;
        public bool IsOtherAlarm
        {
            get { return _isOtherAlarm; }
            set
            {
                _isOtherAlarm = value;
                NotifyOfPropertyChange(() => IsOtherAlarm);
            }
        }

        private bool _isOtherError = false;
        public bool IsOtherError
        {
            get { return _isOtherError; }
            set
            {
                _isOtherError = value;
                NotifyOfPropertyChange(() => IsOtherError);
            }
        }

    }
}
