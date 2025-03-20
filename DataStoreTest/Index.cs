using DatastoreLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatastoreTest
{
    class Index
    {
        #region Fields

        static string _name = String.Empty;
        static string _path = String.Empty;
        static bool _reset = false;
        static DataHandler _datahandler;

        #endregion
        #region Constructors

        internal Index()
        {
            _name = "index";
            _path = "";
            _reset = true;
        }

        #endregion
        #region Methods

        internal void Run()
        {
            string filenamepath = Path.Join(_path, _name) + ".dbf";
            if (File.Exists(filenamepath))
            {
                File.Delete(_name + ".dbf");
                File.Delete(_name + ".idx");
            }

            _datahandler = new DataHandler(_path, _name);
            _datahandler.New();
            _datahandler.Open();
            if (_reset == true)
            {
                _datahandler.Reset();
            }

            // Create properties
            // Set the Id to be the primary key

            _datahandler.Add(new DataHandler.Property("id", 0, 0, TypeCode.Int32, 0, false));
            _datahandler.Add(new DataHandler.Property("name", 0, 1, TypeCode.String, 10, true));

            // Create data
            // 0, id=0, name="hello"

            object[] create = new object[] { 1, "hello" };
            _datahandler.Create(create);

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="Laura"

            create = new object[] { 0, "Laura" };
            _datahandler.Create(create);

            // Seek the record

            int row = _datahandler.Seek("laura");
            Print(row);

        }

        #endregion
        #region Methods
        private void Print(int row)
        {
            object[] record = _datahandler.Read(row);
            for (int j = 0; j < record.Length; j++)
            {
                Console.Write("\"" + _datahandler.Get(j).Name + "\"");
                Console.Write("=");
                TypeCode typeCode = _datahandler.Get(j).Type;
                switch (typeCode)
                {
                    case TypeCode.String:
                        {
                            Console.Write("\"" + Convert.ToString(record[j]) + "\"");
                            break;
                        }
                    default:
                        {
                            Console.Write(record[j]);
                            break;
                        }
                }
                if (j < record.Length - 1)
                {
                    Console.Write(", ");
                }
            }
            Console.Write("\r\n");
        }

        private void PrintAll()
        {

            for (int i = 0; i < _datahandler.Size; i++)
            {
                Console.Write(i + ", ");
                object[] record = _datahandler.Read(i);
                for (int j = 0; j < record.Length; j++)
                {
                    Console.Write("\"" + _datahandler.Get(j).Name + "\"");
                    Console.Write("=");
                    TypeCode typeCode = _datahandler.Get(j).Type;
                    switch (typeCode)
                    {
                        case TypeCode.String:
                            {
                                Console.Write("\"" + Convert.ToString(record[j]) + "\"");
                                break;
                            }
                        default:
                            {
                                Console.Write(record[j]);
                                break;
                            }
                    }
                    if (j < record.Length - 1)
                    {
                        Console.Write(",");
                    }
                }
                Console.Write("\r\n");
            }
            Console.Write("\r\n");
        }
        #endregion

    }
}