// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: TheLacus
// Contributors:    
// 
// Notes:
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility;
using FullSerializer;

namespace DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings
{
    #region Collections

    /// <summary>
    /// A collection of sections which can be accessed by index or name.
    /// </summary>
    [fsObject(Converter = typeof(KeyedCollectionConverter<SectionCollection, string, Section>))]
    public class SectionCollection : EnhancedKeyedCollection<string, Section>
    {
        protected override string GetKeyForItem(Section section)
        {
            return section.Name;
        }
    }

    /// <summary>
    /// A collection of keys which can be accessed by index or name.
    /// </summary>
    [fsObject(Converter = typeof(KeyedCollectionConverter<KeyCollection, string, Key>))]
    public class KeyCollection : EnhancedKeyedCollection<string, Key>
    {
        protected override string GetKeyForItem(Key key)
        {
            return key.Name;
        }
    }

    /// <summary>
    /// A collection of keys in a named group.
    /// </summary>
    [fsObject("v1")]
    public class Section
    {
        string name;
        KeyCollection keys = new KeyCollection();

#if UNITY_EDITOR
        Action<string, string, string> syncKeyCallback;

        public Action<string, string> SyncSectionCallback {get; set;}

        public Action<string, string, string> SyncKeysCallback
        {
            set { SetSyncKeysCallback(value); }
        }
#endif
        /// <summary>
        /// The name of this section.
        /// </summary>
        [fsProperty()]
        public string Name
        {
            get { return name; }
            set { SetName(value); }
        }

        /// <summary>
        /// A short description for this section.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// This section contains experimental settings and/or should be edited only by advanced users.
        /// </summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// All the keys inside this section.
        /// </summary>
        [fsProperty()]
        public KeyCollection Keys
        {
            get { return keys; }
            set { keys = value; }
        }

        public Key this[int index]
        {
            get { return Keys[index]; }
        }

        public Key this[string key]
        {
            get { return Keys[key]; }
        }

#if UNITY_EDITOR
        private void SyncKey(string oldName, string newName)
        {
            Keys.ChangeKey(Keys[oldName], newName);
            if (syncKeyCallback != null)
                syncKeyCallback(name, oldName, newName);
        }

        private void SetSyncKeysCallback(Action<string, string, string> value)
        {
            syncKeyCallback = value;
            foreach (var key in Keys)
                key.SyncKeyCallback = SyncKey;
        }
#endif

        private void SetName(string value)
        {
            if (value == name)
                return;
#if UNITY_EDITOR
            if (SyncSectionCallback != null)
                SyncSectionCallback(name, value);
#endif
            name = value;
        }
    }

    #endregion

    #region Keys

    /// <summary>
    /// Type of setting key.
    /// </summary>
    public enum KeyType
    {
        Toggle,
        MultipleChoice,
        SliderInt,
        SliderFloat,
        TupleInt,
        TupleFloat,
        Text,
        Color
    }

    /// <summary>
    /// A single key with a name.
    /// </summary>
    [fsObject("v1")]
    public abstract class Key
    {
        string name;

#if UNITY_EDITOR
        public delegate void HorizontalCallback(Rect rect, string prefixLabel, params Action<Rect>[] rects);
        public delegate void VerticalCallback(Rect rect, int linesPerItem, params Action<Rect>[] rects);

        public Action<string, string> SyncKeyCallback {get; set;}
#endif
        
        [fsProperty()]
        public string Name
        {
            get { return name; }
            set { SetName(value);}
        }
        
        public string Description { get; set; }
        public abstract KeyType KeyType { get; }

        /// <summary>
        /// The value of this key as a serialized string.
        /// </summary>
        public abstract string TextValue { get; set; }

        /// <summary>
        /// The value of this key casted to object.
        /// </summary>
        public abstract object ToObject();

        /// <summary>
        /// Draws a control on game window.
        /// </summary>
        public abstract object OnWindow(ModSettingsWindow window);

        /// <summary>
        /// Save value from a control.
        /// </summary>
        public abstract void OnSaveWindow(object control);

#if UNITY_EDITOR
        /// <summary>
        /// Draws a control on editor window. Returns number of lines.
        /// </summary>
        public abstract int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache);
#endif

        /// <summary>
        /// Make a new instance of a key of the given type.
        /// </summary>
        public static Key Make(KeyType keyType = KeyType.Toggle)
        {
            switch (keyType)
            {
                case KeyType.Toggle:
                    return new ToggleKey();
                case KeyType.MultipleChoice:
                    return new MultipleChoiceKey();
                case KeyType.SliderInt:
                    return new SliderIntKey();
                case KeyType.SliderFloat:
                    return new SliderFloatKey();
                case KeyType.TupleInt:
                    return new TupleIntKey();
                case KeyType.TupleFloat:
                    return new TupleFloatKey();
                case KeyType.Text:
                    return new TextKey();
                case KeyType.Color:
                    return new ColorKey();
            }

            throw new ArgumentException(string.Format("KeyType {0} is not defined!", keyType));
        }

        /// <summary>
        /// Join values in a single string.
        /// </summary>
        protected static string Join<T>(params T[] args)
        {
            return string.Join(",", args.Select(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Get values from a string.
        /// </summary>
        protected static bool TrySplit<T>(string input, int count, out T[] items)
        {
            try
            {
                string[] args = input.Split(new char[] { ',' }, count);
                items = new T[args.Length];
                for (int i = 0; i < args.Length; i++)
                    items[i] = (T)Convert.ChangeType(args[i], typeof(T));
                return true;
            }
            catch
            {
                items = new T[0];
                return false;
            }
        }

        private void SetName(string value)
        {
            if (value == name)
                return;
#if UNITY_EDITOR
            if (SyncKeyCallback != null)
                SyncKeyCallback(name, value);
#endif
            name = value;
        }
    }

    /// <summary>
    /// A single type-safe key with a name.
    /// </summary>
    public abstract class Key<T> : Key
    {
        /// <summary>
        /// The value of this key.
        /// </summary>
        public T Value { get; set; }

        public sealed override string TextValue
        {
            get { return Serialize(); }
            set { TryDeserialize(value); }
        }

        public sealed override object ToObject()
        {
            return Value;
        }

        protected virtual string Serialize()
        {
            return Value.ToString();
        }

        protected abstract void TryDeserialize(string text);
    }

    /// <summary>
    /// Holds a bool value.
    /// </summary>
    public class ToggleKey : Key<bool>
    {
        public override KeyType KeyType { get { return KeyType.Toggle; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutCheckbox(Value);
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((Checkbox)control).IsChecked;
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            horizontal(rect, null, (r) => Value = EditorGUI.Toggle(r, "Checked", Value));
            return 1;
        }
#endif

        protected override void TryDeserialize(string textValue)
        {
            bool value;
            if (bool.TryParse(textValue, out value))
                Value = value;
        }
    }

    /// <summary>
    /// Holds an option selected from a list.
    /// </summary>
    public class MultipleChoiceKey : Key<int>
    {
#if UNITY_EDITOR
        ReorderableList optionsEditor;
#endif

        public List<string> Options = new List<string>();

        public override KeyType KeyType { get { return KeyType.MultipleChoice; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutSlider(x => x.SetIndicator(Options.ToArray(), Value));
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((HorizontalSlider)control).Value;
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            if (optionsEditor == null)
            {
                optionsEditor = new ReorderableList(Options, typeof(string), true, true, true, true);
                optionsEditor.drawHeaderCallback = r =>
                {
                    EditorGUI.LabelField(r, "Options");
                    if (GUI.Button(new Rect(r.x + r.width - 30, r.y, 30, EditorGUIUtility.singleLineHeight), (Texture2D)EditorGUIUtility.Load("icons/SettingsIcon.png"), EditorStyles.toolbarButton))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Copy choices"), false, () => cache["options"] = Options.ToList());
                        if (cache.ContainsKey("options"))
                            menu.AddItem(new GUIContent("Paste choices"), false, () => { optionsEditor.list = Options = (List<string>)cache["options"]; });
                        menu.ShowAsContext();
                    }
                };
                optionsEditor.onAddCallback = x => x.list.Add(string.Empty);
                optionsEditor.drawElementCallback = (r, i, a, f) => optionsEditor.list[i] = EditorGUI.TextField(new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight), (string)optionsEditor.list[i]);
            }

            vertical(rect, 1,
                (r) => Value = EditorGUI.Popup(r, "Selected", Value, Options.ToArray()),
                (r) => optionsEditor.DoList(r));
            return optionsEditor.count + 3;
        }
#endif

        protected override void TryDeserialize(string textValue)
        {
            int value;
            if (int.TryParse(textValue, out value))
                Value = Mathf.Clamp(value, 0, Options.Count);
        }
    }

    /// <summary>
    /// Holds an int value in a range.
    /// </summary>
    public class SliderIntKey : Key<int>
    {
        public int Min;
        public int Max;

        public override KeyType KeyType { get { return KeyType.SliderInt; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutSlider(x => x.SetIndicator(Min, Max, Value));
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((HorizontalSlider)control).Value;
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            vertical(rect, 1,
                (line) => horizontal(line, null,
                    (r) => Value = EditorGUI.IntSlider(r, "Value", Value, Min, Max)),
                (line) => horizontal(line, "Range",
                    (r) => Min = EditorGUI.IntField(r, "Min", Min),
                    (r) => Max = EditorGUI.IntField(r, "Max", Max))
            );
            return 2;
        }
#endif

        protected override void TryDeserialize(string textValue)
        {
            int value;
            if (int.TryParse(textValue, out value))
                Value = Mathf.Clamp(value, Min, Max);
        }
    }

    /// <summary>
    /// Holds a float value in a range.
    /// </summary>
    public class SliderFloatKey : Key<float>
    {
        public float Min;
        public float Max;

        public override KeyType KeyType { get { return KeyType.SliderFloat; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutSlider(x => x.SetIndicator(Min, Max, Value));
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((HorizontalSlider)control).GetValue();
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            vertical(rect, 1,
                (line) => horizontal(line, null,
                    (r) => Value = EditorGUI.Slider(r, "Value", Value, Min, Max)),
                (line) => horizontal(line, "Range",
                    (r) => Min = EditorGUI.FloatField(r, "Min", Min),
                    (r) => Max = EditorGUI.FloatField(r, "Max", Max))
            );
            return 2;
        }
#endif

        protected override void TryDeserialize(string textValue)
        {
            float value;
            if (float.TryParse(textValue, out value))
                Value = Mathf.Clamp(value, Min, Max);
        }
    }

    /// <summary>
    /// Holds two int values.
    /// </summary>
    public class TupleIntKey : Key<Tuple<int, int>>
    {
        public override KeyType KeyType { get { return KeyType.TupleInt; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutTuple(Value.First.ToString(), Value.Second.ToString());
        }

        public override void OnSaveWindow(object control)
        {
            var tuple = (Tuple<TextBox, TextBox>)control;
            TryDeserialize(Join(tuple.First.ResultText, tuple.Second.ResultText));
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            if (Value == null) Value = Tuple<int, int>.Make(0, 100);
            horizontal(rect, "Value",
                (r) => Value.First = EditorGUI.IntField(r, "First", Value.First),
                (r) => Value.Second = EditorGUI.IntField(r, "Second", Value.Second));
            return 1;
        }
#endif

        protected override string Serialize()
        {
            return Join(Value.First, Value.Second);
        }

        protected override void TryDeserialize(string textValue)
        {
            int[] args;
            if (TrySplit(textValue, 2, out args))
                Value = new Tuple<int, int>(args[0], args[1]);
        }
    }

    /// <summary>
    /// Holds two float values.
    /// </summary>
    public class TupleFloatKey : Key<Tuple<float, float>>
    {
        public override KeyType KeyType { get { return KeyType.TupleFloat; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutTuple(Value.First.ToString(), Value.Second.ToString());
        }

        public override void OnSaveWindow(object control)
        {
            var tuple = (Tuple<TextBox, TextBox>)control;
            TryDeserialize(Join(tuple.First.ResultText, tuple.Second.ResultText));
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            if (Value == null) Value = Tuple<float, float>.Make(0, 100);
            horizontal(rect, "Value",
                (r) => Value.First = EditorGUI.FloatField(r, "First", Value.First),
                (r) => Value.Second = EditorGUI.FloatField(r, "Second", Value.Second));
            return 1;
        }
#endif

        protected override string Serialize()
        {
            return Join(Value.First, Value.Second);
        }

        protected override void TryDeserialize(string textValue)
        {
            int[] args;
            if (TrySplit(textValue, 2, out args))
                Value = new Tuple<float, float>(args[0], args[1]);
        }
    }

    /// <summary>
    /// Holds a raw string.
    /// </summary>
    public class TextKey : Key<string>
    {
        public override KeyType KeyType { get { return KeyType.Text; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutTextBox(Value);
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((TextBox)control).ResultText;
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            vertical(rect, 3, (r => Value = EditorGUI.TextArea(r, Value)));
            return 3;
        }
#endif

        protected override void TryDeserialize(string textValue)
        {
            Value = textValue;
        }
    }

    /// <summary>
    /// Holds an RGBA color.
    /// </summary>
    public class ColorKey : Key<Color32>
    {
        public override KeyType KeyType { get { return KeyType.Color; } }

        public override object OnWindow(ModSettingsWindow window)
        {
            return window.LayoutColorPicker(Value);
        }

        public override void OnSaveWindow(object control)
        {
            Value = ((Button)control).BackgroundColor;
        }

#if UNITY_EDITOR
        public override int OnEditorWindow(Rect rect, HorizontalCallback horizontal, VerticalCallback vertical, Dictionary<string, object> cache)
        {
            vertical(rect, 1, (r) => Value = EditorGUI.ColorField(r, "Color", Value));
            return 1;
        }
#endif

        protected override string Serialize()
        {
            return ColorUtility.ToHtmlStringRGBA(Value);
        }

        protected override void TryDeserialize(string textValue)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + textValue, out color))
                Value = color;
        }
    }

    #endregion

    #region IPreset

    /// <summary>
    /// Holds a group of serialized values which can be merged in a corresponding <cref>ModSettingsData</cref> instance.
    /// </summary>
    public interface IPreset
    {
        string SettingsVersion { get; set; }
        Dictionary<string, Dictionary<string, string>> Values { get; set; }
    }
    
    /// <summary>
    /// Settings values serialized on disk.
    /// </summary>
    [fsObject("v1")]
    public class SettingsValues : IPreset
    {
        public string SettingsVersion { get; set; }
        public Dictionary<string, Dictionary<string, string>> Values { get; set; }
    }

    /// <summary>
    /// A group of settings values with meta properties.
    /// </summary>
    [fsObject("v1")]
    public class Preset : IPreset
    {
        /// <summary>
        /// Title (does not correspond to filename).
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Optional field (for imported presets).
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Short description shown on GUI.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Version of target settings (not version of preset!)
        /// </summary>
        public string SettingsVersion { get; set; }

        /// <summary>
        /// True if can be edited from gui.
        /// </summary>
        [fsIgnore]
        public bool IsLocal;

        public Dictionary<string, Dictionary<string, string>> Values { get; set; }

#if UNITY_EDITOR

        /// <summary>
        /// Does the preset contains this section?
        /// </summary>
        public bool this[string section]
        {
            get { return Values.ContainsKey(section); }
            set { SetState(Values, section, value, () => new Dictionary<string, string>()); }
        }

        /// <summary>
        /// Does the preset contains this key?
        /// </summary>
        public bool this[string section, string key]
        {
            get { return Values.Values.Any(x => x.ContainsKey(key)); }
            set
            {
                Dictionary<string, string> sectionDict;
                if (Values.TryGetValue(section, out sectionDict))
                    SetState(sectionDict, key, value, () => string.Empty);
            }
        }
        
#endif
        public Preset()
        {
            Values = new Dictionary<string, Dictionary<string, string>>();
        }

#if UNITY_EDITOR

        private static void SetState<T>(Dictionary<string, T> dict, string key, bool enabled, Func<T> init)
        {
            bool keyFound = dict.ContainsKey(key);
            if (enabled) { if (!keyFound) dict.Add(key, init()); }
            else { if (keyFound) dict.Remove(key); }
        }

#endif
    }

    #endregion
}
