/* Localization.cs - MemcardRex for Linux
 *
 * Simplified Chinese (zh-CN) localization support for the GTK version.
 *
 * The UI text of the Linux version lives in embedded .ui templates and in
 * code string literals. This class:
 *   1. Loads the zh-CN dictionary (external Languages\zh-CN.xml next to the
 *      executable, falling back to the embedded resource).
 *   2. T(source) translates string literals used from code.
 *   3. Builder(templateName) reads an embedded .ui template, applies the
 *      dictionary to its label/title/tooltip-text properties, and returns a
 *      Gtk.Builder built from the translated XML.
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Gtk;

namespace MemcardRex.Linux;

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
                .GetManifestResourceStream("MemcardRex.Linux.Languages.zh-CN.xml");
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

    //Read an embedded text resource
    private static string ReadResource(string name)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream == null) throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    //Apply the dictionary to .ui XML: translate label/title/tooltip-text values
    private static string TranslateUiXml(string xml)
    {
        if (_table == null) return xml;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        XmlNodeList? nodes = doc.SelectNodes("//*[@name='label' or @name='title' or @name='tooltip-text']");
        if (nodes != null)
        {
            foreach (XmlElement element in nodes)
            {
                //Skip labels containing child elements (e.g. Pango markup)
                bool hasChildElements = false;
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child is XmlElement) { hasChildElements = true; break; }
                }
                if (hasChildElements) continue;

                string text = element.InnerText;
                if (text.Length == 0) continue;
                if (_table.TryGetValue(text, out string? value)) element.InnerText = value;
            }
        }

        return doc.OuterXml;
    }

    //Build a Gtk.Builder from an embedded .ui template with translations applied
    public static Builder Builder(string embeddedTemplateName)
    {
        string uiXml = ReadResource(embeddedTemplateName);
        if (_table != null) uiXml = TranslateUiXml(uiXml);

        var builder = Gtk.Builder.New();
        builder.AddFromString(uiXml, uiXml.Length);
        return builder;
    }
}
