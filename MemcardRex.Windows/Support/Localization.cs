using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace MemcardRex
{
    /// <summary>
    /// Lightweight localization layer for the MemcardRex Windows edition.
    /// Loads a language pack (Languages\zh-CN.xml by default) and applies
    /// exact-match translations to the user interface at runtime.
    /// 
    /// Loading order:
    ///   1. External file next to the executable (Languages\zh-CN.xml) - allows
    ///      users to customize translations without rebuilding the program.
    ///   2. Embedded resource (MemcardRex.Languages.zh-CN.xml) - built into the
    ///      application, used when no external file is present.
    /// </summary>
    public static class Localization
    {
        private static Dictionary<string, string> _table = null;

        /// <summary>Translation table key (for debugging / coverage checks).</summary>
        public static bool IsLoaded { get { return _table != null; } }

        /// <summary>Number of loaded translation entries.</summary>
        public static int EntryCount { get { return _table == null ? 0 : _table.Count; } }

        /// <summary>
        /// Loads the language pack. Tries the external file first so users can
        /// customize the translation, then falls back to the embedded resource.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                string external = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", "zh-CN.xml");
                if (File.Exists(external))
                {
                    LoadFromFile(external);
                    return;
                }
            }
            catch
            {
                //Fall through to the embedded resource
            }

            LoadFromResource();
        }

        /// <summary>Loads translations from an external language pack file.</summary>
        public static void LoadFromFile(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                _table = Parse(stream);
            }
        }

        /// <summary>Loads translations from the embedded resource.</summary>
        public static void LoadFromResource()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MemcardRex.Languages.zh-CN.xml"))
                {
                    if (stream == null) { _table = null; return; }
                    _table = Parse(stream);
                }
            }
            catch
            {
                _table = null;
            }
        }

        private static Dictionary<string, string> Parse(Stream stream)
        {
            Dictionary<string, string> table = new Dictionary<string, string>(StringComparer.Ordinal);

            XmlDocument document = new XmlDocument();
            document.Load(stream);

            XmlNodeList entries = document.SelectNodes("/localization/strings/string");
            if (entries == null) return table;

            foreach (XmlNode entry in entries)
            {
                XmlNode sourceNode = entry.SelectSingleNode("source");
                XmlNode targetNode = entry.SelectSingleNode("target");
                if (sourceNode == null || targetNode == null) continue;

                string source = sourceNode.InnerText;
                string target = targetNode.InnerText;

                if (!string.IsNullOrEmpty(source) && !table.ContainsKey(source))
                    table.Add(source, target);
            }

            return table;
        }

        /// <summary>
        /// Translates a single string. Returns the original text when no
        /// translation is available or when translations are not loaded.
        /// </summary>
        public static string T(string source)
        {
            if (_table == null || string.IsNullOrEmpty(source)) return source;
            return _table.TryGetValue(source, out string target) ? target : source;
        }

        /// <summary>
        /// Walks the control tree of a form (or any control) and applies the
        /// loaded translations to all user-visible text. Called once after
        /// InitializeComponent() of each form.
        /// </summary>
        public static void ApplyToForm(Control root)
        {
            if (_table == null || root == null) return;
            ApplyToControl(root);
        }

        private static void ApplyToControl(Control control)
        {
            try
            {
                //ToolStrip based containers (menu bar, tool bars, status bar)
                if (control is ToolStrip toolStrip)
                {
                    foreach (ToolStripItem item in toolStrip.Items)
                        ApplyToolStripItem(item);
                }

                //List views: translate column headers
                if (control is ListView listView)
                {
                    foreach (ColumnHeader column in listView.Columns)
                        column.Text = Translate(column.Text);
                }

                //Translate the control's own text (unless it holds user data)
                if (!IsDataControl(control))
                    control.Text = Translate(control.Text);

                //Recurse into children
                foreach (Control child in control.Controls)
                    ApplyToControl(child);
            }
            catch
            {
                //Never let a translation issue break the UI
            }
        }

        private static void ApplyToolStripItem(ToolStripItem item)
        {
            try
            {
                item.Text = Translate(item.Text);

                //Recurse into drop-down menus
                if (item is ToolStripMenuItem menuItem)
                {
                    foreach (ToolStripItem subItem in menuItem.DropDownItems)
                        ApplyToolStripItem(subItem);
                }
            }
            catch
            {
                //Ignore single item failures
            }
        }

        //Controls whose Text property holds user data or save data must not be translated.
        private static bool IsDataControl(Control control)
        {
            return control is TextBox ||
                   control is RichTextBox ||
                   control is ComboBox ||
                   control is NumericUpDown ||
                   control is DateTimePicker ||
                   control is ListBox ||
                   control is CheckedListBox ||
                   control is ListView ||
                   control is DataGridView;
        }

        //Exact-match translation of a single text value.
        private static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text) || _table == null) return text;
            return _table.TryGetValue(text, out string translated) ? translated : text;
        }
    }
}
