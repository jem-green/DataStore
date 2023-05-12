using System;
using System.Collections.Generic;
using System.Runtime;

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
        #region General
        public void New()
        {
            if (_open == false)
            {
                _handler = new DataHandler(_path, _name);
                _open = _handler.New();
            }
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
            if (_open == false)
            {
                _handler = new DataHandler(_path, _name);
                _open = _handler.Open();
            }
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
            Add(fieldType.Name, fieldType.Type, fieldType.Length);
        }

        public void Add(string name, string type, sbyte length)
        {
            Add(name, TypeLookup(type), length);
        }

        public void Add(string name, string type)
        {
            Add(name, TypeLookup(type), -1);
        }

        public void Add(string name, TypeCode typeCode)
        {
            Add(name, typeCode, -1);
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

        public void Set(int item, string name, string type, sbyte length)
        {
            Set(item, name, TypeLookup(type), length);
        }

        public void Set(int item, string name, string type)
        {
            Set(item, name, TypeLookup(type), -1);
        }

        public void Set(int item, string name, TypeCode typeCode)
        {
            Set(item, name, typeCode, -1);
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

        // WHat do we return here an object / stuc with the field structure

        public FieldType Get(int item)
        {
            FieldType field = new FieldType();

            if (_handler != null)
            {
                DataHandler.Property property = _handler.Get(item);
                field.Name = property.Name;
                field.Type = property.Type.ToString();
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
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void Insert(List<KeyValuePair<string, object>> data, int row)
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
                data = _handler.Read(row);
                for (int i = 0; i < data.Length; i++)
                {
                    record.Add(new KeyValuePair<string, object>(_handler.Get(i).Name, data[i]));
                }
            }
            return (record);
        }

        /// <summary>
        /// Update the record data at index
        /// </summary>
        /// <param name="row"></param>
        public void Update(List<KeyValuePair<string, object>> data, int row)
        {
            if (_handler != null)
            {

                // Thnk solution is to read in the data to an array
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
        /// Delete the record at index
        /// </summary>
        /// <param name="row"></param>
        public void Delete(int row)
        {
            if (_handler != null)
            {
                _handler.Delete(row);
            }
        }

        public TypeCode TypeLookup(string type)
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
        #endregion
    }
}
