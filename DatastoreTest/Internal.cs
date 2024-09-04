using DatastoreLibrary;

namespace DatastoreTest
{
    internal class Internal
    {
        #region Fields

        static string _name = String.Empty;
        static string _path = String.Empty;
        static bool _reset = false;
        static DataHandler _datahandler;

        #endregion
        #region Constructors

        internal Internal()
        {
            _name = "internal";
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

            _datahandler.Add(new DataHandler.Property("id", _datahandler, 0, TypeCode.Int32, 0, false));
            _datahandler.Add(new DataHandler.Property("name", 0, 1, TypeCode.String, 10, false));

            // Create data
            // 0, id=0, name="hello"

            object[] create = new object[] { 0, "hello" };
            _datahandler.Create(create);
            PrintAll();

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="Laura"

            create = new object[] { 1, "Laura" };
            _datahandler.Create(create);
            PrintAll();

            // Update data
            // 0, id=101, name="Jeremy"
            // 1, id=1, name="Laura"

            object[] update = new object[] { 101, "Jeremy" };
            _datahandler.Update(update, 0);
            PrintAll();

            // insert data
            // 0, id=2, name="Ash"
            // 1, id=101, name="Jeremy"
            // 2, id=1, name="Laura"

            object[] insert = new object[] { 2, "Ash" };
            _datahandler.Insert(insert, 0);
            PrintAll();
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