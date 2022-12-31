using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StorageLibrary
{
    public class PersistentDataStore
    {
        #region Fields

        private string _path = "";
        private string _name = "Storage";
        private DataHandler _handler;

        public struct FieldType
        {
            string _name;
            string _type;
            sbyte _length;

            internal FieldType(string name, string type, sbyte length)
            {
                _name = name;
                _type = type;
                _length = length;
            }

            internal sbyte Length
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

            internal string Name
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


            internal string Type
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
        public PersistentDataStore()
        {
            // Reset, Open or create a new store based on the type

            _handler = new DataHandler(_path, _name);
            if (_handler.Open() == false)
            {
                _handler.Reset();
            }
        }

        /// <summary>
        /// Reset, Open or create a new store based on the type
        /// </summary>
        /// <param name="reset"></param>
        public PersistentDataStore(bool reset)
        {
            // Reset, Open or create a new store based on the type

            _handler = new DataHandler(_path, _name);
            if ((_handler.Open() == false) || (reset == true))
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
        public PersistentDataStore(string path, string filename, bool reset)
        {
            _path = path;
            _name = filename;

            _handler = new DataHandler(_path, _name);
            if ((_handler.Open() == false) || (reset == true))
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
                return (_handler.Fields.Length);
            }
        }

        #endregion
        #region Methods

        public void Add(string name, string type, sbyte length)
        {
            if (_handler != null)
            {
                DataHandler.Property field = new DataHandler.Property();
                field.Name = name;
                field.Type = TypeLookup(type);
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

        public void Set(int item, string name, string type, sbyte length)
        {
            if (_handler != null)
            {
                DataHandler.Property field = new DataHandler.Property();
                field.Name = name;
                field.Type = TypeLookup(type);
                if (field.Type == TypeCode.String)
                {
                    field.Length = length;
                }
                _handler.Set(field, item);
            }
        }

        public void Set(int item, FieldType fieldType)
        {
            if (_handler != null)
            {
                DataHandler.Property field = new DataHandler.Property();
                field.Name = fieldType.Name;
                field.Type = TypeLookup(fieldType.Type);
                if (field.Type == TypeCode.String)
                {
                    field.Length = fieldType.Length;
                }
                _handler.Set(field, item);
            }
        }

        // WHat do we return here an object / stuc with the field structure

        public FieldType Get(int item)
        {
            FieldType fieldType = new FieldType();

            if (_handler != null)
            {
                DataHandler.Property property = new DataHandler.Property();
                property = _handler.Get(item);
                fieldType.Name = property.Name;
                fieldType.Type = property.Type.ToString();
                if (property.Type == TypeCode.String)
                {
                    property.Length = property.Length;
                }
            }
            return (fieldType);
        }


        /// <summary>
        /// Delete the data at index
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
                        _handler.Delete(row,all);
                    }
                    else
                    {
                        _handler.Delete(row);
                    }
                }
            }
        }

        /// <summary>
        /// Creae new record
        /// </summary>
        /// <param name="data"></param>
        public void Create(object[] data)
        {
            if (_handler != null)
            {
                _handler.Create(data);
            }
        }

        /// <summary>
        /// Creae new record
        /// </summary>
        /// <param name="data"></param>
        public void Create(object[] data)
        {
            if (_handler != null)
            {
                _handler.Create(data);
            }
        }

        /// <summary>
        /// Read record at row
        /// </summary>
        /// <param name="row"></param>
        public string Read(int row, bool all)
        {
            StringBuilder builder = new StringBuilder();
            if (_handler != null)
            {
                if (all == true)
                {
                    for (int i = 0; i < _handler.Size; i++)
                    {
                        object[] data;
                        data = _handler.Read(i);
                        for (int j = 0; j < data.Length; j++)
                        {
                            // this will depend on the output formal which needs
                            // passing in via the command line but assume csv
                            builder.Append(_handler.Fields[j]);
                            builder.Append(",");
                            switch (_handler.Fields[j].Type)
                            {
                                case TypeCode.Int16:
                                    {
                                        builder.Append(data[j]);
                                        break;
                                    }
                                case TypeCode.String:
                                    {
                                        builder.Append("\"");
                                        builder.Append(data[j]);
                                        builder.Append("\"");
                                        break;
                                    }
                            }
                        }
                        builder.Append("\n");
                    }
                }
                else
                {
                    object[] data;
                    data = _handler.Read(row);
                    for (int j = 0; j < data.Length; j++)
                    {
                        // this will depend on the output format which needs
                        // passing in via the command line but assume csv
                        builder.Append(_handler.Fields[j]);
                        builder.Append(",");
                        switch (_handler.Fields[j].Type)
                        {
                            case TypeCode.Int16:
                                {
                                    builder.Append(data[j]);
                                    break;
                                }
                            case TypeCode.String:
                                {
                                    builder.Append("\"");
                                    builder.Append(data[j]);
                                    builder.Append("\"");
                                    break;
                                }
                        }
                    }
                }
                
            }
            return (builder.ToString());
        }


        /// <summary>
        /// Update the data at index
        /// </summary>
        /// <param name="row"></param>
        public void Update(string kvp, int row)
        {
            if (_handler != null)
            {
                if ((row >= 0) && (row <= _handler.Size))    // Inital check to save processing
                {
                    object[] data = new object[_handler.Fields.Length];
                    _handler.Update(data, row);
                }
            }
        }

        #endregion
        #region Private

        private TypeCode TypeLookup(string type)
        {
            TypeCode dataType = TypeCode.Int16;

            switch (type.ToUpper())
            {
                case "I":
                case "INT":
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
    }
}
