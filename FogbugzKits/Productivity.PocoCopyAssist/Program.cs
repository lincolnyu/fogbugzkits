using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PocoCopyAssist
{
    class Program
    {
        static bool Convert(string input, out string copyFrom, out string copyTo,
            out string poco, out string table)
        {
            var regex = new Regex("([a-zA-z]+)[ \t]+([a-zA-z_][a-zA-z1-9_]*)");
            var match = regex.Match(input);
            string varType = null;
            string varName = null;
            if (match.Success && match.Groups.Count > 2)
            {
                varType = match.Groups[1].Value;
                varName = match.Groups[2].Value;
            }
            copyFrom = null;
            copyTo = null;
            poco = null;
            table = null;
            if (varName == null)
            {
                return false;
            }

            var copyFromPrefix = "";
            var copyFromSuffix = "";
            var copyToPrefix = "";
            var copyToSuffix = "";
            var pocoType = varType;
            var dbType = $"<{pocoType}>";
            switch (varType)
            {
                case "int":
                    dbType = "INTEGER NOT NULL";
                    break;
                case "long":
                    dbType = "BIGINT NOT NULL";
                    break;
                case "double":
                    dbType = "DOUBLE NOT NULL";
                    break;
                case "bool":
                    dbType = "BOOLEAN NOT NULL";
                    break;
                case "string":
                    dbType = "TEXT";
                    break;
                // TODO other types
                case "DateTime":
                case "TimeSpan":
                    copyFromPrefix = $"new {varType}(";
                    copyFromSuffix = ")";
                    copyToSuffix = ".Ticks";
                    pocoType = "long";
                    dbType = "BIGINT NOT NULL";
                    break;
            }
           
            copyFrom = $"{varName} = {copyFromPrefix}thatPoco.{varName}{copyFromSuffix};";
            copyTo = $"thatPoco.{varName} = {copyToPrefix}{varName}{copyToSuffix};";
            table = $"'{varName}' {dbType},";
            poco = $"public {pocoType} {varName} {{get; set;}}";
            return true;
        }

        static void Convert(TextReader input, TextWriter output)
        {
            var copyToBuffered = new List<string>();
            var pocosBuffered = new List<string>();
            var tablesBuffered = new List<string>();
            while (true)
            {
                var inputLine = input.ReadLine();
                if (inputLine == null)
                {
                    break;
                }
                if (string.IsNullOrWhiteSpace(inputLine))
                {
                    continue;
                }
                string copyFrom, copyTo, poco, table;
                var succ = Convert(inputLine, out copyFrom, out copyTo, out poco, out table);
                if (succ)
                {
                    output.WriteLine(copyFrom);
                    copyToBuffered.Add(copyTo);
                    pocosBuffered.Add(poco);
                    tablesBuffered.Add(table);
                }
                else
                {
                    var msg = $"error with '{inputLine}'";
                    output.WriteLine(msg);
                    copyToBuffered.Add(msg);
                    pocosBuffered.Add(msg);
                    tablesBuffered.Add(msg);
                }
            }

            output.WriteLine();
            foreach (var copyTo in copyToBuffered)
            {
                output.WriteLine(copyTo);
            }

            output.WriteLine();
            foreach (var poco in pocosBuffered)
            {
                output.WriteLine(poco);
            }

            output.WriteLine();
            foreach (var table in tablesBuffered)
            {
                output.WriteLine(table);
            }
        }

        static void Main()
        {
            Convert(Console.In, Console.Out);
        }
    }
}
