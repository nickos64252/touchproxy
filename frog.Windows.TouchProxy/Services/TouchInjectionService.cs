using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using TUIO;

using frog.Windows.TouchProxy.Common;

namespace frog.Windows.TouchProxy.Services
{
	public class TouchInjectionService : BindableBase, TuioListener, IDisposable
	{
		private bool _isDisposed = false;

		public const int DEFAULT_PORT = 3333;
		public const uint MAX_CONTACTS = 256;
		public const int CONTACT_AREA_RADIUS = 24;

		private const uint TOUCH_ORIENTATION = 0;
		private const uint TOUCH_PRESSURE = 1024;
		private const int DEFAULT_WINDOWS_KEY_PRESS_TOUCH_COUNT = 5;
		private const double CALIBRATION_BUFFER_MAXLENGTH = 999999;

		private static volatile bool _isTouchInjectionSuspended = false;

		private TuioClient _tuioClient;

		private List<PointerTouchInfo> _pointerTouchInfos = new List<PointerTouchInfo>();
		
		private DispatcherTimer _refreshTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(250) };

		private int _port = DEFAULT_PORT;
		public int Port 
		{
			get { return _port; }
			set
			{
				if (IsPortValid(value))
				{
					_port = value;
				}
			}
		}



        private bool _use2DCursor = false;
        public bool Use2DCursor
        {
            get { return _use2DCursor; }
            set
            {
               _use2DCursor = value;
            }
        }

        private bool _use2DObject = false;
        public bool Use2DObject
        {
            get { return _use2DObject; }
            set
            {
                _use2DObject = value;
            }
        }

        private bool _use2DBlob = false;
        public bool Use2DBlob
        {
            get { return _use2DBlob; }
            set
            {
                _use2DBlob = value;
            }
        }


        private bool _use25DCursor = false;
        public bool Use25DCursor
        {
            get { return _use25DCursor; }
            set
            {
                _use25DCursor = value;
            }
        }

        private bool _use25DObject = false;
        public bool Use25DObject
        {
            get { return _use25DObject; }
            set
            {
                _use25DObject = value;
            }
        }

        private bool _use25DBlob = false;
        public bool Use25DBlob
        {
            get { return _use25DBlob; }
            set
            {
                _use25DBlob = value;
            }
        }

        private bool _use3DCursor = false;
        public bool Use3DCursor
        {
            get { return _use3DCursor; }
            set
            {
                _use3DCursor = value;
            }
        }

        private bool _use3DObject = false;
        public bool Use3DObject
        {
            get { return _use3DObject; }
            set
            {
                _use3DObject = value;
            }
        }

        private bool _use3DBlob = false;
        public bool Use3DBlob
        {
            get { return _use3DBlob; }
            set
            {
                _use3DBlob = value;
            }
        }


        private bool _use25DasClick = false;
        public bool Use25DasClick
        {
            get { return _use25DasClick; }
            set
            {
                _use25DasClick = value;
            }
        }

        private float _clickThreshold = 0.5f;
        public float ClickThreshold
        {
            get { return _clickThreshold; }
            set
            {
                _clickThreshold = value;
            }
        }


        public static TextConstraintPredicateDelegate IsPortValidPredicate
		{
			get
			{
				return delegate(string input)
				{
					int value = Int32.TryParse(input, out value) ? value : Int32.MinValue;
					return IsPortValid(value);
				};
			}
		}

		private static bool IsPortValid(int port)
		{
			return port.IsBetween(1, 65535);
		}

		private bool _isContactEnabled = true;
		public bool IsContactEnabled 
		{
			get { return _isContactEnabled; }
			set { _isContactEnabled = value; } 
		}

		private bool _isContactVisible = true;
		public bool IsContactVisible 
		{
			get { return _isContactVisible; }
			set { _isContactVisible = value; }  
		}

		private bool _isWindowsKeyPressEnabled = false;
		public bool IsWindowsKeyPressEnabled 
		{
			get { return _isWindowsKeyPressEnabled; }
			set { _isWindowsKeyPressEnabled = value; }  
		}

		private int _windowsKeyPressTouchCount = DEFAULT_WINDOWS_KEY_PRESS_TOUCH_COUNT;
		public int WindowsKeyPressTouchCount 
		{
			get { return _windowsKeyPressTouchCount; }
			set
			{
				if (this.WindowsKeyPressTouchCounts.Contains(value))
				{
					_windowsKeyPressTouchCount = value;
				}
			}
		}

		private List<int> _windowsKeyPressTouchCounts = new List<int> { 3, 4, 5 };
		public List<int> WindowsKeyPressTouchCounts
		{
			get { return _windowsKeyPressTouchCounts; }
		}

		private Rect _screenRect = new Rect();
		public Rect ScreenRect
		{
			get { return _screenRect; }
			set { this.SetProperty(ref _screenRect, value); }
		}

		public double CalibrationBufferMaxLength
		{
			get { return CALIBRATION_BUFFER_MAXLENGTH; }
		}

		public double CalibrationBufferMinLength
		{
			get { return -(CALIBRATION_BUFFER_MAXLENGTH); }
		}

		private double _calibrationBufferLeft = 0;
		public double CalibrationBufferLeft
		{
			get { return _calibrationBufferLeft; }
			set 
			{
				_calibrationBufferLeft = (value.IsBetween(-(CALIBRATION_BUFFER_MAXLENGTH), CALIBRATION_BUFFER_MAXLENGTH)) ? value : 0;
				SetCalibrationBuffer();
			}
		}

		private double _calibrationBufferTop = 0;
		public double CalibrationBufferTop
		{
			get { return _calibrationBufferTop; }
			set
			{
				_calibrationBufferTop = (value.IsBetween(-(CALIBRATION_BUFFER_MAXLENGTH), CALIBRATION_BUFFER_MAXLENGTH)) ? value : 0;
				SetCalibrationBuffer();
			}
		}

		private double _calibrationBufferRight = 0;
		public double CalibrationBufferRight
		{
			get { return _calibrationBufferRight; }
			set
			{
				_calibrationBufferRight = (value.IsBetween(-(CALIBRATION_BUFFER_MAXLENGTH), CALIBRATION_BUFFER_MAXLENGTH)) ? value : 0;
				SetCalibrationBuffer();
			}
		}

		private double _calibrationBufferBottom = 0;
		public double CalibrationBufferBottom
		{
			get { return _calibrationBufferBottom; }
			set
			{
				_calibrationBufferBottom = (value.IsBetween(-(CALIBRATION_BUFFER_MAXLENGTH), CALIBRATION_BUFFER_MAXLENGTH)) ? value : 0;
				SetCalibrationBuffer();
			}
		}

		private struct CalibrationBuffer
		{
			public double Left { get; private set; }
			public double Top { get; private set; }
			public double Width { get; private set; }
			public double Height { get; private set; }

			public CalibrationBuffer(double left, double top, double right, double bottom) : this()
			{
				double oX = Math.Abs(left - right);
				double oY = Math.Abs(top - bottom);

				this.Left = (left > right) ? -(oX) : oX;
				this.Top = (top > bottom) ? -(oY) : oY;
				this.Width = left + right;
				this.Height = top + bottom;
			}
		}

		private CalibrationBuffer _calibrationBuffer = new CalibrationBuffer();

		private void SetCalibrationBuffer()
		{
			_calibrationBuffer = new CalibrationBuffer
			(
				_calibrationBufferLeft, 
				_calibrationBufferTop, 
				_calibrationBufferRight, 
				_calibrationBufferBottom
			);
		}

		private bool _isEnabled = false;
		public bool IsEnabled
		{
			get { return _isEnabled; }
			set
			{
				if (this.SetProperty(ref _isEnabled, value))
				{
					if (_isEnabled)
					{
						Start();
					}
					else
					{
						Stop();
					}
					this.OnIsEnabledChanged(null);
				}
			}
		}

		public event EventHandler IsEnabledChanged;
		public virtual void OnIsEnabledChanged(EventArgs e)
		{
			if (IsEnabledChanged != null)
			{
				IsEnabledChanged(this, e);
			}
		}

		public event TouchInjectedEventHandler TouchInjected;
		public virtual void OnTouchInjected(TouchInjectedEventArgs e)
		{
			if (TouchInjected != null)
			{
				TouchInjected(this, e);
			}
		}

		public TouchInjectionService()
		{
			_refreshTimer.Tick += (s, e) => 
			{ 
				InjectPointerTouchInfos(); 
			};
		}

		~TouchInjectionService()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (!_isDisposed)
			{
				if (isDisposing) 
				{
					_tuioClient.Dispose();
				}
			}
			_isDisposed = true;
		}

		private void Start()
        {



            if (_tuioClient != null) 
			{ 
				Stop(); 
			}

			TouchInjection.Initialize(MAX_CONTACTS, this.IsContactVisible ? TouchFeedback.INDIRECT : TouchFeedback.NONE);

			_tuioClient = new TuioClient(this.Port);
			_tuioClient.addTuioListener(this);
			try
			{
				_tuioClient.connect();
			}
			catch (Exception e)
			{
				this.IsEnabled = false;
				if (e is SocketException)
				{
					SocketException se = (SocketException)e;
					MessageBox.Show(string.Format("{0}\r\n\r\nError Code: {1} ({2})", se.Message, se.ErrorCode, se.SocketErrorCode), "Error: SocketException", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void Stop()
		{
			_pointerTouchInfos.Clear();
			InjectPointerTouchInfos();

			if (_tuioClient != null)
			{
				_tuioClient.disconnect();
				_tuioClient.removeTuioListener(this);
				_tuioClient = null;
			}
		}

        private void AddTouch(int pid,  float tx,float ty)
        {
            _refreshTimer.Stop();

 
            int i = _pointerTouchInfos.FindIndex(pti => pti.PointerInfo.PointerId == pid);
            if (i != -1)
            {
                _pointerTouchInfos.RemoveAt(i);
            }

            int x = (int)((tx * (_screenRect.Width + _calibrationBuffer.Width)) + _calibrationBuffer.Left + _screenRect.Left);
            int y = (int)((ty * (_screenRect.Height + _calibrationBuffer.Height)) + _calibrationBuffer.Top + _screenRect.Top);

            _pointerTouchInfos.Add
            (
                new PointerTouchInfo()
                {
                    TouchFlags = TouchFlags.NONE,
                    Orientation = TOUCH_ORIENTATION,
                    Pressure = TOUCH_PRESSURE,
                    TouchMasks = TouchMask.CONTACTAREA | TouchMask.ORIENTATION | TouchMask.PRESSURE,
                    PointerInfo = new PointerInfo
                    {
                        PointerInputType = PointerInputType.TOUCH,
                        PointerFlags = PointerFlags.DOWN | PointerFlags.INRANGE | ((this.IsContactEnabled) ? PointerFlags.INCONTACT : PointerFlags.NONE),
                        PtPixelLocation = new PointerTouchPoint { X = x, Y = y },
                        PointerId = (uint)pid
                    },
                    ContactArea = new ContactArea
                    {
                        Left = x - CONTACT_AREA_RADIUS,
                        Right = x + CONTACT_AREA_RADIUS,
                        Top = y - CONTACT_AREA_RADIUS,
                        Bottom = y + CONTACT_AREA_RADIUS
                    }
                }
            );

        }

        private void UpdateTouch(int pid, float tx, float ty)
        {
            _refreshTimer.Stop();

            int i = _pointerTouchInfos.FindIndex(pti => pti.PointerInfo.PointerId == pid);
            if (i != -1)
            {
                int x = (int)((tx * (_screenRect.Width + _calibrationBuffer.Width)) + _calibrationBuffer.Left + _screenRect.Left);
                int y = (int)((ty * (_screenRect.Height + _calibrationBuffer.Height)) + _calibrationBuffer.Top + _screenRect.Top);

                PointerTouchInfo pointerTouchInfo = _pointerTouchInfos[i];
                pointerTouchInfo.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE | ((this.IsContactEnabled) ? PointerFlags.INCONTACT : PointerFlags.NONE);
                pointerTouchInfo.PointerInfo.PtPixelLocation = new PointerTouchPoint { X = x, Y = y };
                pointerTouchInfo.ContactArea = new ContactArea
                {
                    Left = x - CONTACT_AREA_RADIUS,
                    Right = x + CONTACT_AREA_RADIUS,
                    Top = y - CONTACT_AREA_RADIUS,
                    Bottom = y + CONTACT_AREA_RADIUS
                };
                _pointerTouchInfos[i] = pointerTouchInfo;

            }
        }

        private bool RemoveTouch(int pid)
        {
            _refreshTimer.Stop();
            
            int i = _pointerTouchInfos.FindIndex(pti => pti.PointerInfo.PointerId == pid);
            if (i != -1)
            {
                PointerTouchInfo pointerTouchInfo = _pointerTouchInfos[i];
                pointerTouchInfo.PointerInfo.PointerFlags = PointerFlags.UP;
                _pointerTouchInfos[i] = pointerTouchInfo;

                return true;
            }
            return false;
        }



        public void addTuioCursor(TuioCursor tuioCursor)
		{
            if(_use2DCursor)
            {
                AddTouch(tuioCursor.CursorID,  tuioCursor.X, tuioCursor.Y);
                Trace.WriteLine(string.Format("add 2Dcur {0} ({1}) {2} {3}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y), "TUIO");

            }
        }

		public void updateTuioCursor(TuioCursor tuioCursor)
		{
            if (_use2DCursor)
            {
                UpdateTouch(tuioCursor.CursorID, tuioCursor.X, tuioCursor.Y);

                Trace.WriteLine(string.Format("set 2Dcur {0} ({1}) {2} {3} {4} {5}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y, tuioCursor.MotionSpeed, tuioCursor.MotionAccel), "TUIO");

            }
		}

		public void removeTuioCursor(TuioCursor tuioCursor)
		{
            if (_use2DCursor)
            {
                if(RemoveTouch(tuioCursor.CursorID))
                    Trace.WriteLine(string.Format("del 2Dcur {0} ({1})", tuioCursor.CursorID, tuioCursor.SessionID), "TUIO");
            }
		}

		public void addTuioObject(TuioObject tuioObject)
        {
            if (_use2DObject)
            {
                AddTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);
                Trace.WriteLine(string.Format("add 2Dobj {0} ({1}) {2} {3}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y), "TUIO");

            }
        }

		public void updateTuioObject(TuioObject tuioObject)
        {
            if (_use2DObject)
            {
                UpdateTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);

                Trace.WriteLine(string.Format("set 2Dobj {0} ({1}) {2} {3} {4} {5}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y, tuioObject.MotionSpeed, tuioObject.MotionAccel), "TUIO");

            }
        }

		public void removeTuioObject(TuioObject tuioObject)
        {
            if (_use2DObject)
            {
                if (RemoveTouch(tuioObject.SymbolID))
                    Trace.WriteLine(string.Format("del 2Dobj {0} ({1})", tuioObject.SymbolID, tuioObject.SessionID), "TUIO");
            }
        }

        public void addTuioBlob(TuioBlob tuioBlob)
        {
            if (_use2DBlob)
            {
                AddTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);
                Trace.WriteLine(string.Format("add 2Dblb {0} ({1}) {2} {3}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y), "TUIO");

            }
        }

        public void updateTuioBlob(TuioBlob tuioBlob)
        {
            if (_use2DBlob)
            {
                UpdateTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);

                Trace.WriteLine(string.Format("set 2Dblb {0} ({1}) {2} {3} {4} {5}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y, tuioBlob.MotionSpeed, tuioBlob.MotionAccel), "TUIO");

            }
        }

        public void removeTuioBlob(TuioBlob tuioBlob)
        {
            if (_use2DBlob)
            {
                if (RemoveTouch(tuioBlob.BlobID))
                    Trace.WriteLine(string.Format("del 2Dblb {0} ({1})", tuioBlob.BlobID, tuioBlob.SessionID), "TUIO");
            }
        }



        public void addTuioCursor25D(TuioCursor25D tuioCursor)
        {
            if (_use25DCursor)
            {
                AddTouch(tuioCursor.CursorID, tuioCursor.X, tuioCursor.Y);
                Trace.WriteLine(string.Format("add 25Dcur {0} ({1}) {2} {3}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y), "TUIO");

            }
        }

        public void updateTuioCursor25D(TuioCursor25D tuioCursor)
        {
            if (_use25DCursor)
            {
                UpdateTouch(tuioCursor.CursorID, tuioCursor.X, tuioCursor.Y);

                if (_use25DasClick)
                {

                    int x = (int)((tuioCursor.X * (_screenRect.Width + _calibrationBuffer.Width)) + _calibrationBuffer.Left + _screenRect.Left);
                    int y = (int)((tuioCursor.Y * (_screenRect.Height + _calibrationBuffer.Height)) + _calibrationBuffer.Top + _screenRect.Top);

                    ControlMouseRightButton(x,y,tuioCursor.Z);
                }

                Trace.WriteLine(string.Format("set 25Dcur {0} ({1}) {2} {3} {4} {5}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y, tuioCursor.MotionSpeed, tuioCursor.MotionAccel), "TUIO");

            }
        }

        public void removeTuioCursor25D(TuioCursor25D tuioCursor)
        {
            if (_use25DCursor)
            {
                if (RemoveTouch(tuioCursor.CursorID))
                    Trace.WriteLine(string.Format("del 25Dcur {0} ({1})", tuioCursor.CursorID, tuioCursor.SessionID), "TUIO");
            }
        }

        public void addTuioObject25D(TuioObject25D tuioObject)
        {
            if (_use25DObject)
            {
                AddTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);
                Trace.WriteLine(string.Format("add 25Dobj {0} ({1}) {2} {3}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y), "TUIO");

            }
        }

        public void updateTuioObject25D(TuioObject25D tuioObject)
        {
            if (_use25DObject)
            {
                UpdateTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);

                if (_use25DasClick)
                {

                    int x = (int)((tuioObject.X * (_screenRect.Width + _calibrationBuffer.Width)) + _calibrationBuffer.Left + _screenRect.Left);
                    int y = (int)((tuioObject.Y * (_screenRect.Height + _calibrationBuffer.Height)) + _calibrationBuffer.Top + _screenRect.Top);

                    ControlMouseRightButton(x, y, tuioObject.Z);
                }

                Trace.WriteLine(string.Format("set 25Dobj {0} ({1}) {2} {3} {4} {5}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y, tuioObject.MotionSpeed, tuioObject.MotionAccel), "TUIO");

            }
        }

        public void removeTuioObject25D(TuioObject25D tuioObject)
        {
            if (_use25DObject)
            {
                if (RemoveTouch(tuioObject.SymbolID))
                    Trace.WriteLine(string.Format("del 25Dobj {0} ({1})", tuioObject.SymbolID, tuioObject.SessionID), "TUIO");
            }
        }

        public void addTuioBlob25D(TuioBlob25D tuioBlob)
        {
            if (_use25DBlob)
            {
                AddTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);
                Trace.WriteLine(string.Format("add 25Dblb {0} ({1}) {2} {3}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y), "TUIO");

            }
        }

        public void updateTuioBlob25D(TuioBlob25D tuioBlob)
        {
            if (_use25DBlob)
            {
                UpdateTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);

                if (_use25DasClick)
                {

                    int x = (int)((tuioBlob.X * (_screenRect.Width + _calibrationBuffer.Width)) + _calibrationBuffer.Left + _screenRect.Left);
                    int y = (int)((tuioBlob.Y * (_screenRect.Height + _calibrationBuffer.Height)) + _calibrationBuffer.Top + _screenRect.Top);

                    ControlMouseRightButton(x, y, tuioBlob.Z);
                }

                Trace.WriteLine(string.Format("set 25Dblb {0} ({1}) {2} {3} {4} {5}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y, tuioBlob.MotionSpeed, tuioBlob.MotionAccel), "TUIO");

            }
        }

        public void removeTuioBlob25D(TuioBlob25D tuioBlob)
        {
            if (_use25DBlob)
            {
                if (RemoveTouch(tuioBlob.BlobID))
                    Trace.WriteLine(string.Format("del 25Dblb {0} ({1})", tuioBlob.BlobID, tuioBlob.SessionID), "TUIO");
            }
        }



        public void addTuioCursor3D(TuioCursor3D tuioCursor)
        {
            if (_use3DCursor)
            {
                AddTouch(tuioCursor.CursorID, tuioCursor.X, tuioCursor.Y);
                Trace.WriteLine(string.Format("add 3Dcur {0} ({1}) {2} {3}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y), "TUIO");

            }
        }

        public void updateTuioCursor3D(TuioCursor3D tuioCursor)
        {
            if (_use3DCursor)
            {
                UpdateTouch(tuioCursor.CursorID, tuioCursor.X, tuioCursor.Y);

                Trace.WriteLine(string.Format("set 3Dcur {0} ({1}) {2} {3} {4} {5}", tuioCursor.CursorID, tuioCursor.SessionID, tuioCursor.X, tuioCursor.Y, tuioCursor.MotionSpeed, tuioCursor.MotionAccel), "TUIO");

            }
        }

        public void removeTuioCursor3D(TuioCursor3D tuioCursor)
        {
            if (_use3DCursor)
            {
                if (RemoveTouch(tuioCursor.CursorID))
                    Trace.WriteLine(string.Format("del 3Dcur {0} ({1})", tuioCursor.CursorID, tuioCursor.SessionID), "TUIO");
            }
        }

        public void addTuioObject3D(TuioObject3D tuioObject)
        {
            if (_use3DObject)
            {
                AddTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);
                Trace.WriteLine(string.Format("add 3Dobj {0} ({1}) {2} {3}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y), "TUIO");

            }
        }

        public void updateTuioObject3D(TuioObject3D tuioObject)
        {
            if (_use3DObject)
            {
                UpdateTouch(tuioObject.SymbolID, tuioObject.X, tuioObject.Y);

                Trace.WriteLine(string.Format("set 3Dobj {0} ({1}) {2} {3} {4} {5}", tuioObject.SymbolID, tuioObject.SessionID, tuioObject.X, tuioObject.Y, tuioObject.MotionSpeed, tuioObject.MotionAccel), "TUIO");

            }
        }

        public void removeTuioObject3D(TuioObject3D tuioObject)
        {
            if (_use3DObject)
            {
                if (RemoveTouch(tuioObject.SymbolID))
                    Trace.WriteLine(string.Format("del 3Dobj {0} ({1})", tuioObject.SymbolID, tuioObject.SessionID), "TUIO");
            }
        }

        public void addTuioBlob3D(TuioBlob3D tuioBlob)
        {
            if (_use3DBlob)
            {
                AddTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);
                Trace.WriteLine(string.Format("add 3Dblb {0} ({1}) {2} {3}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y), "TUIO");

            }
        }

        public void updateTuioBlob3D(TuioBlob3D tuioBlob)
        {
            if (_use3DBlob)
            {
                UpdateTouch(tuioBlob.BlobID, tuioBlob.X, tuioBlob.Y);

                Trace.WriteLine(string.Format("set 3Dblb {0} ({1}) {2} {3} {4} {5}", tuioBlob.BlobID, tuioBlob.SessionID, tuioBlob.X, tuioBlob.Y, tuioBlob.MotionSpeed, tuioBlob.MotionAccel), "TUIO");

            }
        }

        public void removeTuioBlob3D(TuioBlob3D tuioBlob)
        {
            if (_use3DBlob)
            {
                if (RemoveTouch(tuioBlob.BlobID))
                    Trace.WriteLine(string.Format("del 3Dblb {0} ({1})", tuioBlob.BlobID, tuioBlob.SessionID), "TUIO");
            }
        }


        


        public void refresh(TuioTime frameTime)
		{
			Trace.WriteLine(string.Format("refresh {0}", frameTime.TotalMilliseconds), "TUIO");

			_refreshTimer.Stop();

			if (this.IsContactEnabled && this.IsWindowsKeyPressEnabled)
			{
				if (_pointerTouchInfos.Count.Equals(this.WindowsKeyPressTouchCount))
				{
#pragma warning disable 4014
					InjectWindowsKeyPress();
#pragma warning restore 4014
					return;
				}
			}

			InjectPointerTouchInfos();

			if (_pointerTouchInfos.Count > 0)
			{
				for (int i = _pointerTouchInfos.Count - 1; i >= 0; i--)
				{
					if (_pointerTouchInfos[i].PointerInfo.PointerFlags.HasFlag(PointerFlags.UP))
					{
						_pointerTouchInfos.RemoveAt(i);
					}
				}

				if (_pointerTouchInfos.Count > 0)
				{
					for (int i = 0, ic = _pointerTouchInfos.Count; i < ic; i++)
					{
						if (_pointerTouchInfos[i].PointerInfo.PointerFlags.HasFlag(PointerFlags.DOWN))
						{
							PointerTouchInfo pointerTouchInfo = _pointerTouchInfos[i];
							pointerTouchInfo.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE | ((this.IsContactEnabled) ? PointerFlags.INCONTACT : PointerFlags.NONE);
							_pointerTouchInfos[i] = pointerTouchInfo;
						}
					}

					_refreshTimer.Start();
				}
			}	
		}




        private void ControlMouseRightButton(int x, int y,float value)
        {
   
            if (!_isTouchInjectionSuspended)
            {
                // Clic where the touch is
                KeyboardInjection.SendMouse((value > _clickThreshold)? MouseEventFlags.MOUSEEVENTF_LEFTDOWN : MouseEventFlags.MOUSEEVENTF_LEFTUP| MouseEventFlags.MOUSEEVENTF_MOVE , x,y,0);
            }

        }


        private void InjectPointerTouchInfos()
		{
			PointerTouchInfo[] pointerTouchInfos = _pointerTouchInfos.ToArray();

			if (pointerTouchInfos.Length == 0)
			{
				_refreshTimer.Stop();
			}

			if (!_isTouchInjectionSuspended)
			{
				TouchInjection.Send(pointerTouchInfos);
				this.OnTouchInjected(new TouchInjectedEventArgs(pointerTouchInfos));
			}
		}

		private async Task InjectWindowsKeyPress()
		{
			if (!_isTouchInjectionSuspended)
			{
				_isTouchInjectionSuspended = true;

				_pointerTouchInfos.Clear();
				InjectPointerTouchInfos();

				KeyboardInjection.Send(VirtualKeyCode.LWIN,true);
                KeyboardInjection.Send(VirtualKeyCode.LWIN, false);

                await Task.Delay(TimeSpan.FromMilliseconds(750));

				_isTouchInjectionSuspended = false;

				_pointerTouchInfos.Clear();
				InjectPointerTouchInfos();
			}
		}
	}
}
