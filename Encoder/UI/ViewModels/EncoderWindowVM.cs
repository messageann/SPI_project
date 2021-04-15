using DataModule;
using DataModule.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

		private bool _isEditMode;
		public bool IsEditMode
		{
			get => _isEditMode;
			set
			{
				_isEditMode = value;
				NotifyPropertyChanged();
			}
		}

		private int _selectedFolderIndex;
		public int SelectedFolderIndex
		{
			get => _selectedFolderIndex;
			set
			{
				_selectedFolderIndex = value;
				NotifyPropertyChanged();
			}
		}
		#endregion //PROPS

		#region FIELDS
		private EditMode _editMode = EditMode.None;
		#endregion //FIELDS

		#region COMMANDS

		#region Edit mode FolderInfo commands
		private RelayCommand _beginEditFolderInfoCommand;
		public ICommand BeginEditFolderInfoCommand
		{
			get
			{
				if (_beginEditFolderInfoCommand == null)
				{
					_beginEditFolderInfoCommand = new((o) =>
					{
						IsEditMode = true;
						_editMode = EditMode.Edit;
						_ds.BeginEditFolderInfoBody(_selectedFolderIndex);
					});
				}
				return _beginEditFolderInfoCommand;
			}
		}

		private RelayCommand _preAddFolderInfoCommand;
		public ICommand PreAddFolderInfoCommand
		{
			get
			{
				if (_preAddFolderInfoCommand == null)
				{
					_preAddFolderInfoCommand = new((o) =>
					{
						IsEditMode = true;
						_editMode = EditMode.Preadd;
						_ds.PreaddFolderInfo();
						SelectedFolderIndex = 0;
					});
				}
				return _preAddFolderInfoCommand;
			}
		}

		private RelayCommand _cancelEditModeFolderInfoCommand;
		public ICommand CancelEditModeFolderInfoCommand
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
						else throw new NotImplementedException($"Edit mode {_editMode}");
						IsEditMode = false;
						_editMode = EditMode.None;
					});
				}
				return _cancelEditModeFolderInfoCommand;
			}
		}

		private RelayCommand _saveEditableFolderInfoCommand;
		public ICommand SaveEditableFolderInfoCommand
		{
			get
			{
				if (_saveEditableFolderInfoCommand == null)
				{
					_saveEditableFolderInfoCommand = new((o) =>
					{
						if (_editMode == EditMode.Preadd)
						{
							_ds.SavePreaddedFolderInfo();
						}
						else if(_editMode == EditMode.Edit)
						{
							_ds.EndEditFolderInfoBody();
						}
						else throw new NotImplementedException($"Edit mode {_editMode}");
						IsEditMode = false;
						_editMode = EditMode.None;
					});
				}
				return _saveEditableFolderInfoCommand;
			}
		}
		#endregion //Edit mode FolderInfo commands


		private RelayCommand _unlockFolderCommand;
		public ICommand UnlockFolderCommand
		{
			get
			{
				if (_unlockFolderCommand == null)
				{
					_unlockFolderCommand = new((o) =>
					{

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

	enum EditMode : byte
	{
		None = 0,
		Preadd = 1 << 0,
		Edit = 1 << 1
	}
}
