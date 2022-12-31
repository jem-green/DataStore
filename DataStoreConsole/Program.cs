using System;
using StorageLibrary;
using System.CommandLine;
using System.CommandLine.Parser;
using System.CommandLine.Invocation;

namespace StorageConsole
{
    /// <summary>
    /// Simple command line to return results
    /// More like the docker command line so
    /// not a query language just parameters
    /// </summary>
    class Program
    {
        // Need the name and location of the file to read
        // could pass this as a single parameter if nothing
        // else is supplied
        // so --name --path
        // and log to the console all the data in cvs format

        #region Fields

        static string _name = "store";
        static string _path = "";
        bool reset;
        static PersistentDataStore _dataStore;

        #endregion

        static int Main(string[] args)
        {
            var root = new RootCommand()
            {
                 new Option<string>(new string[] { "--name", "-N" }, description: "The data store name"),
                 new Option<string>(new string[] { "--path", "-P" }, description: "The data stre path"),
                 new Option<bool>(new string[] { "--reset", "-R" }, description: "Reset the data store"),
            };
            root.Handler = CommandHandler.Create<string, string, bool>(HandleRoot);

            Command set = new Command("set", "Set field")
            {
                new Option<string>(new string[] { "--name", "-n" }, description: "The field name"),
                new Option<string>(new string[] { "--type", "-T" }, description: "The field type"),
                new Option<int>(new string[] { "--length", "-L" }, description: "The string field length"),
            };
            set.Handler = CommandHandler.Create<string,string,sbyte>(HandleSet);

            Command delete = new Command("delete", "Delete record by index")
            {
                new Argument<int>("index","Record index")
            };
            delete.Handler = CommandHandler.Create<int>(HandleDelete);

            Command update = new Command("update", "Update record by index")
            {
                new Option<int>(new string[] { "--row", "-R" }, description: "The record row"),
                new Option(new string[] { "--all", "-A" }, description: "All records"),
            };
            update.Handler = CommandHandler.Update<int, object>(HandleUpdate);

            Command create = new Command("create", "Create new record")
            {
                new Option<int>(new string[] { "--row", "-R" }, description: "The record row"),
                new Option(new string[] { "--all", "-A" }, description: "All records"),
            };
            create.Handler = CommandHandler.Create<int, object>(HandleCreate);

            root.Add(set);
            root.Add(delete);
            root.Add(update);
            root.Add(create);

            return root.Invoke(args);
        }

        private static void HandleSet(string name, string type, sbyte length)
        {
            // Assume that the datastore has been opened
            // Assume we can add the fields in order

            if (_dataStore != null)
            {
                int item = _dataStore.Length;
                item++;
                _dataStore.Set(item, name, type, length);
            }
        }

        private static void HandleGet(int item)
        {
            // Assume that the datastore has been opened
            if (_dataStore != null)
            {
                _dataStore.Get(item);
            }
        }

        private static void HandleRoot(string name, string path, bool reset)
        {
            if (name != "")
            {
                _name = name;
            }
            if (path != "")
            {
                _path = path;
            }
            _dataStore = new PersistentDataStore(_path,_name, reset);
        }

        private static void HandleCreate(int row, object data)
        {
            // Assume that the datastore has been opened
            if (_dataStore != null)
            {
                string[] data = new string[_dataStore.Length];
                _dataStore.Create(data, row);
            }
        }

        private static string HandleRead(int row, bool all)
        {
            // Assume that the datastore has been opened
            // Need to build the list of fields to form a record
            // The list must be in order

            string data = "";
            if (_dataStore != null)
            {
                data = _dataStore.Read(row, all);
            }
            return(data);
        }

        private static void HandleDelete(int row, bool all)
        {
            // Assume that the datastore has been opened
            if (_dataStore != null)
            {
                _dataStore.Delete(row, all);
            }
        }

        private static void HandleUpdate(int row, object data)
        {
            // Assume that the datastore has been opened
            // Need to build the list of fields to form a record
            // The list must be in order

            if (_dataStore != null)
            {
                string[] data = new string[_dataStore.Length];
                _dataStore.Update(data, row);
            }
        }

    }
}
