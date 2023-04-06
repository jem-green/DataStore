using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DatastoreLibrary;

namespace DatastoreTest
{
    class Performance
    {
        #region Fields

        static string _name = "performance";
        static string _path = "";
        static bool _reset = true;
        private static PersistentDatastore? _datastore;

        #endregion

        static void Main(string[] args)
        {
            /*
Records=1000
Created 1000 records in 00:00:07.5049948
Records per second 133.24459598559613
Read 1000 records in 00:00:07.6445837
Records per second 130.81157055027077
Records=10000
Created 10000 records in 00:01:26.8498778
Records per second 115.14120978993479
Read 10000 records in 00:01:27.7309343
Records per second 113.98487978943135
Records=100000
Created 100000 records in 00:15:10.1075537
Records per second 109.87712341629803
Read 100000 records in 00:15:14.6281130
Records per second 37.68088856022295
             */

            int records;
            //records = 1000;
            //SequentialTest(records);

            //records = 10000;
            //SequentialTest(records);

            //records = 100000;
            //SequentialTest(records);

            records = 1000;
            RandomTest(records);

            records = 10000;
            RandomTest(records);

            records = 65534;
            RandomTest(records);

        }

        public static void RandomTest(int records)
        {
            _datastore = new PersistentDatastore(_path, _name, _reset);

            Console.WriteLine("Records={0}", records);

            // Create an array of Tests

            int[] items = new int[records];
            for (int i = 0; i < records; i++)
            {
                items[i] = i;
            }

            // Shuffle the list using Fisher-Yates method

            Random rng = new Random();
            int n = items.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = items[k];
                items[k] = items[n];
                items[n] = value;
            }

            _datastore.Add("id", "Int32", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", "string", 10));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < records; i++)
            {
                List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
                create.Add(new KeyValuePair<string, object>("id", i));
                create.Add(new KeyValuePair<string, object>("name", "hello"));
                _datastore.Create(create);  // Create a new record       
            }
            sw.Stop();
            Console.WriteLine("Created {0} records in {1}", records, sw.Elapsed);
            Console.WriteLine("Records per second {0}", records / sw.Elapsed.TotalSeconds);

            sw.Start();
            for (int i = 0; i < _datastore.Size; i++)
            {
                List<Dictionary<string, object>> record = _datastore.Read();           // Read a record
            }
            sw.Stop();
            Console.WriteLine("Read {0} records in {1}", records, sw.Elapsed);
            Console.WriteLine("Records per second {0}", _datastore.Size / sw.Elapsed.TotalSeconds);
        }

        public static void SequentialTest(int records)
        {
            _datastore = new PersistentDatastore(_path, _name, _reset);

            Console.WriteLine("Records={0}", records);

            // Create an array of Tests

            int[] items = new int[records];
            for (int i = 0; i < records; i++)
            {
                items[i] = i;
            }

            _datastore.Add("id", "Int32", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", "string", 10));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < records; i++)
            {
                List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
                create.Add(new KeyValuePair<string, object>("id", i));
                create.Add(new KeyValuePair<string, object>("name", "hello"));
                _datastore.Create(create);  // Create a new record       
            }
            sw.Stop();
            Console.WriteLine("Created {0} records in {1}", records, sw.Elapsed);
            Console.WriteLine("Records per second {0}", records / sw.Elapsed.TotalSeconds);

            sw.Start();
            for (int i = 0; i < _datastore.Size; i++)
            {
                List<Dictionary<string, object>> record = _datastore.Read();           // Read a record
            }
            sw.Stop();
            Console.WriteLine("Read {0} records in {1}", records, sw.Elapsed);
            Console.WriteLine("Records per second {0}", _datastore.Size / sw.Elapsed.TotalSeconds);

        }
    }
}