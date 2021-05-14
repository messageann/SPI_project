using DataModule.Exceptions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataModule.Models
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public class LogInfo : INotifyPropertyChanged
	{
		#region CONSTS
		internal const int BYTES_ID = 4;
		internal const int BYTES_ATTRIBUTES = 2;
		internal const int BYTES_NAME = 48;
		internal const int BYTES_DESCR = 114;
		internal const int BYTES_DATE = 8;
		internal const int BYTES_CLOGIN = 112;
		internal const int BYTES_CPASS = 128;
		internal const int BYTES_HMAC = 32;

		internal const int BYTES_LOGINFO = 0
			+ BYTES_ID
			+ BYTES_ATTRIBUTES
			+ BYTES_NAME
			+ BYTES_CLOGIN
			+ BYTES_CPASS
			+ BYTES_DESCR
			+ BYTES_DATE
			+ BYTES_HMAC
			;
		internal const int BLOCKS_PER = BYTES_LOGINFO / CryptService.BLOCK_SIZE;
		#endregion //CONSTS

		#region BODY FIELDS
		private UInt32 _id;
		private LogInfoAttributes _attributes;
		private string _name;
		private string _descr;
		private DateTime _date;
		private byte[] _cLogin;
		private byte[] _cPass;
		private byte[] _hmac;
		#endregion //BODY FIELDS

		#region BODY PROPS
		public UInt32 ID
		{
			get => _id;
			internal set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_id = value;
				//NotifyPropertyChanged();
			}
		}
		public LogInfoAttributes Attributes
		{
			get => _attributes;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_attributes = value;
				NotifyPropertyChanged();
			}
		}
		public string Name
		{
			get => _name;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_name = value;
				NotifyPropertyChanged();
			}
		}
		public string Description
		{
			get => _descr;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_descr = value;
				NotifyPropertyChanged();
			}
		}
		public DateTime DateCreated
		{
			get => _date;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_date = value;
				NotifyPropertyChanged();
			}
		}
		internal byte[] CryptedLogin
		{
			get => _cLogin;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_cLogin = value;
			}
		}
		internal byte[] CryptedPass
		{
			get => _cPass;
			set
			{
				if (_isInited)
				{
					throw new InitModeException();
				}
				_cPass = value;
			}
		}
		internal byte[] HMAC
		{
			get => _hmac;
			set
			{
				if (_isInited) throw new InitModeException();
				_hmac = value;
			}
		} 
		#endregion //BODY PROPS

		#region APP FIELDS
		internal long FilePos;
		private bool _isInited = false;
		internal byte[] _key = CryptService.Empty256Bits;
		private bool _hasKey = false;
		#endregion //APP FIELDS

		#region APP PROPS
		public bool IsInited
		{
			get => _isInited;
			set
			{
				_isInited = value;
				NotifyPropertyChanged();
			}
		}
		internal byte[] Key
		{
			get => _key;
			set
			{
				_key = value;
				HasKey = _key != CryptService.Empty256Bits;
			}
		}
		public bool HasKey
		{
			get => _hasKey;
			private set
			{
				_hasKey = value;
				NotifyPropertyChanged();
			}
		}
		#endregion //APP PROPS

		#region CTOR'S
		public LogInfo(UInt32 id, LogInfoAttributes attributes, string name, string descr, DateTime date, byte[] clogin, byte[] cpass, byte[] hmac) : this(0, id, attributes, name, descr, date, clogin, cpass, hmac) { }
		public LogInfo(long filepos, UInt32 id, LogInfoAttributes attributes, string name, string descr, DateTime date, byte[] clogin, byte[] cpass, byte[] hmac)
		{
			FilePos = filepos;
			_id = id;
			_attributes = attributes;
			_name = name;
			_descr = descr;
			_date = date;
			_cLogin = clogin;
			_cPass = cpass;
			_hmac = hmac;
			IsInited = true;
		}
		#endregion //CTOR'S

		#region NOTIFS
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion //NOTIFS

		#region COPY
		public LogInfo ShallowCopy()
		{
			LogInfo res = new(FilePos, _id, _attributes, _name, _descr, _date, _cLogin, _cPass, _hmac);
			res.Key = this.Key;
			return res;
		}

		public void ShallowCopyTo(LogInfo dest)
		{
			dest.FilePos = this.FilePos;
			dest.ID = this._id;
			dest.Attributes = this._attributes;
			dest.Name = this._name;
			dest.Description = this._descr;
			dest.DateCreated = this._date;
			dest.CryptedLogin = this._cLogin;
			dest.CryptedPass = this._cPass;
			dest._hmac = this._hmac;
			dest.Key = this.Key;
		}
		#endregion //COPY

		public void ClearCache()
		{
			CryptService.ClearBytes(this._key);
			Key = CryptService.Empty256Bits;
		}

		internal static readonly byte[] EmptyLogInfo = new byte[BYTES_LOGINFO];
	}

	public enum LogInfoAttributes : UInt16
	{
		None = 0,
		L1Crypt = 1 << 0,
		L2Crypt = 1 << 1,
		L3Crypt = 1 << 2
	}
}
