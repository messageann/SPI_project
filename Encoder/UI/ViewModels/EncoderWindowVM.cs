using DataModule;
using DataModule.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
		public string Title => "Encoder";
		public ReadOnlyObservableCollection<FolderInfo> Folders => _ds.Folders;

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

		private EditMode _editMode = EditMode.None;
		public EditMode EditMode
		{
			get => _editMode;
			set
			{
				_editMode = value;
				NotifyPropertyChanged();
			}
		}

		private int _selectedFolderIndex;
		public int SelectedFolderIndex
		{
			get => _selectedFolderIndex;
			set
			{
				if (_selectedFolderIndex >= 0)
				{
					Folders[_selectedFolderIndex].ClearCache();
				}
				_selectedFolderIndex = value;
				if (this.EditMode == EditMode.Edit)
				{
					CancelEditModeFolderInfoCommand.Execute(null);
				}
				FolderPass = string.Empty;
				NotifyPropertyChanged();
			}
		}
		#endregion //PROPS

		#region FIELDS
		#endregion //FIELDS

		#region COMMANDS

		#region Edit mode FolderInfo commands
		private RelayCommand _beginEditFolderInfoCommand;
		public RelayCommand BeginEditFolderInfoCommand
		{
			get
			{
				if (_beginEditFolderInfoCommand == null)
				{
					_beginEditFolderInfoCommand = new((o) =>
					{
						EditMode = EditMode.Edit;
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
						_editMode = EditMode.Preadd;
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
						if (_editMode == EditMode.Preadd)
						{
							SelectedFolderIndex = -1;
							_ds.CancelPreaddFolderInfo();
						}
						else if (_editMode == EditMode.Edit)
						{
							_ds.CancelEditFolderInfoBody();
						}
						_editMode = EditMode.None;
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
						_editMode = EditMode.None;
					});
				}
				return _saveEditableFolderInfoCommand;
			}
		}
		#endregion //Edit mode FolderInfo commands


		private RelayCommand _unlockFolderCommand;
		public RelayCommand UnlockFolderCommand
		{
			get
			{
				if (_unlockFolderCommand == null)
				{
					_unlockFolderCommand = new((o) =>
					{
						if(!_ds.TryReadFolderInfoContent(Folders[_selectedFolderIndex], this._folderPass))
						{
							MessageBox.Show("Wrong folder pass!");
						}
					});
				}
				return _unlockFolderCommand;
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
