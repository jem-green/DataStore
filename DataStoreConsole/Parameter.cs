using System;

namespace StorageConsole
{
    public class Parameter
    {
        #region Fields
        string _key = "";
        object _value;
        SourceType _source = SourceType.None;

        public enum SourceType
        {
            None = 0,
            Command = 1,
            Registry = 2,
            App = 3
        }

        #endregion
        #region Constructor
        public Parameter(string key)
        {
            _key = key;
            _value = null;
            _source = SourceType.None;
        }
        public Parameter(string key, SourceType source)
        {
            _key = key;
            _value = null;
            _source = source;
        }
        public Parameter(string key, object value)
        {
            _key = key;
            _value = value;
            _source = SourceType.None;
        }
        public Parameter(string key, object value, SourceType source)
        {
            _key = key;
            _value = value;
            _source = source;
        }
        #endregion
        #region Parameters
        public string Key
        {
            set
            {
                _key = value;
            }
            get
            {
                return (_key);
            }
        }
        public object Value
        {
            set
            {
                _value = value;
            }
            get
            {
                return (_value);
            }
        }

        public SourceType Source
        {
            set
            {
                _source = value;
            }
            get
            {
                return (_source);
            }
        }
        #endregion
        #region Methods
        public override string ToString()
        {
            return (Convert.ToString(_value));
        }
        #endregion
    }
}
