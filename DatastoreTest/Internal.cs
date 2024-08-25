﻿using DatastoreLibrary;

namespace DatastoreTests

{
    internal class Internal
    {
        #region Fields

        static string _name = String.Empty;
        static string _path = String.Empty;
        static bool _reset = false;
        //private DataHandler _datahandler;

        #endregion
        #region Constructors

        internal Internal()
        {
            _name = "internal";
            _path = "";
            _reset = true;
        }

        #endregion

        internal void Run()
        {
            string filenamepath = Path.Join(_path, _name) + ".dbf";
            if (File.Exists(filenamepath))
            {
                File.Delete(_name + ".dbf");
                File.Delete(_name + ".idx");
            }
            //_datahandler = new PersistentDatastore(_path, _name);

            /*

            _datahandler.New();
            _datahandler.Open();
            if (_reset == true)
            {
                _datahandler.Reset();
            }
            _datahandler.Add("id", "Int32", 0);
            _datahandler.Add(new PersistentDatastore.FieldType("name", "string", 10));

            // Create data
            // 0, id=0, name="hello"

            List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 0));
            create.Add(new KeyValuePair<string, object>("name", "hello"));
            _datahandler.Create(create);
            PrintAll(_datahandler.Read());

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="laura"

            create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 1));
            create.Add(new KeyValuePair<string, object>("name", "laura"));
            _datahandler.Create(create);
            PrintAll(_datahandler.Read());

            // Update data
            // 0, id=101, name="Jeremy"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "Jeremy"));
            _datahandler.Update(update, 0);
            PrintAll(_datahandler.Read());

            // insert data
            // 0, id=2, name="ash"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
            insert.Add(new KeyValuePair<string, object>("id", 2));
            insert.Add(new KeyValuePair<string, object>("name", "Ash"));
            _datahandler.Insert(insert, 0);
            PrintAll(_datahandler.Read());
        }

        #endregion
        #region Methods
        private void Print(List<KeyValuePair<string, object>> record)
        {
            for (int j = 0; j < record.Count; j++)
            {
                Console.Write("\"" + record[j].Key + "\"");
                Console.Write("=");
                TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                switch (typeCode)
                {
                    case TypeCode.String:
                        {
                            Console.Write("\"" + record[j].Value + "\"");
                            break;
                        }
                    default:
                        {
                            Console.Write(record[j].Value);
                            break;
                        }
                }
                if (j < record.Count - 1)
                {
                    Console.Write(",");
                }
            }
            Console.Write("\r\n");
        }

        private void PrintAll(List<List<KeyValuePair<string, object>>> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                List<KeyValuePair<string, object>> record = records[i];
                for (int j = 0; j < record.Count; j++)
                {
                    Console.Write("\"" + record[j].Key + "\"");
                    Console.Write("=");
                    TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                    switch (typeCode)
                    {
                        case TypeCode.String:
                            {
                                Console.Write("\"" + record[j].Value + "\"");
                                break;
                            }
                        default:
                            {
                                Console.Write(record[j].Value);
                                break;
                            }
                    }
                    if (j < record.Count - 1)
                    {
                        Console.Write(",");
                    }
                }
                Console.Write("\r\n");
            }
            Console.Write("\r\n");
        }
        #endregion
            */
        }
    }
}