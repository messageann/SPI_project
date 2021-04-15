using DataModule;
using DataModule.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
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

		public string Title => "Encoder";
		public ReadOnlyObservableCollection<FolderInfo> Folders => _ds.Folders;

		private RelayCommand _preAddFolderInfo;
		public ICommand PreAddFolderInfo
		{
			get
			{
				if (_preAddFolderInfo == null) _preAddFolderInfo = new RelayCommand((o) => _ds.PreaddFolderInfo());
				return _preAddFolderInfo;
			}
		}

		#region NOTIFS
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion //NOTIFS
	}
}
