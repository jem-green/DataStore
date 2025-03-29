using System;
using DatastoreLibrary;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace DatastoreConsole
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
        // else is supplied so --name --path
        // and log to the console all the data in KVP format
        //
        // Suggested commands match the methods
        //
        // General methods (OCRN)
        // Open -
        // Close - 
        // Reset -
        // Index -Make
        // New   -
        //
        // Field methods (ARSG)
        // Add -
        // Remove -
        // Set -
        // Get -
        // 
        // Record methods (CRUD) 
        // Create -
        // Read -
        // Update -
        // Delete - 
        //
        // Search methods (F)
        // Find -
        // 
        // 
        // DatastoreConsole --filename "datastore" --filepath "c:\"
        // DatastoreConsole --filename "datastore" --filepath "c:\" OPEN [--reset]
        // DatastoreConsole --filename "datastore" --filepath "c:\" CLOSE
        // DatastoreConsole --filename "datastore" --filepath "c:\" RESET
        // DatastoreConsole --filename "datastore" --filepath "c:\" NEW
        // DatastoreConsole --filename "datastore" --filepath "c:\" ADD --field "f" --type "t" --length l
        // DatastoreConsole --filename "datastore" --filepath "c:\" REMOVE --item i | --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" SET --item --filename --type --length
        // DatastoreConsole --filename "datastore" --filepath "c:\" GET --item i | --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" CREATE --field --value [--field --value]
        // DatastoreConsole --filename "datastore" --filepath "c:\" CREATE --data "field=value,field1=value1]
        // DatastoreConsole --filename "datastore" --filepath "c:\" READ --row r | --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" UPDATE --row --field "f" --value "v" [--field --value]
        // DatastoreConsole --filename "datastore" --filepath "c:\" UPDATE --row --data "field=value[,field1=value1]"
        // DatastoreConsole --filename "datastore" --filepath "c:\" DELETE --row r | --all

        // DatastoreConsole "c:\datastore" READ --row -all
        #region Fields

        static private PersistentDatastore _dataStore;
        static private Command _command = Command.None;
        static private bool _help = false;
        static private bool _version = false;
        static bool _readInput = false;
		
		// Required for the datastore

        static Parameter<bool> _all = new Parameter<bool>("all", false);
        static Parameter<string> _data = new Parameter<string>("data", "");
        static Parameter<string> _field = new Parameter<string>("field", "");
        static Parameter<List<string>> _fields = new Parameter<List<string>>("fields", new List<string>());
        static Parameter<string> _filename = new Parameter<string>("filename", "");
        static Parameter<string> _filePath = new Parameter<string>("filepath", "");
        static Parameter<int> _item = new Parameter<int>("item", 0);
        static Parameter<byte> _length = new Parameter<byte>("length", 0);
        static Parameter<bool> _reset = new Parameter<bool>("reset", false);
        static Parameter<int> _row = new Parameter<int>("row", -1);
        static Parameter<TypeCode> _type = new Parameter<TypeCode>("type", TypeCode.Empty);
        static Parameter<object> _value = new Parameter<object>("value", null);
        static Parameter<List<object>> _values = new Parameter<List<object>>("values", new List<object>());

        internal enum Command
        {
            None = 0,
            Open = 1,
            Close = 2,
            Reset = 3,
            New = 4,
            Add = 5,
            Remove = 6,
            Set = 7,
            Get = 8,
            Create = 9,
            Read = 10,
            Update = 11,
            Delete = 12,
            Insert = 13,
            Find = 14
        }

        #endregion
        #region Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
		
        static int Main(string[] args)
        {
            // Read in specific configuration

            Debug.WriteLine("Enter Main()");
            int errorCode = -2;

            if (args.Length > 0)
            {
                if (ValidateArguments(args))
                {
                    PreProcess(args);
                    if (_version)
                    {
                        Console.WriteLine("datastore " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                        errorCode = 0;
                    }
                    else
                    {
                        if (_readInput)
                        {
                            string currentLine = Console.In.ReadLine();
                            while (currentLine != null)
                            {
                                //ProcessLine(currentLine);
                                currentLine = Console.In.ReadLine();
                            }
                        }
                        errorCode = PostProcess(args);
                    }
                }
                else
                {
                    Console.Error.Write(Usage());
                    errorCode = -1;
                }
            }
            else
            {
                Console.Error.Write(Usage());
                errorCode = -1;
            }

            Debug.WriteLine("Exit Main()");
            return (errorCode);
        }

        private static bool ValidateArguments(string[] args)
        {
            // Passed args allow for changes to web address and application location

            bool path = false;
            bool filename = false;
            bool help = false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/f":
                    case "--filename":
                        {
                            filename = true;
                            break;
                        }
                    case "/?":
                    case "--?":
                    case "/h":
                    case "/H":
                    case "--help":
                        {
                            help = true;
                            break;
                        }
                }
            }

            if ((filename == true) || ((filename == false) && (help == false)))
            {
                return (true);
            }
            else
            {
                return (false);
            }

        }

        private static void PreProcess(string[] args)
        {
            Debug.WriteLine("Enter PreProcess()");

            string filenamePath = "";
            string extension = "";
            int pos = 0;

            int arguments = args.Length;
            if (arguments == 1)
            {
                // Default to reading all the data

                _command = Command.Read;
                _all.Value = true;

                filenamePath = args[0].Trim('"');
                pos = filenamePath.LastIndexOf('.');
                if (pos > 0)
                {
                    extension = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    filenamePath = filenamePath.Substring(0, pos);
                }

                pos = filenamePath.LastIndexOf('\\');
                if (pos > 0)
                {
                    _filePath.Value = filenamePath.Substring(0, pos);
                    _filePath.Source = IParameter.SourceType.Command;
                    _filename.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    _filename.Source = IParameter.SourceType.Command;
                }
                else
                {
                    _filename.Value = filenamePath;
                    _filename.Source = IParameter.SourceType.Command;
                }
            }

            else
            {

                for (int element = 0; element < args.Length; element++)
                {
                    string lookup = args[element];
                    if (lookup.Length > 1)
                    {
                        lookup = lookup.ToLower();
                    }
                    switch (lookup)
                    {
                        case "/v":
                        case "--version":
                            {
                                _version = true;
                                break;
                            }
                        case "/h":
                        case "/?":
                        case "--help":
                            {
                                _help = true;
                                break;
                            }

                        // General

                        case "open":
                            {
                                _command = Command.Open;
                                break;
                            }
                        case "close":
                            {
                                _command = Command.Close;
                                break;
                            }
                        case "reset":
                            {
                                _command = Command.Reset;
                                break;
                            }
                        case "new":
                            {
                                _command = Command.New;
                                break;
                            }

                        // Field

                        case "add":
                            {
                                _command = Command.Add;
                                break;
                            }
                        case "remove":
                            {
                                _command = Command.Remove;
                                break;
                            }
                        case "set":
                            {
                                _command = Command.Set;
                                break;
                            }
                        case "get":
                            {
                                _command = Command.Get;
                                break;
                            }

                        // Record

                        case "create":
                            {
                                _command = Command.Create;
                                break;
                            }
                        case "read":
                            {
                                _command = Command.Read;
                                break;
                            }
                        case "update":
                            {
                                _command = Command.Update;
                                break;
                            }
                        case "delete":
                            {
                                _command = Command.Delete;
                                break;
                            }

                        // Index

                        case "find":
                            {
                                _command = Command.Find;
                                break;
                            }

                        case "/a":
                        case "--all":
                            {
                                _all.Value = true;
                                _all.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/f":
                        case "--filename":
                            {
                                _filename.Value = args[++element];
                                _filename.Value = _filename.Value.TrimStart('"');
                                _filename.Value = _filename.Value.TrimEnd('"');
                                _filename.Source = IParameter.SourceType.Command;
                                pos = _filename.Value.LastIndexOf('.');
                                if (pos > 0)
                                {
                                    extension = _filename.Value.Substring(pos + 1, _filename.Value.Length - pos - 1);
                                    _filename.Value = _filename.Value.Substring(0, pos);
                                }
                                break;
                            }
                        case "/p":
                        case "--filepath":
                            {
                                _filePath.Value = args[++element];
                                _filePath.Value = _filePath.Value.TrimStart('"');
                                _filePath.Value = _filePath.Value.TrimEnd('"');
                                _filePath.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/F":
                        case "--field":
                            {
                                if (_command != Command.None)
                                {
                                    string fieldName = args[++element];
                                    fieldName = fieldName.TrimStart('"');
                                    fieldName = fieldName.TrimEnd('"');

                                    if ((_command == Command.Set) || (_command == Command.Add))
                                    {
                                        _field.Value = fieldName;
                                        _field.Source = IParameter.SourceType.Command;
                                    }
                                    else if ((_command == Command.Create) || (_command == Command.Update))
                                    {
                                        _fields.Value.Add(fieldName);
                                        _fields.Source = IParameter.SourceType.Command;
                                    }
                                }
                                break;
                            }
                        case "/i":
                        case "--item":
                            {
                                string itemName = args[++element];
                                itemName = itemName.TrimStart('"');
                                itemName = itemName.TrimEnd('"');
                                _item.Value = Convert.ToInt32(itemName);
                                _item.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/l":
                        case "--length":
                            {
                                string lengthName = args[++element];
                                lengthName = lengthName.TrimStart('"');
                                lengthName = lengthName.TrimEnd('"');
                                _length.Value = Convert.ToByte(lengthName);
                                _length.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/r":
                        case "--reset":
                            {
                                _reset.Value = true;
                                _reset.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/R":
                        case "--row":
                            {
                                string rowName = args[++element];
                                rowName = rowName.TrimStart('"');
                                rowName = rowName.TrimEnd('"');
                                _row.Value = Convert.ToInt32(rowName);
                                _row.Source = IParameter.SourceType.Command;
                                break;
                            }
                        case "/t":
                        case "--type":
                            {
                                string typeName = args[++element];
                                typeName = typeName.TrimStart('"');
                                typeName = typeName.TrimEnd('"');

                                if ((_command == Command.Set) || (_command == Command.Add))
                                {
                                    _type.Value = TypeLookup(typeName);
                                    _type.Source = IParameter.SourceType.Command;
                                }
                                break;
                            }
                        case "/V":
                        case "--value":
                            {
                                string valueName = args[++element];
                                valueName = valueName.TrimStart('"');
                                valueName = valueName.TrimEnd('"');

                                if ((_command == Command.Set) || (_command == Command.Add) || (_command == Command.Find))
                                {
                                    _value.Value = valueName;
                                    _value.Source = IParameter.SourceType.Command;
                                }
                                else if ((_command == Command.Create) || (_command == Command.Update))
                                {
                                    _values.Value.Add(valueName);
                                    _values.Source = IParameter.SourceType.Command;
                                }
                                break;
                            }
                    }
                }
            }
            _readInput = false; // Indicate that we don't need to do a read input

            Debug.WriteLine("Exit PreProcess()");
        }

        private static int PostProcess(string[] args)
        {
            Debug.WriteLine("Enter PostProcess()");

            int errorCode = 1;

            // Check that the datastore has been opened

            _dataStore = new PersistentDatastore(_filePath.Value, _filename.Value);
            _dataStore.Open();

            if ((_dataStore.IsOpen == true) || (_command == Command.New))
            {
                switch (_command)
                {
                    // General

                    case Command.Open:
                        {
                            // Potentially reopen

                            string output = "OPEN";
                            if (_help == true)
                            {
                                output += " --filename FILE [--filepath PATH] OPEN";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                if (_dataStore.IsOpen == false)
                                {
                                    _dataStore.Open();
                                }
                                Console.WriteLine(output);
                            }
                            break;
                        }
                    case Command.Close:
                        {
                            string output = "CLOSE";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] RESET";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                _dataStore.Close();
                                Console.WriteLine(output);
                            }
                            break;
                        }
                    case Command.Reset:
                        {
                            string output = "RESET";
                            if (_help == true)
                            {
                                output += " --filename \"FILE\" [--filepath \"PATH\"] RESET";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                _dataStore.Reset();
                                Console.WriteLine(output);
                            }
                            break;
                        }
                    case Command.New:
                        {
                            string output = "NEW";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] NEW";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                _dataStore.New();
                                Console.WriteLine(output);
                            }
                            break;
                        }

                    // Field

                    case Command.Add:
                        {
                            string output = "ADD";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --field FIELD --type TYPE [--length LENGTH]";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                output = output + String.Format(" --field \"{0}\" --type \"{1}\" --length {2}", _field.Value, _type.Value, _length.Value);
                                Console.WriteLine(output);
                                _dataStore.Add(_field.Value, _type.Value, _length.Value,false);
                            }
                            break;
                        }
                    case Command.Remove:
                        {
                            string output = "REMOVE";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --all | --item INDEX";
                                Console.WriteLine(output);
                            }
                            else if (_all.Value == true)
                            {
                                output = output + " --all";
                                Console.WriteLine(output);
                                _dataStore.Remove();
                            }
                            else
                            {
                                output = output + String.Format(" --item {0}", _item.Value);
                                Console.WriteLine(output);
                                _dataStore.Remove(_item.Value);
                            }
                            break;
                        }
                    case Command.Set:
                        {
                            string output = "SET";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --item INDEX --field \"FIELD\" --type \"TYPE\" --length LENGTH";
                                Console.WriteLine(output);
                            }
                            else if (_all.Value == true)
                            {
                                output = output + String.Format(" --item {0} --field \"{1}\" --type \"{2}\" --length {3}", ~_item.Value, _field.Value, _type.Value, _length.Value);
                                Console.WriteLine(output);
                                _dataStore.Set(_item.Value, _field.Value, _type.Value, _length.Value);
                            }
                            break;
                        }
                    case Command.Get:
                        {
                            string output = "GET";
                            StringBuilder builder = new StringBuilder();
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --all | --item INDEX";
                                Console.WriteLine(output);
                            }
                            else if (_all.Value == true)
                            {
                                output = output + " --all";
                                Console.WriteLine(output);
                                List<PersistentDatastore.FieldType> fields = _dataStore.Get();
                                for (int j = 0; j < fields.Count; j++)
                                {
                                    builder.Append("\"" + fields[j].Name + "\"");
                                    if (fields[j].Primary == true)
                                    {
                                        builder.Append('*');
                                    }
                                    builder.Append('[');
                                    builder.Append(fields[j].Type);
                                    if (fields[j].Type == TypeCode.String)
                                    {
                                        builder.Append('=');
                                        builder.Append(fields[j].Length);
                                    }
                                    builder.Append(']');

                                    if (j < fields.Count - 1)
                                    {
                                        builder.Append(',');
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                            }
                            else
                            {
                                output = output + " --item " + _item.Value;
                                Console.WriteLine(output);
                                PersistentDatastore.FieldType field = _dataStore.Get(_item.Value);

                                builder.Append("\"" + field.Name + "\"");
                                builder.Append('[');
                                builder.Append(field.Type);
                                if (field.Type == TypeCode.String)
                                {
                                    builder.Append('=');
                                    builder.Append(field.Length);
                                }
                                builder.Append(']');
                                Console.WriteLine(builder.ToString());
                            }
                            break;
                        }     

                    // Record

                    case Command.Create:
                        {
                            string output = "CREATE";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --field \"FIELD\" --value \"VALUE\"";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                                {
                                    if (_fields.Value.Count == _values.Value.Count)
                                    {
                                        List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                                        for (int i = 0; i < _fields.Value.Count; i += 1)
                                        {
                                            output = output + " --field \"" + _fields.Value[i] + "\" --value \"" + _values.Value[i] + "\" ";
                                            record.Add(new KeyValuePair<string, object>(_fields.Value[i], _values.Value[i]));
                                        }
                                        if (record.Count > 0)
                                        {
                                            _dataStore.Create(record);
                                        }
                                        Console.WriteLine(output);
                                    }
                                }
                            }
                            break;
                        }
                    case Command.Insert:
                        {
                            string output = "INSERT";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --row ROW";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                                {
                                    if (_fields.Value.Count == _values.Value.Count)
                                    {
                                        output = output + " --row " + _row.Value;
                                        List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                                        for (int i = 0; i < _fields.Value.Count; i += 1)
                                        {
                                            output = output + " --field \"" + _fields.Value[i] + "\" --value \"" + _values.Value[i] + "\" ";
                                            record.Add(new KeyValuePair<string, object>(_fields.Value[i], _values.Value[i]));
                                        }
                                        if ((record.Count > 0) && (_row.Value >= 0))
                                        {
                                            _dataStore.InsertAt(record, _row.Value);
                                        }
                                        Console.WriteLine(output);
                                    }
                                }
                            }
                            break;
                        }
                    case Command.Read:
                        {
                            string output = "READ";
                            StringBuilder builder = new StringBuilder();
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --all | --row ROW";
                                Console.WriteLine(output);
                            }
                            else if (_all.Value == true)
                            {
                                output = output + " --all";
                                Console.WriteLine(output);
                                List<List<KeyValuePair<string, object>>> records = _dataStore.Read();
                                if (records.Count > 0)
                                {
                                    for (int i = 0; i < records.Count; i++)
                                    {
                                        List<KeyValuePair<string, object>> record = records[i];
                                        for (int j = 0; j < record.Count; j++)
                                        {
                                            builder.Append("\"" + record[j].Key + "\"");
                                            builder.Append('=');
                                            TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                                            switch (typeCode)
                                            {
                                                case TypeCode.String:
                                                    {
                                                        builder.Append("\"" + record[j].Value + "\"");
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        builder.Append(record[j].Value);
                                                        break;
                                                    }
                                            }
                                            if (j < record.Count - 1)
                                            {
                                                builder.Append(',');
                                            }
                                        }
                                        if (i < records.Count - 1)
                                        {
                                            builder.Append("\r\n");
                                        }
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                            }
                            else
                            {
                                output = output + String.Format(" --row {0}", _row.Value);
                                Console.WriteLine(output);
                                List<KeyValuePair<string, object>> record = _dataStore.Read(_row.Value);
                                if (record.Count > 0)
                                {
                                    for (int j = 0; j < record.Count; j++)
                                    {
                                        builder.Append("\"" + record[j].Key + "\"");
                                        builder.Append('=');
                                        TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                                        switch (typeCode)
                                        {
                                            case TypeCode.String:
                                                {
                                                    builder.Append("\"" + record[j].Value + "\"");
                                                    break;
                                                }
                                            default:
                                                {
                                                    builder.Append(record[j].Value);
                                                    break;
                                                }
                                        }
                                        if (j < record.Count - 1)
                                        {
                                            builder.Append(',');
                                        }
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                            }
                            break;
                        }
                    case Command.Update:
                        {
                            string output = "UPDATE";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --row ROW --field \"FIELD\" --value \"VALUE\"";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                                {
                                    if (_fields.Value.Count == _values.Value.Count)
                                    {
                                        output = output + " --row " + _row.Value;
                                        List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                                        for (int i = 0; i < _fields.Value.Count; i += 1)
                                        {
                                            output = output + " --field \"" + _fields.Value[i] + "\" --value \"" + _values.Value[i] + "\" ";
                                            record.Add(new KeyValuePair<string, object>(_fields.Value[i], _values.Value[i]));
                                        }
                                        if ((record.Count > 0) && (_row.Value >= 0))
                                        {
                                            _dataStore.Update(record, _row.Value);
                                        }
                                        Console.WriteLine(output);
                                    }
                                }
                            }
                            break;
                        }
                    case Command.Delete:
                        {
                            string output = "DELETE";
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --all | --row ROW";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                if (_all.Value == true)
                                {
                                    output = output + " --all";
                                    Console.WriteLine(output);
                                    _dataStore.Delete();
                                }
                                else
                                {
                                    output = output + " --row " + _row.Value;
                                    Console.WriteLine(output);
                                    _dataStore.Delete(_row.Value);
                                }
                            }
                            break;
                        }
                    case Command.Find:
                        {
                            string output = "FIND";
                            StringBuilder builder = new StringBuilder();
                            if (_help == true)
                            {
                                output = output + " --filename FILE [--filepath PATH] --value VALUE --";
                                Console.WriteLine(output);
                            }
                            else
                            {
                                output = output + " --value " + _value.Value;
                                Console.WriteLine(output);
                                int row = _dataStore.Find(_value.Value);
                                if (row > -1)
                                {
                                    List<KeyValuePair<string, object>> record = _dataStore.Read(row);
                                    if (record.Count > 0)
                                    {
                                        for (int j = 0; j < record.Count; j++)
                                        {
                                            builder.Append("\"" + record[j].Key + "\"");
                                            builder.Append('=');
                                            TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                                            switch (typeCode)
                                            {
                                                case TypeCode.String:
                                                    {
                                                        builder.Append("\"" + record[j].Value + "\"");
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        builder.Append(record[j].Value);
                                                        break;
                                                    }
                                            }
                                            if (j < record.Count - 1)
                                            {
                                                builder.Append(',');
                                            }
                                        }
                                    }
                                    Console.WriteLine(builder.ToString());
                                }
                            }
                            break;
                        }
                }
                errorCode = 0;
            }

            Debug.WriteLine("Exit Main()");
            return (errorCode);
        }

        #endregion
        #region Private

        private static string Usage()
        {
            string usage = "Manage a datastore";
            usage += "\n";
            usage += "    --help                 -h         Show this help\n";
            usage += "    --version              -V         Show the version number\n";
            usage += "Datastore commands\n";
            usage += "    open                              Open the datastore\n";
            usage += "    --filepath PATH                   Path to the datastore\n";
            usage += "    --filename FILE                   Name of the datastore\n";
            usage += "    --reset                           When opening also reset the datastore\n";
            usage += "    close                             Close the datastore\n";
            usage += "    reset                             Reset the datastore\n";
            usage += "    new                               New datastore\n";
            usage += "Field commands\n";
            usage += "    add                               Add a field to an empty datastore\n";
            usage += "    --field FIELD                     Specify the field name\n";
            usage += "    --type TYPE                       Specify the field type\n";
            usage += "    --length LENGTH                   For text fieles Specify the field length\n";
            usage += "    remove                            Remove a field in an empty datastore\n";
            usage += "    --item INDEX                      Specify the item number\n";
            usage += "    --all                             Specify all items\n";
            usage += "    set                               Set the field in the datastore\n";
            usage += "    get                               Get the field(s) in the datastore\n";
            usage += "Record commands\n";
            usage += "    create                            Create a record in the datastore\n";
            usage += "    --field FIELD                     Specify the field name\n";
            usage += "    --value VALUE                     Specify the field value\n";
            usage += "    --data 'f=v,f1=v1'                Specify the field field,value pairs\n";
            usage += "    read                              Read a record from the datastore\n";
            usage += "    --row ROW                         Specify the row number\n";
            usage += "    --all                             Specify all rows\n";
            usage += "    update                            Update a record in the datastore\n";
            usage += "    delete                            Delete a record in the datastore\n";
            usage += "Search commands\n";
            usage += "    find                              Find a record in the datastore\n";
            usage += "    --value VALUE                     Specify the key\n";
            usage += "\n";
            usage += "\n";
            return (usage);
        }

        private static bool IsLinux
        {
            get
            {
                PlatformID platform = Environment.OSVersion.Platform;
                return ((platform == PlatformID.Unix) || (platform == PlatformID.MacOSX));
            }
        }

        private static TypeCode TypeLookup(string type)
        {
            TypeCode dataType = TypeCode.Int16;
            string lookup = type;
            if (lookup.Length > 1)
            {
                lookup = lookup.ToUpper();
            }

            switch (lookup)
            {
                case "d":
                case "D":
                case "DOUBLE":
                    dataType = TypeCode.Double;
                    break;
                case "I":
                case "INT":
                case "INT32":
                    dataType = TypeCode.Int32;
                    break;
                case "i":
                case "INT16":
                    dataType = TypeCode.Int16;
                    break;
                case "s":
                case "S":
                case "STRING":
                    dataType = TypeCode.String;
                    break;
            }
            return (dataType);
        }

        private static bool BooleanLookup(string value)
        {
            bool boolean = false;
            string lookup = value;
            if (lookup.Length > 1)
            {
                lookup = lookup.ToUpper();
            }

            switch (lookup)
            {
                case "y":
                case "Y":
                case "YES":
                case "TRUE":
                    {
                        boolean = true;
                        break;
                    }
                case "n":
                case "N":
                case "NO":
                case "FALSE":
                    {
                        boolean = false;
                        break;
                    }
            }
            return (boolean);
        }


        #endregion
    }
}
