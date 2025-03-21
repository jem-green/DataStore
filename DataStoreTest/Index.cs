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
        private static PersistentDatastore? _datastore;

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

            // Test the PersistentDatastore public methods

            _datastore = new PersistentDatastore(_path, _name);

            _datastore.New();
            _datastore.Open();
            if (_reset == true)
            {
                _datastore.Reset();
            }

            // Create properties
            // Set the Id to be the primary key

            _datastore.Add(new PersistentDatastore.FieldType("id", TypeCode.Int32, 4, false));
            _datastore.Add(new PersistentDatastore.FieldType("name", TypeCode.String, 10, true));

            // Read in some name data to test the Seek method

            TextReader tr = new StreamReader("boys.txt");
            int row = 199;
            do
            {
                string line = tr.ReadLine();
                if (line == null)
                {
                    break;
                }
                List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
                insert.Add(new KeyValuePair<string, object>("id", row));
                insert.Add(new KeyValuePair<string, object>("name", line));
                _datastore.Insert(insert);
                //_datastore.Create(insert);
                row--;
            } while (true);
            tr.Close();
            tr.Dispose();

            tr = new StreamReader("girls.txt");
            do
            {
                string line = tr.ReadLine();
                if (line == null)
                {
                    break;
                }
                List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
                insert.Add(new KeyValuePair<string, object>("id", row));
                insert.Add(new KeyValuePair<string, object>("name", line));
                _datastore.Insert(insert);
                //_datastore.Create(insert);
                row--;
            } while (true);
            tr.Close();
            tr.Dispose();

            // Seek the record

            row = _datastore.Seek("Bodhi");
            Console.WriteLine("Seek: " + row);
            Print(_datastore.Read(row));
            Console.WriteLine("----");
            //row = _datastore.Seek(199);
            //Console.WriteLine("Seek: " + row);
            //Print(_datastore.Read(row));

            // Print the sorted table

            PrintAll(_datastore.Read());

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

    }
}