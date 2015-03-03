﻿#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Diagnostics;
using System.Xml.Serialization;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    public enum EEqualizerStyle
    {
        Columns
    }

    [XmlType("Equalizer")]
    public struct SThemeEqualizer
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string Skin;

        public SRectF Rect;

        public int NumBars;
        public float Space;
        public EEqualizerStyle Style;
        public EOffOn DrawNegative;

        public SThemeColor Color;
        public SThemeColor MaxColor;
        public SReflection? Reflection;
    }

    public class CEqualizer : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;
        private SThemeEqualizer _Theme;

        public SColorF Color;
        public SColorF MaxColor;
        public float Space;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        private float[] _Bars;
        private int _MaxBar;
        private float _MaxVolume;

        public bool Selectable
        {
            get { return false; }
        }

        public CEqualizer(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeEqualizer();
            ThemeLoaded = false;

            Color = new SColorF();
            MaxColor = new SColorF();

            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;
        }

        public CEqualizer(SThemeEqualizer theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            ThemeLoaded = true;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            ThemeLoaded = true;

            ThemeLoaded &= xmlReader.GetValue(item + "/Skin", out _Theme.Skin, String.Empty);

            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _Theme.Rect.X);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Theme.Rect.Y);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Theme.Rect.Z);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _Theme.Rect.W);
            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.Rect.H);

            ThemeLoaded &= xmlReader.TryGetIntValue(item + "/NumBars", ref _Theme.NumBars);

            ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Space", ref _Theme.Space);

            ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Style", ref _Theme.Style);

            ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/DrawNegative", ref _Theme.DrawNegative);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/MaxColor", out _Theme.MaxColor.Name, String.Empty))
                ThemeLoaded &= _Theme.MaxColor.Get(_PartyModeID, out MaxColor);
            else
            {
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxR", ref MaxColor.R);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxG", ref MaxColor.G);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxB", ref MaxColor.B);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxA", ref MaxColor.A);
            }

            //Reflection
            if (xmlReader.ItemExists(item + "/Reflection"))
            {
                Reflection = true;
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);

                _Theme.Reflection = new SReflection(ReflectionHeight, ReflectionSpace);
            }
            else
            {
                Reflection = false;
                _Theme.Reflection = null;
            }

            if (ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.Color.Color = Color;
                _Theme.MaxColor.Color = MaxColor;
                LoadSkin();
            }
            return ThemeLoaded;
        }

        public void Update(float[] weights, float volume)
        {
            if (weights == null || weights.Length == 0 || _Bars == null)
                return;
            if (volume < 0.001)
            {
                for (int i = 0; i < _Bars.Length; i++)
                    _Bars[i] = 0f;
                return;
            }
            if (volume > _MaxVolume)
                _MaxVolume = volume;
            _MaxBar = 0;
            float maxVal = -99f;
            for (int i = 0; i < _Bars.Length; i++)
            {
                if (i < weights.Length)
                {
                    if (_Theme.DrawNegative == EOffOn.TR_CONFIG_OFF && weights[i] < 0)
                        _Bars[i] = 0f;
                    else
                    {
                        _Bars[i] = weights[i] * volume / _MaxVolume;
                        if (_Bars[i] > maxVal)
                        {
                            maxVal = _Bars[i];
                            _MaxBar = i;
                        }
                    }
                }
                else
                    _Bars[i] = 0f;
            }
        }

        public void Reset()
        {
            if (_Bars == null || _Bars.Length == 0)
                return;

            for (int i = 0; i < _Bars.Length; i++)
                _Bars[i] = 0f;
            _MaxBar = 0;
            _MaxVolume = 0f;
        }

        public void Draw()
        {
            if (_Bars == null || _Theme.Style != EEqualizerStyle.Columns)
                return;

            float dx = Rect.W / _Bars.Length;
            float scaleVal = (_Bars[_MaxBar] < 0.00001f) ? 0f : 1 / _Bars[_MaxBar];

            for (int i = 0; i < _Bars.Length; i++)
            {
                float value = _Bars[i] * scaleVal;
                var bar = new SRectF(Rect.X + dx * i, Rect.Y + Rect.H - value * Rect.H, dx - Space, value * Rect.H, Rect.Z);
                SColorF color = Color;
                if (i == _MaxBar)
                    color = MaxColor;

                CBase.Drawing.DrawRect(color, bar);

                if (Reflection)
                    CBase.Drawing.DrawRectReflection(color, bar, ReflectionSpace, ReflectionHeight);
            }
        }

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            _Theme.Color.Get(_PartyModeID, out Color);
            _Theme.MaxColor.Get(_PartyModeID, out MaxColor);

            MaxRect = _Theme.Rect;
            Space = _Theme.Space;
            Reflection = _Theme.Reflection.HasValue;
            if (Reflection)
            {
                Debug.Assert(_Theme.Reflection != null);
                ReflectionHeight = _Theme.Reflection.Value.Height;
                ReflectionSpace = _Theme.Reflection.Value.Space;
            }

            _Bars = new float[_Theme.NumBars];
            for (int i = 0; i < _Bars.Length; i++)
                _Bars[i] = 0f;
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
                W = 1;

            _Theme.Rect.W = Rect.W;

            H += stepH;
            if (H <= 0)
                H = 1;

            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}