using DataModule;
using DataModule.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPFCoreEx.Bases;

namespace UI.ViewModels
{
	public class EncoderWindowVM : INotifyPropertyChanged
	{
		private readonly DataService _ds;

		public EncoderWindowVM(DataService ds)
		{
			_ds = ds;
		}

		#region PROPS

		private string _accountName;
		public string AccountName
		{
			get => _accountName;
			set
			{
				_accountName = value;
				NotifyPropertyChanged();
			}
		}

		#region FOLDERINFO'S
		public ReadOnlyObservableCollection<FolderInfo> Folders => _ds.Folders;
		private FolderInfo _selectedFolder = null;
		public FolderInfo SelectedFolder
		{
			get => _selectedFolder;
			set
			{
				if (_selectedFolder != null)
				{
					if (EditModeFolder == EditMode.Edit)
					{
						CancelEditModeFolderInfoCommand.Execute(null);
					}
					_selectedFolder.ClearCache();
					FolderPass = string.Empty;
				}
				_selectedFolder = value;
				if (_selectedFolder != null && !_selectedFolder.IsCrypted)
				{
					_ds.ReadFolderInfoContent(_selectedFolder);
					IsContentReady = true;
				}
				else
				{
					IsContentReady = false;
				}
				NotifyPropertyChanged();
			}
		}

		private int _selectedFolderIndex = -1;
		public int SelectedFolderIndex
		{
			get => _selectedFolderIndex;
			set
			{
				_selectedFolderIndex = value;
				NotifyPropertyChanged();
			}
		}

		private bool _isContentReady = false;
		public bool IsContentReady
		{
			get => _isContentReady;
			private set
			{
				_isContentReady = value;
				NotifyPropertyChanged();
			}
		}

		private EditMode _editModeFolder = EditMode.None;
		public EditMode EditModeFolder
		{
			get => _editModeFolder;
			set
			{
				_editModeFolder = value;
				NotifyPropertyChanged();
			}
		}

		private string _folderPass;
		public string FolderPass
		{
			get => _folderPass;
			set
			{
				_folderPass = value;
				NotifyPropertyChanged();
			}
		}
		#endregion //FOLDERINFO'S

		#region LOGINFO'S
		private LogInfo _selectedLogInfo;
		public LogInfo SelectedLogInfo
		{
			get => _selectedLogInfo;
			set
			{
				_selectedLogInfo = value;
				NotifyPropertyChanged();
			}
		}

		private int _selectedLogInfoIndex = -1;
		public int SelectedLogInfoIndex
		{
			get => _selectedLogInfoIndex;
			set
			{
				if (_selectedLogInfoIndex >= 0)
				{
					_selectedLogInfo.ClearCache();
				}
				_selectedLogInfoIndex = value;
				if (this.EditModeLogInfo == EditMode.Edit)
				{
					CancelEditModeLogInfoCommand.Execute(null);
				}
				LogInfoPass = string.Empty;
				LogInfoDLogin = string.Empty;
				LogInfoDPass = string.Empty;
				NotifyPropertyChanged();
			}
		}

		private string _loginfoPass;
		public string LogInfoPass
		{
			get => _loginfoPass;
			set
			{
				_loginfoPass = value;
				NotifyPropertyChanged();
			}
		}

		private string _loginfoDPass;
		public string LogInfoDPass
		{
			get => _loginfoDPass;
			set
			{
				_loginfoDPass = value;
				NotifyPropertyChanged();
			}
		}

		private string _loginfoDLogin;
		public string LogInfoDLogin
		{
			get => _loginfoDLogin;
			set
			{
				_loginfoDLogin = value;
				NotifyPropertyChanged();
			}
		}

		private EditMode _editModeLogInfo = EditMode.None;
		public EditMode EditModeLogInfo
		{
			get => _editModeLogInfo;
			set
			{
				_editModeLogInfo = value;
				NotifyPropertyChanged();
			}
		}
		#endregion //LOGINFO'S

		#endregion //PROPS

		#region FIELDS
		#endregion //FIELDS

		#region COMMANDS

		#region EDIT MODE FOLDERINFO
		private RelayCommand _beginEditFolderInfoCommand;
		public RelayCommand BeginEditFolderInfoCommand
		{
			get
			{
				if (_beginEditFolderInfoCommand == null)
				{
					_beginEditFolderInfoCommand = new((o) =>
					{
						EditModeFolder = EditMode.Edit;
						_ds.BeginEditFolderInfoBody(_selectedFolderIndex);
					});
				}
				return _beginEditFolderInfoCommand;
			}
		}

		private RelayCommand _preAddFolderInfoCommand;
		public RelayCommand PreAddFolderInfoCommand
		{
			get
			{
				if (_preAddFolderInfoCommand == null)
				{
					_preAddFolderInfoCommand = new((o) =>
					{
						EditModeFolder = EditMode.Preadd;
						_ds.PreaddFolderInfo();
						SelectedFolderIndex = 0;
					});
				}
				return _preAddFolderInfoCommand;
			}
		}

		private RelayCommand _cancelEditModeFolderInfoCommand;
		public RelayCommand CancelEditModeFolderInfoCommand
		{
			get
			{
				if (_cancelEditModeFolderInfoCommand == null)
				{
					_cancelEditModeFolderInfoCommand = new((o) =>
					{
						if (_editModeFolder == EditMode.Preadd)
						{
							SelectedFolderIndex = -1;
							_ds.CancelPreaddFolderInfo();
						}
						else if (_editModeFolder == EditMode.Edit)
						{
							_ds.CancelEditFolderInfoBody();
						}
						EditModeFolder = EditMode.None;
					});
				}
				return _cancelEditModeFolderInfoCommand;
			}
		}

		private RelayCommand _saveEditableFolderInfoCommand;
		public RelayCommand SaveEditableFolderInfoCommand
		{
			get
			{
				if (_saveEditableFolderInfoCommand == null)
				{
					_saveEditableFolderInfoCommand = new((o) =>
					{
						_ds.EndEditFolderInfoBody(this._folderPass);
						EditModeFolder = EditMode.None;
						FolderPass = string.Empty;
					});
				}
				return _saveEditableFolderInfoCommand;
			}
		}
		#endregion //EDIT MODE FOLDERINFO

		#region EDIT MODE LOGINFO
		private RelayCommand _beginEditLogInfoCommand;
		public RelayCommand BeginEditLogInfoCommand
		{
			get
			{
				if (_beginEditLogInfoCommand == null)
				{
					_beginEditLogInfoCommand = new((o) =>
					{
						EditModeLogInfo = EditMode.Edit;
						_ds.BeginEditLogInfo(_selectedFolder, _selectedLogInfoIndex, isNew: false);
					});
				}
				return _beginEditLogInfoCommand;
			}
		}

		private RelayCommand _preAddLogInfoCommand;
		public RelayCommand PreAddLogInfoCommand
		{
			get
			{
				if (_preAddLogInfoCommand == null)
				{
					_preAddLogInfoCommand = new((o) =>
					{
						EditModeLogInfo = EditMode.Preadd;
						_ds.PreaddLogInfo(_selectedFolder);
						SelectedLogInfoIndex = 0;
					});
				}
				return _preAddLogInfoCommand;
			}
		}

		private RelayCommand _cancelEditModeLogInfoCommand;
		public RelayCommand CancelEditModeLogInfoCommand
		{
			get
			{
				if (_cancelEditModeLogInfoCommand == null)
				{
					_cancelEditModeLogInfoCommand = new((o) =>
					{
						if (_editModeLogInfo == EditMode.Preadd)
						{
							SelectedLogInfoIndex = -1;
							_ds.CancelPreaddLogInfo();
						}
						else if (_editModeFolder == EditMode.Edit)
						{
							_ds.CancelEditLogInfo();
						}
						EditModeLogInfo = EditMode.None;
					});
				}
				return _cancelEditModeLogInfoCommand;
			}
		}

		private RelayCommand _saveEditableLogInfoCommand;
		public RelayCommand SaveEditableLogInfoCommand
		{
			get
			{
				if (_saveEditableLogInfoCommand == null)
				{
					_saveEditableLogInfoCommand = new((o) =>
					{
						if (string.IsNullOrEmpty(LogInfoPass))
						{
							MessageBox.Show("Enter key!");
						}
						else if (string.IsNullOrEmpty(SelectedLogInfo.Name))
						{
							MessageBox.Show("Enter name!");
						}
						else if (string.IsNullOrEmpty(LogInfoDLogin))
						{
							MessageBox.Show("Enter login!");
						}
						else if (string.IsNullOrEmpty(LogInfoDPass))
						{
							MessageBox.Show("Enter password!");
						}
						else
						{
							_ds.EndEditLogInfo(this._loginfoPass, this._loginfoDLogin, this._loginfoDPass);
							_selectedLogInfo.ClearCache();
							EditModeLogInfo = EditMode.None;
							LogInfoPass = string.Empty;
							LogInfoDLogin = string.Empty;
							LogInfoDPass = string.Empty;
						}
					});
				}
				return _saveEditableLogInfoCommand;
			}
		}
		#endregion //EDIT MODE LOGINFO

		#region LOCKS
		private RelayCommand _unlockFolderCommand;
		public RelayCommand UnlockFolderCommand
		{
			get
			{
				if (_unlockFolderCommand == null)
				{
					_unlockFolderCommand = new((o) =>
					{
						if (!_ds.TryReadFolderInfoContent(_selectedFolder, this._folderPass))
						{
							MessageBox.Show("Wrong folder key!");
						}
						else
						{
							FolderPass = string.Empty;
							IsContentReady = true;
						}
					});
				}
				return _unlockFolderCommand;
			}
		}

		private RelayCommand _toggleLockLogInfoCommand;
		public RelayCommand ToggleLockLogInfoCommand
		{
			get
			{
				if (_toggleLockLogInfoCommand == null)
				{
					_toggleLockLogInfoCommand = new((o) =>
					{
						if (_selectedLogInfo.IsInited)
						{
							if (_selectedLogInfo.HasKey)
							{
								SelectedLogInfoIndex = -1;
							}
							else if (_ds.TryReadLogInfoContent(_selectedLogInfo, _loginfoPass, out var login, out var pass))
							{
								LogInfoDLogin = login;
								LogInfoDPass = pass;
							}
							else
							{
								MessageBox.Show("Bad key!");
							}
						}
						else
						{
							SaveEditableLogInfoCommand.Execute(null);
						}
					});
				}
				return _toggleLockLogInfoCommand;
			}
		}
		#endregion //LOCKS

		#region REMOVE
		private RelayCommand _removeFolderInfoCommand;
		public RelayCommand RemoveFolderInfoCommand
		{
			get
			{
				if (_removeFolderInfoCommand == null)
				{
					_removeFolderInfoCommand = new((o) =>
					{
						var t = _selectedFolderIndex;
						SelectedFolderIndex = -1;
						_ds.RemoveFolderInfo(t);
					});
				}
				return _removeFolderInfoCommand;
			}
		}

		private RelayCommand _removeLogInfoCommand;
		public RelayCommand RemoveLogInfoCommand
		{
			get
			{
				if(_removeLogInfoCommand == null)
				{
					_removeLogInfoCommand = new((o) =>
					{
						var t = _selectedLogInfoIndex;
						SelectedLogInfoIndex = -1;
						_ds.RemoveLogInfo(_selectedFolder, t);
					});
				}
				return _removeLogInfoCommand;
			}
		}
		#endregion //REMOVE

		private RelayCommand<string> _encryptFileCommand;
		public RelayCommand<string> EncryptFileCommand
		{
			get
			{
				if(_encryptFileCommand == null)
				{
					_encryptFileCommand = new((f) =>
					{
						if (f.EndsWith(".enc"))
						{
							_ds.DecryptFile(f);
						}
						else
						{
							_ds.EncryptFile(f);
						}
					});
				}
				return _encryptFileCommand;
			}
		}
		#endregion //COMMANDS

		#region NOTIFS
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion //NOTIFS
	}

	public enum EditMode : byte
	{
		None = 0,
		Preadd = 1 << 0,
		Edit = 1 << 1
	}
}
