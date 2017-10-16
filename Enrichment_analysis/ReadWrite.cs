/*
Created by Jens Hansen on 10/1/17.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Global_parameter;
using Report;
  

namespace ReadWrite
{
    public enum ReadWrite_report_enum { E_m_p_t_y = 0, Report_main = 1, Report_everything = 2, Report_nothing }

    public abstract class ReadWriteOptions_base
    {
        public string File { get; set; }
        public string[] Key_columnNames { get; set; }
        public string[] Key_propertyNames { get; set; }
        public int[] Key_columnIndexes { get; set; }
        public string[] SafeCondition_columnNames { get; set; }
        public int[] SafeCondition_columnIndexes { get; set; }
        public string[] SafeCondition_entries { get; set; }
        public bool File_has_headline { get; set; }
        public string[] RemoveFromHeadline { get; set; }
        public char[] LineDelimiters { get; set; }
        public char[] HeadlineDelimiters { get; set; }
        public int Skip_lines { get; set; }
        public int Empty_integer_value { get; set; }
        public string Empty_string_value { get; set; }
        public string[] Invalid_line_defining_columnNames { get; set; }
        public ReadWrite_report_enum Report { get; set; }
        public bool Report_unhandled_null_entries { get; set; }

        public ReadWriteOptions_base()
        {
            Report_unhandled_null_entries = true;
            Invalid_line_defining_columnNames = new string[0];
            Empty_string_value = Global_class.Empty_entry;
        }
    }

    class ReadWriteClass
    {
        #region Column and property indexes
        private static string[] Get_and_modify_columnNames(string headline, ReadWriteOptions_base Options)
        {
            List<string> columnNamesList = new List<string>();
            columnNamesList.AddRange(headline.Split(Options.HeadlineDelimiters));
            if (Options.RemoveFromHeadline != null)
            {
                int removeFromHeadline_length = Options.RemoveFromHeadline.Length;
                for (int i = 0; i < removeFromHeadline_length; i++)
                {
                    columnNamesList.Remove(Options.RemoveFromHeadline[i]);
                }
            }

            string[] columnNames = columnNamesList.ToArray();
            return columnNames;
        }

        private static int[] Get_columnIndexes_of_given_columnNames<T>(string[] columnNames, params string[] given_columnNames)
        {
            int given_length = given_columnNames.Length;
            if (given_length == 0)
            {
                Report_class.Write_error_line("{0}: no columnNames to search for", typeof(T).Name);
            }
            int[] columnIndexes = new int[given_length];
            for (int i = 0; i < given_length; i++)
            {
                int index = Array.IndexOf(columnNames, given_columnNames[i]);
                if (index >= 0) { columnIndexes[i] = index; }
                else
                {
                    Report_class.Write_error_line("{0}: columnName \"{1}\" does not exist", typeof(T).Name, given_columnNames[i]);
                    throw new Exception();
                }
            }
            return columnIndexes;
        }

        private static int[] Get_propertyIndexes_of_corresponding_given_columnNames<T>(PropertyInfo[] propInfo, string[] propertyNames, string[] given_columnNames, string[] search_given_columnNames)
        {
            int search_length = search_given_columnNames.Length;
            int[] columnNames_indexes = new int[search_length];
            if (search_length == 0)
            {
                Report_class.Write_error_line("{0}: no search columnNames to search for", typeof(T).Name);
            }
            for (int i = 0; i < search_length; i++)
            {
                int index = Array.IndexOf(given_columnNames, search_given_columnNames[i]);
                if (index >= 0) { columnNames_indexes[i] = index; }
                else
                {
                    Report_class.Write_error_line("{0}: given_columnName \"{1}\" does not exist", typeof(T).Name, given_columnNames[i]);
                }
            }
            string[] corresponding_propertyNames = new string[search_length];
            for (int indexS = 0; indexS < search_length; indexS++)
            {
                corresponding_propertyNames[indexS] = propertyNames[columnNames_indexes[indexS]];
            }
            int[] propertyIndexes = Get_propertyIndexes<T>(propInfo, corresponding_propertyNames);
            return propertyIndexes;
        }

        private static int[] Get_propertyIndexes<T>(PropertyInfo[] propInfo, string[] key_propertyNames)
        {
            int key_length = key_propertyNames.Length;
            int[] propertyIndexes = new int[key_length];
            string[] propInfo_names = new string[propInfo.Length];

            for (int i = 0; i < propInfo.Length; i++)
            {
                propInfo_names[i] = propInfo[i].Name;
            }

            for (int i = 0; i < key_length; i++)
            {
                int index = Array.IndexOf(propInfo_names, key_propertyNames[i]);
                if (index >= 0) { propertyIndexes[i] = index; }
                if (index < 0)
                {
                    Report_class.Write_error_line("{0}: propertyName \"{1}\" does not exist", typeof(T).Name, key_propertyNames[i]);
                }
            }
            return propertyIndexes;
        }
        #endregion

        #region Create/Delete directories
        public static void Create_subdirectory_if_it_does_not_exist(string actDirectory, string sub_directory_name)
        {
            if (!Directory.Exists(actDirectory + sub_directory_name))
            {
                Report_class.WriteLine("{0}: Create subdirectory {1} in directory {2}", typeof(ReadWriteClass).Name, sub_directory_name, actDirectory);
                DirectoryInfo dir = new DirectoryInfo(actDirectory);
                dir.CreateSubdirectory(sub_directory_name);
            }
        }

        public static void Create_directory_if_it_does_not_exist(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void Create_subdirectory_in_results_directory_if_it_does_not_exist(string sub_directory_name)
        {
            Create_subdirectory_if_it_does_not_exist(Global_directory_class.Results_directory, sub_directory_name);
        }

        public static void Delete_directory_if_it_exists_without_asking(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        public static void Delete_directory_if_it_exists(string directory)
        {
            if (Directory.Exists(directory))
            {
                Report_class.Write("Delete {0}?", directory);
                Console.Read();
                Directory.Delete(directory, true);
            }
        }
        #endregion

        #region Read data
        public static List<T> ReadRawData_and_FillList<T>(StreamReader stream, ReadWriteOptions_base options, string file_name) where T : class
        {
            if (options.Report >= ReadWrite_report_enum.Report_main)
            {
                Report_class.WriteLine("{0}:\nRead file: {1}", typeof(T).Name, file_name);
            }
            Stopwatch timer = new Stopwatch();
            timer.Start();

            PropertyInfo[] propInfo = typeof(T).GetProperties();
            FileInfo file = new FileInfo(options.File);

            #region Determine columns to be safed and invalidLine_defining columns and properties
            //Read headline, if it exists, determine indexes of columns to be safed in list
            //Begin
            string[] columnNames = { Global_class.Empty_entry };
            int[] columnIndexes;
            int[] invalidLine_defining_columnIndexes = new int[0];
            int[] invalidLine_defining_popertyIndexes = new int[0];
            int[] propertyIndexes;

            if (options.File_has_headline)
            {
                string headline = stream.ReadLine();
                columnNames = Get_and_modify_columnNames(headline, options);
                columnIndexes = Get_columnIndexes_of_given_columnNames<T>(columnNames, options.Key_columnNames);
                if (options.Invalid_line_defining_columnNames.Length > 0)
                {
                    invalidLine_defining_columnIndexes = Get_columnIndexes_of_given_columnNames<T>(columnNames, options.Invalid_line_defining_columnNames);
                    invalidLine_defining_popertyIndexes = Get_propertyIndexes_of_corresponding_given_columnNames<T>(propInfo, options.Key_propertyNames, options.Key_columnNames, options.Invalid_line_defining_columnNames);
                }
            }
            else { columnIndexes = options.Key_columnIndexes; }
            propertyIndexes = Get_propertyIndexes<T>(propInfo, options.Key_propertyNames);
            if (columnIndexes.Length != propertyIndexes.Length)
            {
                Report_class.Write_error_line("{0}: Length columnIndexes (Key_columnNames/columnIndexes) != propertyIndexes (Key_propertyNames)", typeof(T).Name);
            }
            //End
            #endregion

            #region Skip lines
            //Skip lines
            for (int indexSkip = 0; indexSkip < options.Skip_lines; indexSkip++)
            {
                stream.ReadLine();
            }
            #endregion

            #region Determine indexes of columns which contain a safecondition, if safeconditions exist
            bool safeConditions_exist = options.SafeCondition_entries != null;
            int[] safeConditions_columnIndexes = new int[0];
            string[] safeConditions_entries = options.SafeCondition_entries;
            int safeConditions_length = -1;

            if (safeConditions_exist == true)
            {
                safeConditions_length = options.SafeCondition_entries.Length;
                if (options.File_has_headline)
                {
                    safeConditions_columnIndexes = Get_columnIndexes_of_given_columnNames<T>(columnNames, options.SafeCondition_columnNames);
                }
                else
                {
                    safeConditions_columnIndexes = options.SafeCondition_columnIndexes;
                }
                if (safeConditions_columnIndexes.Length != safeConditions_entries.Length) { Report_class.WriteLine("{0}: length safeConditions_columnIndexes (_columnNames/columnIndexes) != length safeConditions_columnEntries", typeof(T).Name); }
            }
            #endregion

            #region Generate and fill list
            List<T> Data = new List<T>();
            var TType = typeof(T);

            int invalidLine_defining_columnIndexes_length = invalidLine_defining_columnIndexes.Length;
            string inputLine;
            int readLines = 0;
            int safedLines = 0;
            int colIndex;
            int propIndex;
            bool safeLine;
            bool report_check_lineDelimiters = false;
            bool valid;
            string invalidLineDefiningColumnEntry;
            int line_count = 0;

            while ((inputLine = stream.ReadLine()) != null)
            {
                if ((inputLine.Length > 0) && (!inputLine.Substring(0, 5).Equals("-----")))
                {
                    line_count++;
                    //line_count++;
                    string[] columnEntries = inputLine.Split(options.LineDelimiters);
                    if (columnEntries.Length == 1)
                    {
                        report_check_lineDelimiters = true;
                    }
                    safeLine = true;
                    if (safeConditions_exist)
                    {
                        for (int indexSC = 0; indexSC < safeConditions_length; indexSC++)
                        {
                            columnEntries[safeConditions_columnIndexes[indexSC]] = char.ToUpper(columnEntries[safeConditions_columnIndexes[indexSC]][0]) + columnEntries[safeConditions_columnIndexes[indexSC]].ToLower().Substring(1);  ///transient solution!!!!!!
                            if (safeConditions_entries[indexSC] != columnEntries[safeConditions_columnIndexes[indexSC]])
                            {
                                safeLine = false;
                            }
                        }
                    }
                    valid = true;
                    for (int indexIndex = 0; indexIndex < invalidLine_defining_columnIndexes_length; indexIndex++)
                    {
                        invalidLineDefiningColumnEntry = columnEntries[invalidLine_defining_columnIndexes[indexIndex]];
                        try
                        {
                            var obj = Convert.ChangeType(invalidLineDefiningColumnEntry, propInfo[invalidLine_defining_popertyIndexes[indexIndex]].PropertyType);
                            valid = true;
                        }
                        catch (InvalidCastException)
                        {
                            valid = false;
                        }
                        catch (FormatException)
                        {
                            valid = false;
                        }
                        catch (OverflowException)
                        {
                            valid = false;
                        }
                        catch (ArgumentNullException)
                        {
                            valid = false;
                        }
                    }
                    if ((safeLine) && (valid))
                    {
                        T newLine = (T)Activator.CreateInstance(TType);
                        for (int i = 0; i < columnIndexes.Length; i++)
                        {
                            colIndex = columnIndexes[i];
                            propIndex = propertyIndexes[i];
                            if (columnEntries[colIndex] == "#DIV/0!") { columnEntries[colIndex] = "NaN"; }
                            if (propInfo[propIndex].PropertyType.IsEnum)
                            {
                                columnEntries[colIndex] = char.ToUpper(columnEntries[colIndex][0]) + columnEntries[colIndex].ToLower().Substring(1);
                                propInfo[propIndex].SetValue(newLine, Enum.Parse(propInfo[propIndex].PropertyType, columnEntries[colIndex]), null);
                            }
                            else if (string.IsNullOrEmpty(columnEntries[colIndex]))
                            {
                                if (propInfo[propIndex].PropertyType == typeof(int))
                                {
                                    propInfo[propIndex].SetValue(newLine, options.Empty_integer_value, null);
                                }
                                else if (propInfo[propIndex].PropertyType == typeof(string))
                                {
                                    propInfo[propIndex].SetValue(newLine, "", null);
                                }
                                else if (options.Report_unhandled_null_entries)
                                {
                                    Report_class.Write_error_line("{0}: ReadRawData_and_FillList: {1} unhandled null entry", typeof(ReadWriteClass).Name, options.File);
                                }
                            }
                            else
                            {
                                if ((columnEntries[colIndex] != "") && ((columnEntries[colIndex] != "NA") || (propInfo[propIndex].PropertyType == typeof(string))))
                                {
                                    propInfo[propIndex].SetValue(newLine, Convert.ChangeType(columnEntries[colIndex], propInfo[propIndex].PropertyType), null);
                                }
                            }
                        }
                        Data.Add(newLine);
                        safedLines = safedLines + 1;
                    }
                    readLines = readLines + 1;
                    if ((options.Report == ReadWrite_report_enum.Report_everything) && (readLines % 2000000 == 0)) { Report_class.WriteLine("{0}: Read lines: {1} Mio, \tSafed lines: {2} Mio", typeof(T).Name, (double)readLines / 1000000, (double)safedLines / 1000000); }
                }
            }
            #endregion

            #region Final report
            if (report_check_lineDelimiters)
            {
                Report_class.Write_error_line("{0}: only one column entry: Check lineDelimiters", typeof(ReadWriteClass).Name);
            }
            timer.Stop();
            if (options.Report == ReadWrite_report_enum.Report_everything)
            {
                Report_class.WriteLine("{0}: Read lines: {1} Mio, Safed lines: {2} Mio", typeof(T).Name, (double)readLines / 1000000, (double)safedLines / 1000000);
                Report_class.WriteLine("{0}: Time: {1}", typeof(T).Name, timer.Elapsed);
            }
            if (options.Report >= ReadWrite_report_enum.Report_main)
            {
                Report_class.WriteLine();
            }
            stream.Close();
            if (Data.Count == 0)
            {
                Report_class.Write_error_line("{0}: ReadFile {1} data.count == 0, no lines filled!", typeof(ReadWriteClass).Name, options.File);
            }
            #endregion

            return Data;
        }

        public static List<T> ReadRawData_and_FillList<T>(ReadWriteOptions_base options) where T : class
        {
            FileInfo file = new FileInfo(options.File);
            StreamReader stream = file.OpenText();
            List<T> Data = ReadRawData_and_FillList<T>(stream, options, options.File);
            return Data;
        }

        public static T[] ReadRawData_and_FillArray<T>(ReadWriteOptions_base Options) where T : class
        {
            return ReadRawData_and_FillList<T>(Options).ToArray();
        }
        #endregion

        #region Files
        public static bool File_exists(string file_name)
        {
            return File.Exists(file_name);
        }

        public static void Delete_file_if_it_exists(string complete_file_name)
        {
            Report_class.Write("Delete file {0}:", complete_file_name);
            if (File.Exists(complete_file_name))
            {
                FileInfo delete_file = new FileInfo(complete_file_name);
                delete_file.Delete();
                Report_class.WriteLine(" deleted");
            }
            else
            {
                Report_class.WriteLine(" File does not exist");
            }
        }
        #endregion

        #region Write data
        public static void WriteData<T>(List<T> Data, ReadWriteOptions_base Options) where T : class
        {
            Report_class.WriteLine("{0}: Write file {1}", typeof(T).Name, Options.File);
            StreamWriter writer = new StreamWriter(Options.File, false);
            WriteData(Data, Options, writer);
            writer.Close();
        }

        public static void WriteData<T>(T[] Data, ReadWriteOptions_base Options) where T : class
        {
            Report_class.WriteLine("{0}: Write file {1}", typeof(T).Name, Options.File);
            StreamWriter writer = new StreamWriter(Options.File, false);
            WriteData(Data, Options, writer);
            writer.Close();
        }

        public static void WriteData<T>(T[] Data, ReadWriteOptions_base Options, StreamWriter writer) where T : class
        {
            WriteData(Data.ToList(), Options, writer);
        }

        public static void WriteData<T>(List<T> Data, ReadWriteOptions_base Options, StreamWriter writer) where T : class
        {
            PropertyInfo[] propInfo = typeof(T).GetProperties();
            PropertyInfo prop;

            int[] propertyIndexes = Get_propertyIndexes<T>(propInfo, Options.Key_propertyNames);

            //Generate and write Headline
            int propertyIndexes_length = propertyIndexes.Length;
            if (Options.File_has_headline == true)
            {
                char headline_delimiter = Options.HeadlineDelimiters[0];
                StringBuilder headline = new StringBuilder();
                for (int index = 0; index < propertyIndexes_length; index++)
                {
                    if (index < propertyIndexes_length - 1)
                    {
                        headline.AppendFormat("{0}{1}", Options.Key_columnNames[index], headline_delimiter);
                    }
                    else
                    {
                        headline.AppendFormat("{0}", Options.Key_columnNames[index]);
                    }
                }
                writer.WriteLine(headline);
            }

            //Generate and write lines
            char line_delimiter = Options.LineDelimiters[0];
            StringBuilder line = new StringBuilder();
            int data_count = Data.Count;
            for (int lineIndex = 0; lineIndex < data_count; lineIndex++)
            {
                line.Clear();
                for (int index = 0; index < propertyIndexes_length; index++)
                {
                    prop = propInfo[propertyIndexes[index]];
                    if (index < propertyIndexes_length - 1) { line.AppendFormat("{0}{1}", prop.GetValue(Data[lineIndex], null), line_delimiter); }
                    else { line.AppendFormat("{0}", prop.GetValue(Data[lineIndex], null)); }
                }
                writer.WriteLine(line);
            }
            writer.Close();
            Report_class.WriteLine();
        }
        #endregion

        #region Arrays <-> readLines
        public static T[] Get_array_from_readLine<T>(string readLine, char delimiter)
        {
            string[] split = readLine.Split(delimiter);
            int split_length = split.Length;
            if (string.IsNullOrEmpty(split[split_length - 1])) { split_length--; }
            var TType = typeof(T);
            T[] array = new T[split_length];
            for (int i = 0; i < split_length; i++)
            {
                array[i] = (T)Convert.ChangeType(split[i], TType);
            }
            return array;
        }

        public static string Get_writeLine_from_array<T>(T[] array, char delimiter)
        {
            StringBuilder stringBuild = new StringBuilder();
            int array_length = array.Length;
            for (int i = 0; i < array_length; i++)
            {
                if (i == 0) { stringBuild.AppendFormat("{0}", array[i]); }
                else { stringBuild.AppendFormat("{0}{1}", delimiter, array[i]); }
            }
            return stringBuild.ToString();
        }
        #endregion
     }

    /////////////////////////////////////////////////////////


}

