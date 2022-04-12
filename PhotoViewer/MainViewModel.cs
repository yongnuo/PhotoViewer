using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using WpfTools.Command;
using WpfTools.Configuration;
using WpfTools.Observable;
using Timer = System.Timers.Timer;

namespace PhotoViewer
{
	public class MainViewModel
	{
		private readonly object _thisLock = new object();

		private readonly string _path;
		private readonly int _photoLoadDelay;
		private readonly bool _onlyLoadPhotoFiles;

		private readonly bool _useSlideShow;
		private readonly bool _randomizeSlideShow;

		private readonly Timer _slideShowTimer;
		private List<string> _slideShowPhotos;
		private readonly Timer _restartSlideShowTimer;
        private readonly List<string> _recognisedImageExtensions;

        public Observable<string> Photo { get; set; }
		public Observable<WindowState> WindowState { get; set; }
		public Observable<WindowStyle> WindowStyle { get; set; }
		public Observable<ResizeMode> ResizeMode { get; set; }
		public ICommand ExitFullScreenCommand { get; set; }
		public Observable<bool> IsNormalWindowState { get; set; }
		public ICommand FullScreenCommand { get; set; }

		public MainViewModel()
		{
			_path = GetPath();
			if (!Directory.Exists(_path))
			{
				Application.Current.Shutdown();
				return;
			}

            _recognisedImageExtensions = new List<string>();

            foreach (System.Drawing.Imaging.ImageCodecInfo imageCodec in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
                _recognisedImageExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(";".ToCharArray()));

			_photoLoadDelay = ConfigurationHelper.GetIntFromConfig("PhotoLoadDelay", 1000);
			_onlyLoadPhotoFiles = ConfigurationHelper.GetBoolFromConfig("OnlyLoadPhotoFiles", true);

			_useSlideShow = ConfigurationHelper.GetBoolFromConfig("UseSlideShow", true);
			var slideShowStartDelay = ConfigurationHelper.GetIntFromConfig("SlideShowStartDelay", 30000);
			var slideShowPhotoDelay = ConfigurationHelper.GetIntFromConfig("SlideShowPhotoDelay", 500);
			_randomizeSlideShow = ConfigurationHelper.GetBoolFromConfig("RandomizeSlideShow", true);

			_slideShowPhotos = new List<string>();

			_slideShowTimer = new Timer
			{
				AutoReset = true,
				Interval = slideShowPhotoDelay
			};
			_slideShowTimer.Elapsed += NextPhoto;
			if (_useSlideShow)
				_slideShowTimer.Start();

			_restartSlideShowTimer = new Timer
			{
				AutoReset = false,
				Interval = slideShowStartDelay
			};
			_restartSlideShowTimer.Elapsed += (s, e) => _slideShowTimer.Start();

			InitializeSlideShow();

			Photo = new Observable<string>();
			IsNormalWindowState = new Observable<bool>(true);
			WindowState = new Observable<WindowState> { Value = System.Windows.WindowState.Normal };
			WindowStyle = new Observable<WindowStyle>(System.Windows.WindowStyle.SingleBorderWindow);
			ResizeMode = new Observable<ResizeMode>(System.Windows.ResizeMode.NoResize);

			FullScreenCommand = new RelayCommand(FullScreen);
			ExitFullScreenCommand = new RelayCommand(ExitFullScreen);

			LoadDefaultPhoto();

			var watcher = new FileSystemWatcher(_path);
			if (ConfigurationHelper.GetBoolFromConfig("EventOnCreate", true))
				watcher.Created += FolderContentChanged;
			if (ConfigurationHelper.GetBoolFromConfig("EventOnChange", true))
				watcher.Changed += FolderContentChanged;
			if (ConfigurationHelper.GetBoolFromConfig("EventOnRename", true))
				watcher.Renamed += FolderContentChanged;
			watcher.EnableRaisingEvents = true;
		}

		private void InitializeSlideShow()
		{
			_slideShowPhotos = Directory.GetFiles(_path)
				.Where(IsRecognisedImageFile)
				.OrderBy(x => x)
				.ToList();
			if (_randomizeSlideShow)
				_slideShowPhotos.Shuffle();
		}

		private void NextPhoto(object state, ElapsedEventArgs elapsedEventArgs)
		{
			lock (_thisLock)
			{
				if (!_slideShowTimer.Enabled)
					return;
				if (_slideShowPhotos.Any())
				{
					Photo.Value = _slideShowPhotos.First();
					_slideShowPhotos.RemoveAt(0);
				}
				else
				{
					_slideShowTimer.Stop();
					InitializeSlideShow();
					_slideShowTimer.Start();
				}
			}
		}

		private string GetPath()
		{
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			var path = ConfigurationHelper.GetStringFromConfig("Path", baseDirectory);
			return Path.IsPathRooted(path)
				? path
				: string.Format("{0}{1}", baseDirectory, path);
		}

		private void LoadDefaultPhoto()
		{
			var defaultFile = Directory
				.GetFiles(_path)
				.FirstOrDefault(IsRecognisedImageFile);
			if (defaultFile != null)
				Photo.Value = defaultFile;
		}

		private void FolderContentChanged(object sender, FileSystemEventArgs e)
		{
			lock (_thisLock)
			{
				if (!_onlyLoadPhotoFiles || IsRecognisedImageFile(e.FullPath))
				{
					Thread.Sleep(_photoLoadDelay);
					Photo.Value = e.FullPath;
					if (_useSlideShow)
					{
						_slideShowTimer.Stop();
						_restartSlideShowTimer.Start();
					}
				}
			}
		}

		public bool IsRecognisedImageFile(string fileName)
		{
			var targetExtension = Path.GetExtension(fileName);
			if (string.IsNullOrEmpty(targetExtension))
				return false;
			targetExtension = "*" + targetExtension.ToLowerInvariant();

			return _recognisedImageExtensions.Any(extension => extension.Equals(targetExtension));
		}

		private void FullScreen()
		{
			if (IsNormalWindowState)
				EnterFullScreen();
			else
				ExitFullScreen();
		}

		private void EnterFullScreen()
		{
			ResizeMode.Value = System.Windows.ResizeMode.NoResize;
			WindowState.Value = System.Windows.WindowState.Maximized;
			WindowStyle.Value = System.Windows.WindowStyle.None;
			IsNormalWindowState.Value = false;
		}

		private void ExitFullScreen()
		{
			WindowState.Value = System.Windows.WindowState.Normal;
			WindowStyle.Value = System.Windows.WindowStyle.SingleBorderWindow;
			ResizeMode.Value = System.Windows.ResizeMode.CanResize;
			IsNormalWindowState.Value = true;
		}
	}

	public static class Extensions
	{
		private static readonly Random Random = new Random();

		public static void Shuffle<T>(this IList<T> list)
		{
			var listCount = list.Count;
			while (listCount > 1)
			{
				listCount--;
				var k = Random.Next(listCount + 1);
				var value = list[k];
				list[k] = list[listCount];
				list[listCount] = value;
			}
		}
	}
}