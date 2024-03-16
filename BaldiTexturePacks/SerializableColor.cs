using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace BaldiTexturePacks
{
    public class SerializableColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        [JsonIgnore]
        public Color unityColor => new Color(R / 255f, G / 255f, B / 255f, A / 255f);

        public static implicit operator Color(SerializableColor c) => c.unityColor;

        public static explicit operator SerializableColor(Color c) => new SerializableColor() { 
            R = (byte)((int)c.r * 255f),
            G = (byte)((int)c.g * 255f),
            B = (byte)((int)c.b * 255f),
            A = (byte)((int)c.a * 255f)
        };

        public override int GetHashCode()
        {
            return (R.ToString() + B.ToString() + G.ToString() + A.ToString()).GetHashCode();
        }

        public static bool operator ==(SerializableColor c1, SerializableColor c2) { return c1.GetHashCode() == c2.GetHashCode(); }
        public static bool operator !=(SerializableColor c1, SerializableColor c2) { return !(c1 == c2); }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
