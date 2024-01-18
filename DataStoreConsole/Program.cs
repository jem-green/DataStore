using System;
using DatastoreLibrary;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using TracerLibrary;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;

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
        // Index -
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
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true"
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" OPEN
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" CLOSE
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" RESET
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" NEW
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" ADD --field --type --length
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" REMOVE --item --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" SET --item --filename --type --length
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" GET --item --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" CREATE --field --value [--field --value]
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" CREATE --data "field=value,field1=value1]
        // DatastoreConsole --filename "datastore" --filepath "c:\" READ --row --all
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" UPDATE --row --field --value [--field --value]
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" UPDATE --row --data "field=value,field1=value1]
        // DatastoreConsole --filename "datastore" --filepath "c:\" --reset "true" DELETE --row --all

        #region Fields
        static private bool _isClosing = false;
        static private HandlerRoutine ctrlCHandler;
        static private PersistentDatastore _dataStore;
        static private Command _command = Command.None;

        static Parameter<bool> _all;
        static Parameter<string> _data;
        static Parameter<string> _field;
        static Parameter<List<string>> _fields;
        static Parameter<string> _filename;
        static Parameter<string> _filePath;
        static Parameter<int> _item;
        static Parameter<sbyte> _length;
        static Parameter<bool> _reset;
        static Parameter<int> _row;
        static Parameter<TypeCode> _type;
        static Parameter<object> _value;
        static Parameter<List<object>> _values;

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
            Insert = 13
        }


        #endregion
        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        #endregion
        #region Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            Debug.WriteLine("Enter Main()");

            int errorCode = 1;

            ctrlCHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(ctrlCHandler, true);

            // Required for the datastore

            _all = new Parameter<bool>(false);
            _data = new Parameter<string>("");
            _field = new Parameter<string>("");
            _fields = new Parameter<List<string>>(new List<string>());
            _filename = new Parameter<string>("");
            _filePath = new Parameter<string>("");
            _item = new Parameter<int>(0);
            _length = new Parameter<sbyte>(0);
            _reset = new Parameter<bool>(false);
            _row = new Parameter<int>(-1);
            _type = new Parameter<TypeCode>(TypeCode.Empty);
            _value = new Parameter<object>(null);
            _values = new Parameter<List<object>>(new List<object>());

            // Read in any settings provided in the XML configuration

            int pos = 0;
            Parameter<string> appPath = new Parameter<string>("");
            Parameter<string> appName = new Parameter<string>("datastore.xml");
            appPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = appPath.Value.ToString().LastIndexOf(Path.DirectorySeparatorChar);
            if (pos > 0)
            {
                appPath.Value = appPath.Value.ToString().Substring(0, pos);
                appPath.Source = Parameter<string>.SourceType.App;
            }

            Parameter<string> logPath = new Parameter<string>("");
            Parameter<string> logName = new Parameter<string>("datastoreconsole");
            logPath.Value = Environment.CurrentDirectory;
            //logPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //pos = logPath.Value.ToString().LastIndexOf(Path.DirectorySeparatorChar);
            //if (pos > 0)
            //{
            //    logPath.Value = logPath.Value.ToString().Substring(0, pos);
            //    logPath.Source = Parameter<string>.SourceType.App;
            //}

            Parameter<SourceLevels> traceLevels = new Parameter<SourceLevels>();
            traceLevels.Value = TraceInternal.TraceLookup("none");
            traceLevels.Source = Parameter<SourceLevels>.SourceType.App;

            // Configure tracer options

            string logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            FileStreamWithRolling dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            TraceFilter fileTraceFilter = new System.Diagnostics.EventTypeFilter(traceLevels.Value);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);

            //ConsoleTraceListener console = new ConsoleTraceListener();
            //TraceFilter consoleTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.None);
            //console.Filter = consoleTraceFilter;
            //Trace.Listeners.Add(console);

            _filePath.Value = Environment.CurrentDirectory;
            _filePath.Source = Parameter<string>.SourceType.App;

            if (IsLinux == false)
            {

                RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                string keys = "software\\green\\datastore\\";
                foreach (string subkey in keys.Split('\\'))
                {
                    key = key.OpenSubKey(subkey);
                    if (key == null)
                    {
                        TraceInternal.TraceVerbose("Failed to open registry key " + subkey);
                        break;
                    }
                }

                // Get the log path

                try
                {
                    if (key.GetValue("logpath", "").ToString().Length > 0)
                    {
                        logPath.Value = (string)key.GetValue("logpath", logPath);
                        logPath.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; logPath=" + logPath);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; logPath=" + logPath.Value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }

                // Get the log name

                try
                {
                    if (key.GetValue("logname", "").ToString().Length > 0)
                    {
                        logName.Value = (string)key.GetValue("logname", logName);
                        logName.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; LogName=" + logName);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; LogName=" + logName.Value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }

                // Get the name

                try
                {
                    if (key.GetValue("name", "").ToString().Length > 0)
                    {
                        appName.Value = (string)key.GetValue("name", appName);
                        appName.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; Name=" + appName);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; Name=" + appName.Value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }

                // Get the path

                try
                {
                    if (key.GetValue("path", "").ToString().Length > 0)
                    {
                        appPath.Value = (string)key.GetValue("path", appPath);
                        appPath.Source = Parameter<string>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use registry value; Path=" + appPath);
                    }
                }
                catch (NullReferenceException)
                {
                    TraceInternal.TraceVerbose("Registry error use default values; Path=" + appPath.Value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }

                // Get the traceLevels

                try
                {
                    if (key.GetValue("debug", "").ToString().Length > 0)
                    {
                        string traceName = (string)key.GetValue("debug", "Verbose");
                        traceName = traceName.TrimStart('"');
                        traceName = traceName.TrimEnd('"');
                        traceLevels.Value = TraceInternal.TraceLookup(traceName);
                        traceLevels.Source = Parameter<SourceLevels>.SourceType.Registry;
                        TraceInternal.TraceVerbose("Use command value Debug=" + traceLevels);
                    }
                }
                catch (NullReferenceException)
                {
                    Trace.TraceWarning("Registry error use default values; Debug=" + traceLevels.Value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                }
            }

            // Check if the config file has been passed in and overwrite the registry

            int elements = args.Length;
            for (int element = 0; element < elements; element++)
            {

                switch (args[element])
                {
                    case "/D":
                    case "--debug":
                        {
                            string traceName = args[++element];
                            traceName = traceName.TrimStart('"');
                            traceName = traceName.TrimEnd('"');
                            traceLevels.Value = TraceInternal.TraceLookup(traceName);
                            traceLevels.Source = Parameter<SourceLevels>.SourceType.Command;
                            TraceInternal.TraceVerbose("Use command value Debug=" + traceLevels);
                            break;
                        }
                    case "/N":
                    case "--name":
                        {
                            appName.Value = args[++element];
                            appName.Value = appName.Value.TrimStart('"');
                            appName.Value = appName.Value.TrimEnd('"');
                            appName.Source = Parameter<string>.SourceType.Command;
                            TraceInternal.TraceVerbose("Use command value Name=" + appName);
                            break;
                        }
                    case "/P":
                    case "--path":
                        {
                            appPath.Value = args[++element];
                            appPath.Value = appPath.Value.TrimStart('"');
                            appPath.Value = appPath.Value.TrimEnd('"');
                            appPath.Source = Parameter<string>.SourceType.Command;
                            TraceInternal.TraceVerbose("Use command value Path=" + appPath);
                            break;
                        }
                    case "/n":
                    case "--logname":
                        {
                            logName.Value = args[++element];
                            logName.Value = logName.Value.ToString().TrimStart('"');
                            logName.Value = logName.Value.ToString().TrimEnd('"');
                            logName.Source = Parameter<string>.SourceType.Command;
                            TraceInternal.TraceVerbose("Use command value logName=" + logName);
                            break;
                        }
                    case "/p":
                    case "--logpath":
                        {
                            logPath.Value = args[++element];
                            logPath.Value = logPath.Value.ToString().TrimStart('"');
                            logPath.Value = logPath.Value.ToString().TrimEnd('"');
                            logPath.Source = Parameter<string>.SourceType.Command;
                            TraceInternal.TraceVerbose("Use command value logPath=" + logPath);
                            break;
                        }
                }

            }
            TraceInternal.TraceInformation("Use Name=" + appName.Value + " Path=" + appPath.Value);

            // Read in configuration

            //Serialise serialise = new Serialise(appName.Value, appPath.Value);
            //_clean = serialise.FromXML();
            //if (_clean != null)
            //{
            //    // update configuration if parameters are passed

            //    if (process.Source == Parameter<string>.SourceType.None)
            //    {
            //        process.Source = Parameter<string>.SourceType.File;
            //        process.Value = _clean.Process.ToString();
            //    }

            //    if (type.Source == Parameter<string>.SourceType.None)
            //    {
            //        type.Source = Parameter<string>.SourceType.File;
            //        type.Value = _clean.Type.ToString();
            //    }
            //}


            string filenamePath = "";
            string extension = "";
            elements = args.Length;
            if (elements == 1)
            {
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
                    _filePath.Source = Parameter<string>.SourceType.Command;
                    _filename.Value = filenamePath.Substring(pos + 1, filenamePath.Length - pos - 1);
                    _filename.Source = Parameter<string>.SourceType.Command;
                }
                else
                {
                    _filename.Value = filenamePath;
                    _filename.Source = Parameter<string>.SourceType.Command;
                }
                TraceInternal.TraceVerbose("Use filename=" + _filename.Value);
                TraceInternal.TraceVerbose("use filePath=" + _filePath.Value);
            }
            else
            {
                for (int element = 0; element < elements; element++)
                {
                    {
                        string lookup = args[element];
                        if (lookup.Length > 1)
                        {
                            lookup = lookup.ToLower();
                        }
                        switch (lookup)
                        {
                            // General

                            case "open":
                                {
                                    _command = Command.Open;
                                    TraceInternal.TraceVerbose("OPEN datastore");
                                    break;
                                }
                            case "close":
                                {
                                    _command = Command.Close;
                                    TraceInternal.TraceVerbose("OPEN datastore");
                                    break;
                                }
                            case "reset":
                                {
                                    _command = Command.Reset;
                                    TraceInternal.TraceVerbose("RESET datastore");
                                    break;
                                }
                            case "new":
                                {
                                    _command = Command.New;
                                    TraceInternal.TraceVerbose("NEW datastore");
                                    break;
                                }

                            // Field

                            case "add":
                                {
                                    _command = Command.Add;
                                    TraceInternal.TraceVerbose("ADD field property to datastore");
                                    break;
                                }
                            case "remove":
                                {
                                    _command = Command.Remove;
                                    TraceInternal.TraceVerbose("REMOVE field property to datastore");
                                    break;
                                }
                            case "set":
                                {
                                    _command = Command.Set;
                                    TraceInternal.TraceVerbose("SET field property to new value in datastore");
                                    break;
                                }
                            case "get":
                                {
                                    _command = Command.Get;
                                    TraceInternal.TraceVerbose("GET field property from datastore");
                                    break;
                                }

                            // Record

                            case "create":
                                {
                                    _command = Command.Create;
                                    TraceInternal.TraceVerbose("CREATE record in datastore");
                                    break;
                                }
                            case "read":
                                {
                                    _command = Command.Read;
                                    TraceInternal.TraceVerbose("READ record from datastore");
                                    break;
                                }
                            case "update":
                                {
                                    _command = Command.Update;
                                    TraceInternal.TraceVerbose("UPDATE record in datastore");
                                    break;
                                }
                            case "delete":
                                {
                                    _command = Command.Delete;
                                    TraceInternal.TraceVerbose("DELETE record in datastore");
                                    break;
                                }
                            case "/a":
                            case "--all":
                                {
                                    string allName = args[++element];
                                    allName = allName.TrimStart('"');
                                    allName = allName.TrimEnd('"');
                                    _all.Value = BooleanLookup(allName);
                                    _all.Source = Parameter<bool>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value All=" + _all);
                                    break;
                                }
                            case "/f":
                            case "--filename":
                                {
                                    _filename.Value = args[++element];
                                    _filename.Value = _filename.Value.TrimStart('"');
                                    _filename.Value = _filename.Value.TrimEnd('"');
                                    _filename.Source = Parameter<string>.SourceType.Command;
                                    pos = _filename.Value.LastIndexOf('.');
                                    if (pos > 0)
                                    {
                                        extension = _filename.Value.Substring(pos + 1, _filename.Value.Length - pos - 1);
                                        _filename.Value = _filename.Value.Substring(0, pos);
                                    }
                                    TraceInternal.TraceVerbose("Use command value Filename=" + _filename);
                                    break;
                                }
                            case "/p":
                            case "--filepath":
                                {
                                    _filePath.Value = args[++element];
                                    _filePath.Value = _filePath.Value.TrimStart('"');
                                    _filePath.Value = _filePath.Value.TrimEnd('"');
                                    _filePath.Source = Parameter<string>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value FilePath=" + _filePath);
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
                                        TraceInternal.TraceVerbose("Use command value Field=" + fieldName);

                                        if ((_command == Command.Set) || (_command == Command.Add))
                                        {
                                            _field.Value = fieldName;
                                            _field.Source = Parameter<string>.SourceType.Command;
                                            TraceInternal.TraceVerbose("Set field");
                                        }
                                        else if ((_command == Command.Create) || (_command == Command.Update))
                                        {
                                            _fields.Value.Add(fieldName);
                                            _fields.Source = Parameter<List<string>>.SourceType.Command;
                                            TraceInternal.TraceVerbose("Add to fields");
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
                                    _item.Source = Parameter<int>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value Item=" + _item);
                                    break;
                                }
                            case "/l":
                            case "--length":
                                {
                                    string lengthName = args[++element];
                                    lengthName = lengthName.TrimStart('"');
                                    lengthName = lengthName.TrimEnd('"');
                                    _length.Value = Convert.ToSByte(lengthName);
                                    _length.Source = Parameter<sbyte>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value Length=" + _length);
                                    break;
                                }
                            case "/r":
                            case "--reset":
                                {
                                    string resetName = args[++element];
                                    resetName = resetName.TrimStart('"');
                                    resetName = resetName.TrimEnd('"');
                                    _reset.Value = BooleanLookup(resetName);
                                    _reset.Source = Parameter<bool>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value Reset=" + _reset);
                                    break;
                                }
                            case "/R":
                            case "--row":
                                {
                                    string rowName = args[++element];
                                    rowName = rowName.TrimStart('"');
                                    rowName = rowName.TrimEnd('"');
                                    _row.Value = Convert.ToInt32(rowName);
                                    _row.Source = Parameter<int>.SourceType.Command;
                                    TraceInternal.TraceVerbose("Use command value Row=" + _row);
                                    break;
                                }
                            case "/t":
                            case "--type":
                                {
                                    string typeName = args[++element];
                                    typeName = typeName.TrimStart('"');
                                    typeName = typeName.TrimEnd('"');
                                    TraceInternal.TraceVerbose("Use command value Type=" + typeName);

                                    if ((_command == Command.Set) || (_command == Command.Add))
                                    {
                                        _type.Value = TypeLookup(typeName);
                                        _type.Source = Parameter<TypeCode>.SourceType.Command;
                                        TraceInternal.TraceVerbose("Set type");
                                    }
                                    break;
                                }
                            case "/V":
                            case "--value":
                                {
                                    string valueName = args[++element];
                                    valueName = valueName.TrimStart('"');
                                    valueName = valueName.TrimEnd('"');
                                    TraceInternal.TraceVerbose("Use command value Value=" + valueName);

                                    if ((_command == Command.Set) || (_command == Command.Add))
                                    {
                                        _value.Value = valueName;
                                        _value.Source = Parameter<object>.SourceType.Command;
                                        TraceInternal.TraceVerbose("Set value");
                                    }
                                    else if ((_command == Command.Create) || (_command == Command.Update))
                                    {
                                        _values.Value.Add(valueName);
                                        TraceInternal.TraceVerbose("Add to values");
                                    }
                                    break;
                                }
                        }
                    }
                }
                TraceInternal.TraceInformation("Use Filename=" + _filename.Value + " Filepath=" + _filePath.Value);
            }

            // Redirect the output

            listener.Flush();
            Trace.Listeners.Remove(listener);
            listener.Close();
            listener.Dispose();

            logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            SourceLevels sourceLevels = TraceInternal.TraceLookup(traceLevels.Value.ToString());
            fileTraceFilter = new System.Diagnostics.EventTypeFilter(sourceLevels);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Add(listener);

            TraceInternal.TraceInformation("Use Name=" + appName.Value);
            TraceInternal.TraceInformation("Use Path=" + appPath.Value);
            TraceInternal.TraceInformation("Use Filename=" + _filename);
            TraceInternal.TraceInformation("Use FilePath=" + _filePath);
            TraceInternal.TraceInformation("Use Log Name=" + logName.Value);
            TraceInternal.TraceInformation("Use Log Path=" + logPath.Value);

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
                            string output = "OPEN";
                            if (_dataStore.IsOpen == false)
                            {
                                _dataStore.Open();
                            }
                            Console.WriteLine(output);
                            break;
                        }
                    case Command.Close:
                        {
                            string output = "CLOSE";
                            _dataStore.Close();
                            Console.WriteLine(output);
                            break;
                        }
                    case Command.Reset:
                        {
                            string output = "RESET";
                            _dataStore.Reset();
                            Console.WriteLine(output);
                            break;
                        }
                    case Command.New:
                        {
                            string output = "NEW";
                            _dataStore.New();
                            Console.WriteLine(output);
                            break;
                        }

                    // Field

                    case Command.Add:
                        {
                            Console.WriteLine("ADD --field \"{0}\" --type \"{1}\" --length {2}", _field.Value, _type.Value, _length.Value);
                            _dataStore.Add(_field.Value, _type.Value, _length.Value);
                            break;
                        }
                    case Command.Remove:
                        {
                            if (_all.Value == true)
                            {
                                Console.WriteLine("REMOVE --all");
                                _dataStore.Remove();
                            }
                            else
                            {
                                Console.WriteLine("REMOVE --item {0}", _item.Value);
                                _dataStore.Remove(_item.Value);
                            }
                            break;
                        }
                    case Command.Set:
                        {
                            Console.WriteLine("SET --item {0} --field \"{1}\" --type \"{2}\" --length {3}", ~_item.Value, _field.Value, _type.Value, _length.Value);
                            _dataStore.Set(_item.Value, _field.Value, _type.Value, _length.Value);
                            break;
                        }
                    case Command.Get:
                        {
                            StringBuilder builder = new StringBuilder();
                            if (_all.Value == true)
                            {
                                Console.WriteLine("GET --all");
                                List<PersistentDatastore.FieldType> fields = _dataStore.Get();
                                for (int j = 0; j < fields.Count; j++)
                                {
                                    builder.Append("\"" + fields[j].Name + "\"");
                                    builder.Append("[");
                                    builder.Append(fields[j].Type);
                                    if (fields[j].Type == "String")
                                    {
                                        builder.Append("=");
                                        builder.Append(fields[j].Length);
                                    }
                                    builder.Append("]");

                                    if (j < fields.Count - 1)
                                    {
                                        builder.Append(",");
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                                break;
                            }
                            else
                            {
                                Console.WriteLine("GET --item {0}", _item.Value);
                                PersistentDatastore.FieldType field = _dataStore.Get(_item.Value);

                                builder.Append("\"" + field.Name + "\"");
                                builder.Append("[");
                                builder.Append(field.Type);
                                if (field.Type == "String")
                                {
                                    builder.Append("=");
                                    builder.Append(field.Length);
                                }
                                builder.Append("]");
                                Console.WriteLine(builder.ToString());
                                break;
                            }
                        }
           

                    // Record

                    case Command.Create:
                        {
                            string output = "";
                            if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                            {
                                if (_fields.Value.Count == _values.Value.Count)
                                {
                                    output = "CREATE";
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
                            break;
                        }
                    case Command.Insert:
                        {
                            string output = "";
                            if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                            {
                                if (_fields.Value.Count == _values.Value.Count)
                                {
                                    output = "INSERT";
                                    output = output + " --row " + _row.Value;
                                    List<KeyValuePair<string, object>> record = new List<KeyValuePair<string, object>>();
                                    for (int i = 0; i < _fields.Value.Count; i += 1)
                                    {
                                        output = output + " --field \"" + _fields.Value[i] + "\" --value \"" + _values.Value[i] + "\" ";
                                        record.Add(new KeyValuePair<string, object>(_fields.Value[i], _values.Value[i]));
                                    }
                                    if ((record.Count > 0) && (_row.Value >= 0))
                                    {
                                        _dataStore.Insert(record, _row.Value);
                                    }
                                    Console.WriteLine(output);
                                }
                            }
                            break;
                        }
                    case Command.Read:
                        {
                            string output = "READ ";
                            StringBuilder builder = new StringBuilder();
                            if (_all.Value == true)
                            {
                                output = output + "-all";
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
                                            builder.Append("=");
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
                                                builder.Append(",");
                                            }
                                        }
                                        if (i < records.Count - 1)
                                        {
                                            builder.Append("\r\n");
                                        }
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                                break;
                            }
                            else
                            {
                                output = output + String.Format("--row {0}", _row.Value);
                                Console.WriteLine(output);
                                List<KeyValuePair<string, object>> record = _dataStore.Read(_row.Value);
                                if (record.Count > 0)
                                {
                                    for (int j = 0; j < record.Count; j++)
                                    {
                                        builder.Append("\"" + record[j].Key + "\"");
                                        builder.Append("=");
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
                                            builder.Append(",");
                                        }
                                    }
                                }
                                Console.WriteLine(builder.ToString());
                                break;
                            }
                            Console.WriteLine("READ --all");
                        }
                    case Command.Update:
                        {
                            string output = "";
                            if ((_fields.Value.Count > 0) && (_values.Value.Count > 0))
                            {
                                if (_fields.Value.Count == _values.Value.Count)
                                {
                                    output = "UPDATE";
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
                            else
                            {

                            }
                            break;
                        }
                    case Command.Delete:
                        {
                            if (_all.Value == true)
                            {
                                _dataStore.Delete();
                            }
                            else
                            {
                                _dataStore.Delete(_row.Value);
                            }
                            break;
                        }
                }
                errorCode = 0;
            }

            // Redirect the output

            listener.Flush();
            listener.Close();
            listener.Dispose();

            Debug.WriteLine("Exit Main()");
            return (errorCode);
        }

        #endregion
        #region Private

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Debug.WriteLine("In ConsoleCtrlCheck()");

            if (_isClosing == false)
            {
                switch (ctrlType)
                {
                    case CtrlTypes.CTRL_C_EVENT:
                        {
                            _isClosing = true;
                            TraceInternal.TraceVerbose("CTRL+C received:");
                            break;
                        }
                    case CtrlTypes.CTRL_BREAK_EVENT:
                        {
                            _isClosing = true;
                            TraceInternal.TraceVerbose("CTRL+BREAK received:");
                            break;
                        }
                    case CtrlTypes.CTRL_CLOSE_EVENT:
                        {
                            _isClosing = true;
                            TraceInternal.TraceVerbose("Program being closed:");
                            break;
                        }
                    case CtrlTypes.CTRL_LOGOFF_EVENT:
                    case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                        {
                            _isClosing = true;
                            TraceInternal.TraceVerbose("User is logging off:");
                            break;
                        }
                }
            }
            Debug.WriteLine("Out ConsoleCtrlCheck()");
            Environment.Exit(0);
            return (true);

        }

        private static bool IsLinux
        {
            get
            {
                PlatformID platfrom = Environment.OSVersion.Platform;
                return ((platfrom == PlatformID.Unix) || (platfrom == PlatformID.MacOSX));
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
