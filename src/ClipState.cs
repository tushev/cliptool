using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ClipboardMonitor
{
    class ClipState : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public Visibility ToolTipVisibility { get; set; } = Visibility.Visible;
        public ImageSource TooltipImageSource { get; set; }
        public double TooltipImageWidth { get; set; }
        public double TooltipImageHeight { get; set; }
        public Visibility TooltipImageVisibility { get; set; } = Visibility.Collapsed;
        public string TooltipContentText { get; set; }
        public Visibility TooltipContentVisibility { get; set; } = Visibility.Collapsed;
        public string TooltipFileNamesText { get; set; }
        public Visibility TooltipFileNamesVisibility { get; set; } = Visibility.Collapsed;
        public string TooltipFormatsText { get; set; }
        public Visibility TooltipFormatsVisibility { get; set; } = Visibility.Visible;

        public string PreviewText { get; set; }
        public Brush PreviewTextBrush { get; set; }

        public string TypeMessage { get; set; }
        public Brush TypeMessageBrush { get; set; }
        public Brush TypeMessageBackgroundBrush { get; set; }

        void UpdateTooltipVisibility()
        {
            ToolTipVisibility =
                (TooltipContentVisibility   == Visibility.Visible ||
                 TooltipFileNamesVisibility == Visibility.Visible ||
                 TooltipFormatsVisibility   == Visibility.Visible ||
                 TooltipImageVisibility     == Visibility.Visible   ) ? Visibility.Visible : Visibility.Hidden;
        }
        void OnTooltipContentVisibilityChanged() { UpdateTooltipVisibility(); }
        void OnTooltipFileNamesVisibilityChanged() { UpdateTooltipVisibility(); }
        void OnTooltipFormatsVisibilityChanged() { UpdateTooltipVisibility(); }
        void OnTooltipImageVisibilityChanged() { UpdateTooltipVisibility(); }

        public void OnTooltipImageSourceChanged()
        {
            if (TooltipImageSource == null)
                TooltipImageVisibility = Visibility.Collapsed;
            else
            {
                TooltipImageVisibility = Visibility.Visible;
                TooltipImageHeight = TooltipImageSource.Height;
                TooltipImageWidth = TooltipImageSource.Width;
            }
        }
        public void OnTooltipFormatsTextChanged()
        {
            if (string.IsNullOrEmpty(TooltipFormatsText))
                TooltipFormatsVisibility = Visibility.Collapsed;
            else
                TooltipFormatsVisibility = Visibility.Visible;
        }
        public void OnTooltipFileNamesTextChanged()
        {
            if (string.IsNullOrEmpty(TooltipFileNamesText))
                TooltipFileNamesVisibility = Visibility.Collapsed;
            else
                TooltipFileNamesVisibility = Visibility.Visible;
        }
        public void OnTooltipContentTextChanged()
        {
            if (string.IsNullOrEmpty(TooltipContentText))
                TooltipContentVisibility = Visibility.Collapsed;
            else
                TooltipContentVisibility = Visibility.Visible;
        }
        public void OnPreviewTextChanged()
        {
            if (string.IsNullOrEmpty(PreviewText))
                TooltipContentText = "";
        }

        void SetMessage(string typeMessage, Brush foreground, Brush background, string previewText = "")
        {
            TypeMessage = typeMessage;
            TypeMessageBrush = foreground;
            TypeMessageBackgroundBrush = background;
            PreviewText = previewText;
        }
        void SetMessage(string typeMessage, Brush foreground, string previewText = "") { SetMessage(typeMessage, foreground, Brushes.White, previewText); }

        void SetFormats(string[] in_array, out string[] out_array)
        {
            // list formats in tooltip
            TooltipFormatsText = String.Join(Environment.NewLine, in_array);

            out_array = in_array;
        }

        public void Update()
        {
            string[] formats;

            // Get formats of data in clipboard
            try // prevent OOM Exception
            {
                SetFormats(Clipboard.GetDataObject().GetFormats(), out formats);                
            }
            catch (Exception ex)
            {
                SetFormats(new string[] { "⚠ Unknown", ex.Message }, out formats);

                // try one more time for robustness
                if (Clipboard.ContainsText())
                {
                    SetMessage("⚠ Text (?)", Application.Current.Resources["TypeUnknown"] as SolidColorBrush, Brushes.Black, Clipboard.GetText());
                    TooltipContentText = Clipboard.GetText();
                }
                else
                {
                    SetMessage("⛔ Error (not empty)", Application.Current.Resources["TypeError"] as SolidColorBrush);                    
                    return;
                }
            }

            // Check if clipboard is empty
            if (formats.Length == 0)
            {
                SetMessage("✔ Clipboard is empty", Application.Current.Resources["TypeEmpty"] as SolidColorBrush, Brushes.Black);
                TooltipImageSource = null;
                TooltipFileNamesText = "";
                return;
            }


            // Clipboard not empty, handle common types:
            // (should be in this order)

            // Text
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText().Split(Environment.NewLine.ToCharArray());
                var multiline = text.Length > 1; // bool flag
                string message = (multiline ? "Multi" : "Single") + "-line text";

                // try to get first non-empty string for preview
                string trimmed_str, preview_text = "";
                foreach (string str in text)
                {
                    trimmed_str = str.Trim();
                    if (trimmed_str.Length > 0)
                    {
                        preview_text = $" {trimmed_str}" + (multiline ? "  ↩" : "");
                        break;
                    }
                }
                SetMessage(message, 
                    multiline ? Application.Current.Resources["TypeMultiLineText"] as SolidColorBrush :
                                Application.Current.Resources["TypeSingleLineText"] as SolidColorBrush, 
                    Brushes.Black, preview_text);

                // full text goes to tooltip
                TooltipContentText = Clipboard.GetText();
            }
            else
            {
                SetMessage($"{formats[0]}", Application.Current.Resources["TypeUnknown"] as SolidColorBrush, Brushes.Black);
            }

            // Image
            if (Clipboard.ContainsImage())
            {
                SetMessage("📷 Image, hover for preview", Application.Current.Resources["TypeImage"] as SolidColorBrush, Brushes.Black);
                TooltipImageSource = Clipboard.GetImage();
            }
            else
            {
                TooltipImageSource = null;
            }

            // File(s)
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToList();
                string message  = files.Count > 1 ? $"Files ({files.Count})" : $"File";
                string text     = files.Count > 1 ? $"{ Path.GetFileName(files[0])}, {Path.GetFileName(files[1])}, ..." : $"{Path.GetFileName(files[0])}";

                if (string.IsNullOrEmpty(text))
                    text = files[0];

                SetMessage(message, Application.Current.Resources["TypeFiles"] as SolidColorBrush, Brushes.Black, text);
                TooltipFileNamesText = String.Join(Environment.NewLine, files);
            }
            else
            {
                TooltipFileNamesText = "";
            }

        }

        public ClipState()
        {
            this.Update();
        }

    }
}
