using System;
using System.Windows;

namespace AccesClientWPF.Helpers
{
    public static class ClipboardHelper
    {
        public static void CopyPlainText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                Clipboard.Clear();                   // on part d’un presse‑papiers vide
                var data = new DataObject();
                data.SetData(DataFormats.Text, text);   // CF_TEXT  (ANSI)
                data.SetData(DataFormats.UnicodeText, text);   // CF_UNICODETEXT
                Clipboard.SetDataObject(data, true);           // true = persistant
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible de copier dans le presse‑papiers : {ex.Message}",
                                "Erreur copie", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
