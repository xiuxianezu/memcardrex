/* Localization.cs - MemcardRex for macOS
 *
 * Simplified Chinese (zh-CN) localization support for the macOS version.
 *
 * UI text of the macOS version lives in Main.storyboard (menus, toolbars and
 * dialog labels) and in code string literals. This class:
 *   1. Loads the zh-CN dictionary (external Languages\zh-CN.xml next to the
 *      executable, falling back to the embedded resource).
 *   2. T(source) translates string literals used from code.
 *   3. ApplyToMenus / ApplyToWindow / ApplyToView walk the NSMenu, NSToolbar
 *      and view hierarchies and translate titles/labels by exact dictionary
 *      match.
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Foundation;
using AppKit;

namespace MemcardRex
{
    public static class Localization
    {
        private static Dictionary<string, string>? _table = null;

        //Load dictionary from external file or embedded resource
        public static void Initialize()
        {
            string external = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", "zh-CN.xml");
            if (File.Exists(external))
            {
                LoadFromFile(external);
                return;
            }

            LoadFromResource();
        }

        public static void LoadFromFile(string path)
        {
            using FileStream fs = File.OpenRead(path);
            _table = Parse(fs);
        }

        public static void LoadFromResource()
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("MemcardRex.Languages.zh-CN.xml");
                if (stream == null)
                {
                    _table = null;
                    return;
                }

                _table = Parse(stream);
            }
            catch
            {
                _table = null;
            }
        }

        //Parse the <localization><strings><string><source>/<target> dictionary
        private static Dictionary<string, string>? Parse(Stream stream)
        {
            var table = new Dictionary<string, string>();
            try
            {
                var doc = new XmlDocument();
                doc.Load(stream);
                XmlNodeList? nodes = doc.SelectNodes("/localization/strings/string");
                if (nodes == null) return table;
                foreach (XmlNode node in nodes)
                {
                    XmlNode? source = node.SelectSingleNode("source");
                    XmlNode? target = node.SelectSingleNode("target");
                    if (source == null || target == null) continue;
                    string key = source.InnerText;
                    if (key.Length == 0 || table.ContainsKey(key)) continue;
                    table[key] = target.InnerText;
                }
            }
            catch
            {
                return null;
            }

            return table;
        }

        //Translate a single source string
        public static string T(string? source)
        {
            if (_table == null || string.IsNullOrEmpty(source)) return source ?? "";
            return _table.TryGetValue(source, out string? value) ? value : source;
        }

        //Translate a title/label, returning the dictionary value when present
        private static string Translate(string? source)
        {
            if (_table == null || string.IsNullOrEmpty(source)) return source ?? "";
            return _table.TryGetValue(source, out string? value) ? value : source;
        }

        //Walk an NSMenu recursively and translate item titles
        public static void ApplyToMenu(NSMenu menu)
        {
            if (_table == null || menu == null) return;

            foreach (NSMenuItem item in menu.Items)
            {
                string title = item.Title;
                if (!string.IsNullOrEmpty(title))
                {
                    string translated = Translate(title);
                    if (translated != title) item.Title = translated;
                }

                if (item.Submenu != null) ApplyToMenu(item.Submenu);
            }
        }

        //Apply to the application main menu
        public static void ApplyToMenus()
        {
            NSMenu? mainMenu = NSApplication.SharedApplication.MainMenu;
            if (mainMenu != null) ApplyToMenu(mainMenu);
        }

        //Apply to a window: toolbar item labels and the content view
        public static void ApplyToWindow(NSWindow window)
        {
            if (_table == null || window == null) return;

            NSToolbar? toolbar = window.Toolbar;
            if (toolbar != null)
            {
                foreach (NSToolbarItem item in toolbar.Items)
                {
                    string label = item.Label;
                    if (!string.IsNullOrEmpty(label))
                    {
                        string translated = Translate(label);
                        if (translated != label) item.Label = translated;
                    }

                    string paletteLabel = item.PaletteLabel;
                    if (!string.IsNullOrEmpty(paletteLabel))
                    {
                        string translated = Translate(paletteLabel);
                        if (translated != paletteLabel) item.PaletteLabel = translated;
                    }
                }
            }

            NSView? contentView = window.ContentView;
            if (contentView != null) ApplyToView(contentView);
        }

        //Walk a view hierarchy: translate button titles and non-editable labels
        public static void ApplyToView(NSView view)
        {
            if (_table == null || view == null) return;

            if (view is NSPopUpButton popup)
            {
                //Translate the popup title and its menu item titles
                string title = popup.Title;
                if (!string.IsNullOrEmpty(title))
                {
                    string translated = Translate(title);
                    if (translated != title) popup.Title = translated;
                }

                if (popup.Menu != null) ApplyToMenu(popup.Menu);
            }
            else if (view is NSButton button)
            {
                string title = button.Title;
                if (!string.IsNullOrEmpty(title))
                {
                    string translated = Translate(title);
                    if (translated != title) button.Title = translated;
                }
            }
            else if (view is NSTextField field && !field.Editable)
            {
                string value = field.StringValue;
                if (!string.IsNullOrEmpty(value))
                {
                    string translated = Translate(value);
                    if (translated != value) field.StringValue = translated;
                }
            }

            foreach (NSView subview in view.Subviews) ApplyToView(subview);
        }
    }
}
