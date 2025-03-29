//  Copyright (c) 2017, Jeremy Green All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Xml.Linq;

namespace DatastoreLibrary
{
    public class PersistentDatastore
    {
        #region Fields

        private string _path = "";
        private string _name = "";
        private string _index = "";
        private DataHandler _handler;
        private bool _open = false;

        public enum RetrievalType
        {
            EqualTo = 0,
            GreaterThan = 1,
            LessThan = 2,
            GreaterThanOrEqual = 3,
            LaterThanOrEqual = 4,
            Next = 5,
            Previous = 6,
            First = 7,
            Last = 8,
            Current = 9
        }

        public struct FieldType
        {
            string _name;
            TypeCode _typeCode;
            byte _length;
            bool _primary;

            public FieldType(string name, TypeCode type, byte length, bool primary)
            {
                _name = name;
                _typeCode = type;
                _length = length;
                _primary = primary;
            }

            public byte Length
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

            public TypeCode Type
            {
                set
                {
                    _typeCode = value;
                }
                get
                {
                    return (_typeCode);
                }
            }

            public bool Primary
            {
                set
                {
                    _primary = value;
                }
                get
                {
                    return (_primary);
                }
            }

            public override string ToString()
            {
                string s = _name + "," + _typeCode;
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
        /// Reset, Open or create a new data store
        /// </summary>
        public PersistentDatastore()
        {
            // Placeholder
        }

        /// <summary>
        /// specific name and location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        public PersistentDatastore(string path, string filename)
        {
            if ((path != null) && (path.Length > 0))
            {
                _path = path;
            }

            if ((filename != null) && (filename.Length > 0))
            {
                _name = filename;
                _index = filename;
            }
        }

        #endregion
        #region Properties

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

        public string Path
        {
            set
            {
                _path = value;
            }
            get
            {
                return (_path);
            }
        }

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
                return (_open);
            }
        }

        #endregion
        #region Methods
        #region Index

        public void Index()
        {
            // Need to delete the index and crate a new one
            if (_handler != null)
            {
                _handler.Index();
            }
        }

        #endregion
        #region General
        public void New()
        {
            New(_path, _name);
        }

        public void New(string path, string name)
        {
            if (_open == false)
            {
                _handler = new DataHandler(path, name);
                _open = _handler.New();
            }
        }

        public void Open()
        {
           Open(_path, _name);
        }

        public void Open(string path, string name)
        {
            if (_open == false)
            {
                _handler = new DataHandler(path, name);
                _open = _handler.Open();
            }
        }

        public void Close()
        {
            if (_open == true)
            {
                _open = !_handler.Close();
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
            Add(fieldType.Name, fieldType.Type, fieldType.Length, fieldType.Primary);
        }

        public void Add(string name, string type, byte length)
        {
            Add(name, TypeLookup(type), length, false);
        }

        public void Add(string name, string type)
        {
            Add(name, TypeLookup(type), 0, false);
        }

        public void Add(string name, TypeCode typeCode)
        {
            Add(name, typeCode, 0, false);
        }

        public void Add(string name, TypeCode typeCode, byte length)
        {
            Add(name, typeCode,length, false);
        }

        public void Add(string name, TypeCode typeCode, byte length, bool primary)
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
                field.Primary = primary;
                _handler.Add(field);
            }
        }

        public void Remove(int item)
        {
            if (_handler != null)
            {
                _handler.RemoveAt(item);
            }
        }

        public void Remove()
        {
            if (_handler != null)
            {
                for (int i = _handler.Items; i >= 0; i--)
                {
                    _handler.RemoveAt(i);
                }
            }
        }

        public void Set(int item, FieldType fieldType)
        {
            Set(item, fieldType.Name, fieldType.Type, fieldType.Length);
        }

        public void Set(int item, string name, string type, byte length)
        {
            Set(item, name, TypeLookup(type), length);
        }

        public void Set(int item, string name, string type)
        {
            Set(item, name, TypeLookup(type), 0);
        }

        public void Set(int item, string name, TypeCode typeCode)
        {
            Set(item, name, typeCode, 0);
        }

        public void Set(int item, string name, TypeCode typeCode, byte length)
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
                else if (field.Type == TypeCode.Boolean)
                {
                    field.Length = 1;
                }
                else if (field.Type == TypeCode.Int16)
                {
                    field.Length = 2;
                }
                else if (field.Type == TypeCode.Int32)
                {
                    field.Length = 4;
                }
                else if (field.Type == TypeCode.Int64)
                {
                    field.Length = 8;
                }
                else
                {
                    throw new NotImplementedException();
                }
                
                _handler.Set(field, item);
            }
        }

        // WHat do we return here an object / structure with the field structure

        public FieldType Get(int item)
        {
            FieldType field = new FieldType();

            if (_handler != null)
            {
                DataHandler.Property property = _handler.Get(item);
                field.Name = property.Name;
                field.Type = property.Type;
                field.Length = property.Length;
            }
            return (field);
        }

        public List<FieldType> Get()
        {
            List<FieldType> fields = new List<FieldType>();
            if (_handler != null)
            {
                for (int i = 0; i < _handler.Items; i++)
                {
                    DataHandler.Property property = _handler.Get(i);
                    FieldType field = new FieldType();
                    field.Name = property.Name;
                    field.Type = property.Type;
                    field.Length = property.Length;
                    fields.Add(field);
                }
            }
            return (fields);
        }

        #endregion
        #region Record

        /// <summary>
        /// Create new record at the end of the records
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
                            else if (_handler.Get(i).Type == TypeCode.Int16)
                            {
                                record[i] = Convert.ToInt16(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int32)
                            {
                                record[i] = Convert.ToInt32(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int64)
                            {
                                record[i] = Convert.ToInt64(entry.Value);
                            }
                            else
                            {
                                throw new NotImplementedException("Type not implemented " + _handler.Get(i).Type.ToString());
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
        /// Insert a new record ordered by the primary key
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public void Insert(List<KeyValuePair<string, object>> data)
        {
            if (_handler != null)
            {
                object[] record = new object[_handler.Items];
                bool inserted = false;
                object value = null;
                foreach (KeyValuePair<string, object> entry in data)
                {
                    bool match = false;

                    for (int i = 0; i < _handler.Items; i++)
                    {
                        if (entry.Key == _handler.Get(i).Name)
                        {
                            if (_handler.Get(i).Primary == true)
                            {
                                value = entry.Value;
                            }

                            if (_handler.Get(i).Type == TypeCode.String)
                            {
                                record[i] = Convert.ToString(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int16)
                            {
                                record[i] = Convert.ToInt16(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int32)
                            {
                                record[i] = Convert.ToInt32(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int64)
                            {
                                record[i] = Convert.ToInt64(entry.Value);
                            }
                            else
                            {
                                throw new NotImplementedException("Type not implemented " + _handler.Get(i).Type.ToString());
                            }
                            match = true;
                            inserted = true;
                        }
                    }
                    if (match == false)
                    {
                        throw new KeyNotFoundException("No such key " + entry.Key.ToString());
                    }
                }

                if (inserted == true)
                {
                    if ((_handler.Size == 0) || (value == null))
                    {
                        _handler.Create(record);
                    }
                    else
                    {
                        int row = _handler.Seek(value, DataHandler.SearchType.Greater);
                        if (row < 0)
                        {
                            _handler.Create(record);
                        }
                        else
                        {
                            _handler.Insert(record, row);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Insert a new record at the specified row
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void InsertAt(List<KeyValuePair<string, object>> data, int row)
        {
            if (_handler != null)
            {
                object[] record = new object[_handler.Items];
                bool inserted = false;
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
                            else if (_handler.Get(i).Type == TypeCode.Int16)
                            {
                                record[i] = Convert.ToInt16(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int32)
                            {
                                record[i] = Convert.ToInt32(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int64)
                            {
                                record[i] = Convert.ToInt64(entry.Value);
                            }
                            else
                            {
                                throw new NotImplementedException("Type not implemented " + _handler.Get(i).Type.ToString());
                            }
                            match = true;
                            inserted = true;
                        }
                    }
                    if (match == false)
                    {
                        throw new KeyNotFoundException("No such key " + entry.Key.ToString());
                    }
                }
                if (inserted == true)
                {
                    _handler.Insert(record, row);
                }
            }
        }

        /// <summary>
        /// Read all records
        /// </summary>
        /// <returns></returns>
        public List<List<KeyValuePair<string, object>>> Read()
        {
            List<List<KeyValuePair<string, object>>> records = new List<List<KeyValuePair<string, object>>>();
            if (_handler != null)
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
            return (records);
        }

        /// <summary>
        /// Read record
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public List<KeyValuePair<string, object>> Read(int row)
        {
            List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
            if (_handler != null)
            {
                object[] data;
                try
                {
                    data = _handler.Read(row);
                    for (int i = 0; i < data.Length; i++)
                    {
                        record.Add(new KeyValuePair<string, object>(_handler.Get(i).Name, data[i]));
                    }
                }
                catch
                {
                    data = null;
                }

            }
            return (record);
        }

        /// <summary>
        /// Update the record data at specified row
        /// </summary>
        /// <param name="row"></param>
        public void Update(List<KeyValuePair<string, object>> data, int row)
        {
            if (_handler != null)
            {

                // Think solution is to read in the data to an array
                // then update any fields then update the entire record

                object[] record = _handler.Read(row);
                bool updated = false;
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
                            else if (_handler.Get(i).Type == TypeCode.Int16)
                            {
                                record[i] = Convert.ToInt16(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int32)
                            {
                                record[i] = Convert.ToInt32(entry.Value);
                            }
                            else if (_handler.Get(i).Type == TypeCode.Int64)
                            {
                                record[i] = Convert.ToInt64(entry.Value);
                            }
                            else
                            {
                                throw new NotImplementedException("Type not implemented " + _handler.Get(i).Type.ToString());
                            }
                            match = true;
                            updated = true;
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

        /// <summary>
        /// Delete all the records
        /// </summary>
        public void Delete()
        {
            if (_handler != null)
            {
                for (int i = 0; i < _handler.Size; i++)
                {
                    _handler.Delete(i);
                }
            }
        }

        /// <summary>
        /// Delete the record at specified row
        /// </summary>
        /// <param name="row"></param>
        public void Delete(int row)
        {
            if (_handler != null)
            {
                _handler.Delete(row);
            }
        }

        #endregion
        #region Search

        public int Search(object value)
        {
            int row = -1;
            if (_handler != null)
            {
                row = _handler.Search(value);
            }
            return (row);
        }

        public int Find(object value)
        {
            return (Find(value, RetrievalType.EqualTo));
        }

        public int Find(object value, RetrievalType retrieval)
        {
            int row = -1;
            if (_handler != null)
            {
                if (_handler.Size > 0)
                {
                    switch (retrieval)
                    {
                        case RetrievalType.EqualTo:
                            {
                                row = _handler.Seek(value, DataHandler.SearchType.Equal);
                                break;
                            }
                        case RetrievalType.LessThan:
                            {
                                row = _handler.Seek(value, DataHandler.SearchType.Less);
                                break;
                            }
                        case RetrievalType.GreaterThan:
                            {
                                row = _handler.Seek(value, DataHandler.SearchType.Less);
                                break;
                            }
                    }
                }
            }
            return (row);
        }

        #endregion
        #endregion
        #region Private
        private TypeCode TypeLookup(string type)
        {
            TypeCode dataType = TypeCode.Int16;

            switch (type.ToUpper())
            {
                case "INT16":
                    {
                        dataType = TypeCode.Int16;
                        break;
                    }
                case "I":
                case "INT":
                case "INT32":
                    {
                        dataType = TypeCode.Int32;
                        break;
                    }
                case "LONG":
                case "INT64":
                    {
                        dataType = TypeCode.Int64;
                        break;
                    }
                case "S":
                case "STRING":
                    {
                        dataType = TypeCode.String;
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException("type not implemented " + type);
                    }
            }
            return (dataType);
        }
        #endregion
    }
}
