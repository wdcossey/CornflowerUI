using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RefApp
{
    /// <summary>A minimal JSON writer: the phone's .NET profile has no serializer worth the setup.</summary>
    internal sealed class Json
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Stack<bool> _first = new Stack<bool>();
        private bool _afterKey;

        public Json Obj()
        {
            Separate();
            _sb.Append('{');
            _first.Push(true);
            return this;
        }

        public Json EndObj()
        {
            _first.Pop();
            _sb.Append('}');
            return this;
        }

        public Json Arr()
        {
            Separate();
            _sb.Append('[');
            _first.Push(true);
            return this;
        }

        public Json EndArr()
        {
            _first.Pop();
            _sb.Append(']');
            return this;
        }

        public Json Key(string key)
        {
            Separate();
            Str(key);
            _sb.Append(':');
            _afterKey = true;
            return this;
        }

        public Json Val(string value)
        {
            Separate();
            if (value == null)
            {
                _sb.Append("null");
            }
            else
            {
                Str(value);
            }

            return this;
        }

        public Json Val(double value)
        {
            Separate();
            _sb.Append(double.IsNaN(value) || double.IsInfinity(value)
                ? "null"
                : Math.Round(value, 4).ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        public Json Val(bool value)
        {
            Separate();
            _sb.Append(value ? "true" : "false");
            return this;
        }

        public Json Prop(string key, string value) => Key(key).Val(value);

        public Json Prop(string key, double value) => Key(key).Val(value);

        public Json Prop(string key, bool value) => Key(key).Val(value);

        public override string ToString() => _sb.ToString();

        private void Separate()
        {
            if (_afterKey)
            {
                _afterKey = false;
                return;
            }

            if (_first.Count == 0)
            {
                return;
            }

            if (_first.Peek())
            {
                _first.Pop();
                _first.Push(false);
            }
            else
            {
                _sb.Append(',');
            }
        }

        private void Str(string s)
        {
            _sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _sb.Append(c);
                        }

                        break;
                }
            }

            _sb.Append('"');
        }
    }
}
