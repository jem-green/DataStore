using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DatastoreLibrary
{
    public class PersistentDatastore
    {
        #region Fields

        private string _path = "";
        private string _name = "";
        private DataHandler _handler;
        private bool _open = false;

        public struct FieldType
        {
            string _name;
            sbyte _length;
            string _type;

            public FieldType(string name, string type, sbyte length)
            {
                _name = name;
                _type = type;
                _length = length;
            }

            public sbyte Length
            {
                set
                {
                    _length = value;
                }
                get
                {
                    return (_length);
                }
            }

            public string Name
            {
                set
                {
                    _name = value;
                }
                get
                {
                    return (_name);
                }
            }

            public string Type
            {
                set
                {
                    _type = value;
                }
                get
                {
                    return (_type);
                }
            }

            public override string ToString()
            {
                string s = _name + "," + _type;
                if (_length > 0)
                {
                    s = s + "[" + _length + "]";
                }
                return (s);
            }
        }

        #endregion
        #region Constructors 

        /// <summary>
        /// Open or create a new data store
        /// </summary>
        public PersistentDatastore()
        {
            // Reset, Open or create a new store based on the type

            _handler = new DataHandler(_path, _name);
            if (_handler.Open() == false)
            {
                _handler.New();
            }
        }

        /// <summary>
        /// Reset, Open or create a new store based on the type
        /// </summary>
        /// <param name="reset"></param>
        public PersistentDatastore(bool reset)
        {
            // Reset, Open or create a new store based on the type

            _handler = new DataHandler(_path, _name);
            if (_handler.Open() == false)
            {
                _handler.New();
            }
            else if (reset == true)
            {
                _handler.Reset();
            }
        }

        /// <summary>
        /// Reset, Open or create a new store based on the type with a specific name and location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <param name="reset"></param>
        public PersistentDatastore(string path, string filename, bool reset)
        {
            if ((path != null) && (path.Length > 0))
            {
                _path = path;
            }

            if ((filename != null) && (filename.Length > 0))
            {
                _name = filename;
            }

            _handler = new DataHandler(_path, _name);
            if (_open == false)
            {
                _open = _handler.Open();
            }

            if (_open == false)
            {
                _handler.New();
            }
            else if (reset == true)
            {
                _handler.Reset();
            }
        }

        #endregion
        #region Properties

        public int Size
        {
            get
            {
                return (_handler.Size);
            }
        }

        public int Length
        {
            get
            {
                return (_handler.Items);
            }
        }

        public bool IsOpen
        {
            get
            {
                return(_open);
            }
        }

        #endregion
        #region Methods
        #region General
        public void New()
        {
            _handler.New();
        }

        public void Open()
        {
            if (_open == false)
            {
                _open =_handler.Open();
            }
        }

        public void Close()
        {
            if (_open == true)
            {
                _handler.Close();
            }
        }

        public void Reset()
        {
            if (_open == true)
            {
                _handler.Reset();
            }
        }

        #endregion
        #region Field

        public void Add(FieldType fieldType)
        {
            Add(fieldType.Name, fieldType.Type, fieldType.Length);
        }

        public void Add(string name, string type, sbyte length)
        {
            Add(name, TypeLookup(type), length);
        }

        public void Add(string name, TypeCode typeCode, sbyte length)
        {
            if (_handler != null)
            {
                DataHandler.Property field = new DataHandler.Property();
                field.Name = name;
                field.Type = typeCode;
                if (field.Type == TypeCode.String)
                {
                    field.Length = length;
                }
                _handler.Add(field);
            }
        }

        public void Remove(int item, bool all)
        {
            if (_handler != null)
            {
                if (all == true)
                {
                    for (int i = _handler.Items; i>=0;  i--)
                    {
                        _handler.RemoveAt(i);
                    }
                }
                else
                {
                    _handler.RemoveAt(item);
                }
            }
        }

        public void Set(int item, FieldType fieldType)
        {
            Set(item, fieldType.Name, fieldType.Type, fieldType.Length);
        }

        public void Set(int item, string name, string type, sbyte length)
        {
            Set(item, name, TypeLookup(type), length);
        }

        public void Set(int item, string name, TypeCode typeCode, sbyte length)
        {
            if (_handler != null)
            {
                DataHandler.Property field = new DataHandler.Property();
                field.Name = name;
                field.Type = typeCode;
                if (field.Type == TypeCode.String)
                {
                    field.Length = length;
                }
                _handler.Set(field, item);
            }
        }

        // WHat do we return here an object / stuc with the field structure

        public List<FieldType> Get(int item, bool all)
        {
            List<FieldType> fields = new List<FieldType>();

            if (_handler != null)
            {
                if (all == true)
                {
                    for (int i = 0; i < _handler.Items; i++)
                    {
                        DataHandler.Property property = _handler.Get(i);
                        FieldType field = new FieldType();
                        field.Name = property.Name;
                        field.Type = property.Type.ToString();
                        field.Length = property.Length;
                        fields.Add(field);
                    }
                }
                else
                {
                    DataHandler.Property property = _handler.Get(item);
                    FieldType field = new FieldType();
                    field.Name = property.Name;
                    field.Type = property.Type.ToString();
                    field.Length = property.Length;
                    fields.Add(field);
                }
            }
            
            return (fields);
        }

        #endregion
        #region Record

        /// <summary>
        /// Creae new record
        /// </summary>
        /// <param name="data"></param>
        public void Create(List<KeyValuePair<string, object>> data)
        {
            if (_handler != null)
            {
                object[] record = new object[_handler.Items];
                bool created = false;
                foreach (KeyValuePair<string, object> entry in data)
                {
                    bool match = false;
                    for (int i = 0; i < _handler.Items; i++)
                    {
                        if (entry.Key == _handler.Get(i).Name)
                        {
                            if (_handler.Get(i).Type == TypeCode.String)
                            {
                                record[i] = Convert.ToString(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int32)
                            {
                                record[i] = Convert.ToInt32(entry.Value);
                            }
                            match = true;
                            created = true;
                        }
                    }
                    if (match == false)
                    {
                        throw new KeyNotFoundException("No such key " + entry.Key.ToString());
                    }
                }
                if (created == true)
                {
                    _handler.Create(record);
                }
            }
        }

        /// <summary>
        /// Read record
        /// </summary>
        /// <param name="row"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public List<List<KeyValuePair<string, object>>> Read(int row, bool all)
        {
            List<List<KeyValuePair<string, object>>> records = new List<List<KeyValuePair<string, object>>>();
            if (_handler != null)
            {
                if (all == true)
                {
                    for (int i = 0; i < _handler.Size; i++)
                    {
                        object[] data;
                        data = _handler.Read(i);
                        List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                        for (int j = 0; j < data.Length; j++)
                        {

                            record.Add(new KeyValuePair<string, object>(_handler.Get(j).Name, data[j]));
                        }
                        records.Add(record);
                    }
                }
                else
                {
                    if (_handler.Size > 0)
                    {
                        object[] data;
                        data = _handler.Read(row);
                        List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                        for (int i = 0; i < data.Length; i++)
                        {
                            record.Add(new KeyValuePair<string, object>(_handler.Get(i).Name, data[i]));
                        }
                        records.Add(record);
                    }
                }
            }
            return(records);
        }

        /// <summary>
        /// Update the record data at index
        /// </summary>
        /// <param name="row"></param>
        public void Update(int row, List<KeyValuePair<string, object>> data)
        {
            if (_handler != null)
            {
                if ((row >= 0) && (row <= _handler.Items))    // Inital check to save processing
                {
                    // Thnk solution is to read in the data to an array
                    // then update any fields then update the entire record

                    object[] record = _handler.Read(row);
                    bool updated = false;
                    foreach(KeyValuePair<string,object> entry in data)
                    {
                        bool match = false;
                        for (int i=0; i<_handler.Items; i++)
                        {
                            if (entry.Key == _handler.Get(i).Name)
                            {
                                if (_handler.Get(i).Type == TypeCode.String)
                                {
                                    record[i] = Convert.ToString(entry.Value);
                                }
                                else if (_handler.Get(i).Type == TypeCode.Int32)
                                {
                                    record[i] = Convert.ToInt32(entry.Value);
                                }
                                match = true;
                                updated= true;
                            }
                        }
                        if (match == false)
                        {
                            throw new KeyNotFoundException("No such key " + entry.Key.ToString());
                        }
                    }
                    if (updated == true)
                    {
                        _handler.Update(record, row);
                    }
                }
            }
        }

        /// <summary>
        /// Delete the record at index
        /// </summary>
        /// <param name="row"></param>
        public void Delete(int row, bool all)
        {
            if (_handler != null)
            {
                if ((row >= 0) && (row <= _handler.Size))    // Inital check to save processing
                {
                    if (all == true)
                    {
                        for (int i = 0; i < _handler.Size; i++)
                        {
                            _handler.Delete(row);
                        }
                    }
                    else
                    {
                        _handler.Delete(row);
                    }
                }
            }
        }

        public TypeCode TypeLookup(string type)
        {
            TypeCode dataType = TypeCode.Int16;

            switch (type.ToUpper())
            {
                case "I":
                case "INT":
                case "INT32":
                    dataType = TypeCode.Int32;
                    break;
                case "INT16":
                    dataType = TypeCode.Int16;
                    break;
                case "S":
                case "STRING":
                    dataType = TypeCode.String;
                    break;
            }
            return (dataType);
        }

        #endregion
        #endregion
    }
}
