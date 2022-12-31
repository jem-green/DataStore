using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StorageLibrary
{
    public class Storage<T> where T : struct
    {
        #region Fields

        private string _path = "";
        private string _name = "Storage";
        private DataHandler _handler;

        #endregion
        #region Constructors 

        /// <summary>
        /// Open or create a new store base on the type name
        /// </summary>
        public Storage()
        {
            // Open or create a new store base on the type name

            Type type = typeof(T);
            _name = type.Name;

            _handler = new DataHandler(_path, _name);
            if (_handler.Open() == false)
            {
                _handler.Reset();

                // The logic might be to use the generic structure
                // to create the fields list to create the new 

                IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
                byte index = 0; // Can only have 255 properties
                if (props.Count < 256)
                {
                    foreach (PropertyInfo prop in props)
                    {
                        byte flag = 0; // Flag 0 = normal, 1 = deleted, 2 = spare
                        DataHandler.Field field = new DataHandler.Field(prop.Name, flag, index, Type.GetTypeCode(prop.PropertyType), -1, false);
                        _handler.Add(field);
                        index++;
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException("More than 255 properties");
                }
            }
        }

        /// <summary>
        /// Reset, Open or create a new store based on the type
        /// </summary>
        /// <param name="reset"></param>
        public Storage(bool reset)
        {
            // Reset, Open or create a new store based on the type

            Type type = typeof(T);
            _name = type.Name;

            _handler = new DataHandler(_path, _name);
            if ((_handler.Open() == false) || (reset == true))
            {
                _handler.Reset();

                // The logic might be to use the generic structure
                // to create the fields list to create the new 

                IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
                byte index = 0; // Can only have 255 properties
                if (props.Count < 256)
                {
                    foreach (PropertyInfo prop in props)
                    {
                        byte flag = 0; // Flag 0 = normal, 1 = deleted, 2 = spare
                        DataHandler.Field field = new DataHandler.Field(prop.Name, flag, index, Type.GetTypeCode(prop.PropertyType), -1, false);
                        _handler.Add(field);
                        index++;
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException("More than 255 properties");
                }
            }
        }

        /// <summary>
        /// Reset, Open or create a new store based on the type with a specific name and location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <param name="reset"></param>
        public Storage(string path, string filename, bool reset)
        {
            Type type = typeof(T);
            _path = path;
            _name = filename;

            _handler = new DataHandler(_path, _name);
            if ((_handler.Open() == false) || (reset == true))
            {
                _handler.Reset();

                // The logic might be to use the generic structure
                // to create the fields list to create the new 

                IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
                byte index = 0; // Can only have 255 properties
                if (props.Count < 256)
                {
                    foreach (PropertyInfo prop in props)
                    {
                        byte flag = 0; // Flag 0 = normal, 1 = deleted, 2 = spare
                        DataHandler.Field field = new DataHandler.Field(prop.Name, flag,  index, Type.GetTypeCode(prop.PropertyType), -1, false);
                        _handler.Add(field);
                        index++;
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException("More than 255 properties");
                }
            }
        }

        #endregion
        #region Properties
        
        public DataHandler Handler
        {
            get
            {
                return (_handler);
            }
        }

        public int Size
        {
            get
            {
                return (_handler.Size);
            }
        }

        #endregion
        #region Methods

        /// <summary>
        /// Create data
        /// </summary>
        /// <param name="data"></param>
        public void Create(T data)
        {
            object[] row = new object[_handler.Items];
            IList<PropertyInfo> props = new List<PropertyInfo>(data.GetType().GetProperties());
            int item = 0;
            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(data, null);
                row[item] = propValue;
                item++;
            }
            _handler.Create(row);
        }

        /// <summary>
        /// Read the data at index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T Read(int index)
        {
            T data = new T();
            if ((index >= 0) && (index <= _handler.Size))    // Initial check to save processing
            {
                object[] row;
                row = _handler.Read(index);
                if (row != null)
                {
                    IList<PropertyInfo> props = new List<PropertyInfo>(data.GetType().GetProperties());
                    int item = 0;
                    foreach (PropertyInfo prop in props)
                    {
                        prop.SetValue(data, row[item]);
                        item++;
                    }
                }
            }
            return (data);
        }

        /// <summary>
        /// Update the data at index
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        public void Update(T data, int index)
        {
            if ((index >= 0) && (index <= _handler.Size))    // Inital check to save processing
            {
                object[] row = new object[_handler.Items];
                IList<PropertyInfo> props = new List<PropertyInfo>(data.GetType().GetProperties());
                int item = 0;
                foreach (PropertyInfo prop in props)
                {
                    object propValue = prop.GetValue(data, null);
                    row[item] = propValue;
                    item++;
                }
                _handler.Update(row,index);
            }
        }

        /// <summary>
        /// Delete the data at index
        /// </summary>
        /// <param name="index"></param>
        public void Delete(int index)
        {
            if ((index >= 0) && (index <= _handler.Size))    // Inital check to save processing
            {
                _handler.Delete(index);
            }
        }

        public IEnumerator GetEnumerator()
        {
            for (int cursor = 0; cursor < _handler.Size; cursor++)
            {
                // Return the current element and then on next function call 
                // resume from next element rather than starting all over again;
                T data = new T();
                object[] row = _handler.Read(cursor);
                if (row != null)
                {
                    IList<PropertyInfo> props = new List<PropertyInfo>(data.GetType().GetProperties());
                    int item = 0;
                    foreach (PropertyInfo prop in props)
                    {
                        prop.SetValue(data, row[item]);
                        item++;
                    }
                }
                yield return (data);
            }
        }

        #endregion
    }
}
