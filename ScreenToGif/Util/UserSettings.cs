﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace ScreenToGif.Util
{
    internal sealed class UserSettings : INotifyPropertyChanged
    {
        #region Variables

        private static ResourceDictionary _local;
        private static ResourceDictionary _appData;
        private static readonly ResourceDictionary Default;

        public static UserSettings All { get; } = new UserSettings();

        #endregion

        static UserSettings()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            //Paths.
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            //Only creates an empty AppData settings file if there's no local settings defined.
            if (!File.Exists(local) && !File.Exists(appData))
            {
                var directory = Path.GetDirectoryName(appData);

                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                //Just creates an empty filewithout writting anything. 
                File.Create(appData).Dispose();
            }

            //Loads AppData settings.
            if (File.Exists(appData))
            {
                _appData = LoadOrDefault(appData);
                Application.Current.Resources.MergedDictionaries.Add(_appData);
            }

            //Loads Local settings.
            if (File.Exists(local))
            {
                _local = LoadOrDefault(local);
                Application.Current.Resources.MergedDictionaries.Add(_local);
            }

            //Reads the default settings (It's loaded by default).
            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        public static void Save()
        {
            //Only writes if there's something changed. Should not write the default dictionary.
            if (_local == null && _appData == null)
                return;

            //Filename: Local or AppData.
            var filename = _local != null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml") :
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            #region Create folder

            var folder = Path.GetDirectoryName(filename);

            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            #endregion

            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(filename, settings))
                XamlWriter.Save(_local ?? _appData, writer);

            #region Old

            //if (Local != null)
            //{
            //    foreach (var key in Default.Keys)
            //    {
            //        if (Local.Contains(key))
            //            Local[key] = Application.Current.Resources[key]; //Does not make sense here, I already do this when SetValue.
            //        else
            //            Local.Add(key, Application.Current.Resources[key]); //Will load all settings.
            //    }
            //}

            //if (AppData != null)
            //{
            //    foreach (var key in Default.Keys)
            //    {
            //        if (AppData.Contains(key))
            //            AppData[key] = Application.Current.Resources[key];
            //        else
            //            AppData.Add(key, Application.Current.Resources[key]);
            //    }
            //}

            #endregion
        }

        private static object GetValue([CallerMemberName] string key = "", object defaultValue = null)
        {
            if (Default == null)
                return defaultValue;

            if (Application.Current == null || Application.Current.Resources == null)
                return Default[key];

            if (Application.Current.Resources.Contains(key))
                return Application.Current.FindResource(key);

            return Default[key] ?? defaultValue;
        }

        private static void SetValue(object value, [CallerMemberName] string key = "")
        {
            //Updates or inserts the value to the Local resource.
            if (_local != null)
            {
                if (_local.Contains(key))
                    _local[key] = value;
                else
                    _local.Add(key, value);
            }

            //Updates or inserts the value to the AppData resource.
            if (_appData != null)
            {
                if (_appData.Contains(key))
                    _appData[key] = value;
                else
                    _appData.Add(key, value);
            }

            //Updates/Adds the current value of the resource.
            if (Application.Current.Resources.Contains(key))
                Application.Current.Resources[key] = value;
            else
                Application.Current.Resources.Add(key, value);

            All.OnPropertyChanged(key);
        }

        private static ResourceDictionary LoadOrDefault(string path)
        {
            ResourceDictionary resource = null;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        //Read in ResourceDictionary File
                        resource = (ResourceDictionary)XamlReader.Load(fs);
                    }
                    catch (Exception)
                    {
                        //Sets a default value if null.
                        resource = new ResourceDictionary();
                    }
                }

                //Tries to load the resource from disk. 
                //resource = new ResourceDictionary {Source = new Uri(path, UriKind.RelativeOrAbsolute)};
            }
            catch (Exception)
            {
                //Sets a default value if null.
                resource = new ResourceDictionary();
            }

            return resource;
        }

        public static void CreateLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (!File.Exists(local))
                File.Create(local).Dispose();

            _local = LoadOrDefault(local);
        }

        public static void RemoveLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (File.Exists(local))
                File.Delete(local);

            _local = null; //TODO: Should I remove from the merged dictionaries?
        }

        public static void RemoveAppDataSettings()
        {
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            if (File.Exists(appData))
                File.Delete(appData);

            _appData = null; //TODO: Should I remove from the merged dictionaries?
        }

        #region Property Changed

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Properties

        public bool FullScreenMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AsyncRecording
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool UsePreStart
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int PreStartValue
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool ShowCursor
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SnapshotMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int StartUp
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool DetectMouseClicks
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Color ClickColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public string LanguageCode
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public int LatestFps
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double RecorderLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double RecorderTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public int RecorderWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int RecorderHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Key StartPauseShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys StartPauseModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key StopShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys StopModifiers
        {
            get => (ModifierKeys)GetValue();
            set => SetValue(value);
        }

        public Key DiscardShortcut
        {
            get => (Key)GetValue();
            set => SetValue(value);
        }

        public ModifierKeys DiscardModifiers
        {
            get => (ModifierKeys)GetValue();
            set => SetValue(value);
        }

        public bool CheckForUpdates
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Color GridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color GridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color RecorderBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color RecorderForeground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Rect GridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public bool FixedFrameRate
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int SnapshotDefaultDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double EditorTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public WindowState EditorWindowState
        {
            get => (WindowState)GetValue();
            set => SetValue(value);
        }

        public Color InsertFillColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int LatestFpsImport
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #region Options

        public Color BoardGridBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Rect BoardGridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public bool EditorExtendChrome
        {
            get => (bool)GetValue(defaultValue: false);
            set => SetValue(value);
        }

        public bool RecorderThinMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool TripleClickSelection
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NewRecorder
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string LogsFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string TemporaryFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticCleanUp
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string FfmpegLocation
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Board

        public int BoardWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color BoardColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool BoardFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool BoardIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor

        public PasteBehavior PasteBehavior
        {
            get => (PasteBehavior)GetValue();
            set => SetValue(value);
        }

        #region Save As

        public Export SaveType
        {
            get => (Export)GetValue();
            set => SetValue(value);
        }

        public GifEncoderType GifEncoder
        {
            get => (GifEncoderType)GetValue();
            set => SetValue(value);
        }

        public VideoEncoderType VideoEncoder
        {
            get => (VideoEncoderType)GetValue();
            set => SetValue(value);
        }

        public int AviQuality
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool FlipVideo
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ZipImages
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Color ChromaKey
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool DetectUnchanged
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool PaintTransparent
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool Looped
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int RepeatCount
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool RepeatForever
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int Quality
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int MaximumColors
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int OutputFramerate
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ExtraParameters
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Gif.
        public string LatestOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Video.
        public string LatestVideoOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestVideoFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestVideoExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Project.
        public string LatestProjectOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestProjectFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestProjectExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Image.
        public string LatestImageOutputFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestImageFilename
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LatestImageExtension
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool OverwriteOnSave
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SaveToClipboard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Caption

        public string CaptionText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public FontFamily CaptionFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle CaptionFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight CaptionFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double CaptionFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double CaptionOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment CaptionVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment CaptionHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double CaptionMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Free Text

        public string FreeTextText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public FontFamily FreeTextFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle FreeTextFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight FreeTextFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double FreeTextFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color FreeTextFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region New Animation

        public int NewAnimationWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int NewAnimationHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color NewAnimationColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Title Frame

        public string TitleFrameText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public int TitleFrameDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public FontFamily TitleFrameFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle TitleFrameFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight TitleFrameFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment TitleFrameVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment TitleFrameHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Key Strokes

        public string KeyStrokesSeparator
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesExtended
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesDelay
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public FontFamily KeyStrokesFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle KeyStrokesFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight KeyStrokesFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment KeyStrokesVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment KeyStrokesHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Watermark

        public string WatermarkFilePath
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double WatermarkOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Border

        public Color BorderColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double BorderLeftThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderTopThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderRightThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderBottomThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Free Drawing

        public int FreeDrawingPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color FreeDrawingColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        public int ReduceFactor
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int ReduceCount
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #region Delay

        public int OverrideDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int IncrementDecrementDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Cinemagraph

        public Color CinemagraphColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Progress

        public Color ProgressColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color ProgressFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public FontFamily ProgressFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle ProgressFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight ProgressFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double ProgressFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment ProgressVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment ProgressHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Orientation ProgressOrientation
        {
            get => (Orientation)GetValue();
            set => SetValue(value);
        }

        public int ProgressPrecision
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ProgressFormat
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double ProgressThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public bool ProgressShowTotal
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public ProgressType ProgressType
        {
            get => (ProgressType)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Transitions

        public FadeToType FadeToType
        {
            get => (FadeToType)GetValue();
            set => SetValue(value);
        }

        public Color FadeToColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #endregion

        public string Version
        {
            get
            {
                var version = Assembly.GetEntryAssembly().GetName().Version;
                var result = $"{version.Major}.{version.Minor}";

                if (version.Build > 0)
                    result += $".{version.Build}";

                if (version.Revision > 0)
                    result += $".{version.Revision}";

                return result;
            }
        }

        #endregion
    }
}
