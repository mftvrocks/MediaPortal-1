#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Effects;
using Microsoft.DirectX.Direct3D;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Brushes
{
  public class LinearGradientBrush : GradientBrush
  {
    Texture _texture;
    double _height;
    double _width;
    EffectAsset _effect;

    Property _startPointProperty;
    Property _endPointProperty;
    float[] _offsets = new float[12];
    ColorValue[] _colors = new ColorValue[12];

    public LinearGradientBrush()
    {
      _startPointProperty = new Property((double)0.0f);
      _endPointProperty = new Property((double)1.0f);
    }

    public Property StartPointProperty
    {
      get
      {
        return _startPointProperty;
      }
      set
      {
        _startPointProperty = value;
      }
    }

    public Point StartPoint
    {
      get
      {
        return (Point)_startPointProperty.GetValue();
      }
      set
      {
        _startPointProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    public Property EndPointProperty
    {
      get
      {
        return _endPointProperty;
      }
      set
      {
        _endPointProperty = value;
      }
    }

    public Point EndPoint
    {
      get
      {
        return (Point)_endPointProperty.GetValue();
      }
      set
      {
        _endPointProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public override void SetupBrush(FrameworkElement element)
    {
      if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        if (_texture != null)
        {
          _texture.Dispose();
        }
        _height = element.ActualHeight;
        _width = element.ActualWidth;
        _texture = new Texture(GraphicsDevice.Device, 2, 2, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);

        int index = 0;
        foreach (GradientStop stop in GradientStops)
        {
          _offsets[index] = (float)stop.Offset;
          _colors[index] = ColorValue.FromColor(stop.Color);
          _colors[index].Alpha *= (float)Opacity;
          index++;
        }
        for (int i = index + 1; i < 12; i++)
        {
          _offsets[i] = 2.0f;
          _colors[i] = _colors[index];
        }
      }
    }
    public override void BeginRender()
    {
      if (_texture == null) return;
      _effect = ContentManager.GetEffect("lineargradient");
      _effect.Parameters["g_offset"] = _offsets;
      _effect.Parameters["g_color"] = _colors;
      _effect.StartRender(_texture);
    }

    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
        _effect = null;
      }
    }
  }
}
