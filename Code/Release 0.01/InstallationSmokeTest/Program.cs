﻿/**
* Smoke Tester Tool : Post deployment smoke testing tool.
* 
* http://www.stephenhaunts.com
* 
* This file is part of Smoke Tester Tool.
* 
* Smoke Tester Tool is free software: you can redistribute it and/or modify it under the terms of the
* GNU General Public License as published by the Free Software Foundation, either version 2 of the
* License, or (at your option) any later version.
* 
* Smoke Tester Tool is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* 
* See the GNU General Public License for more details <http://www.gnu.org/licenses/>.
* 
* Curator: Stephen Haunts
*/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Xml;
using ConfigurationTests;
using ConfigurationTests.Tests;

namespace InstallationSmokeTest
{
    internal class Program
    {
        private const string RunOperation = "Run";
        private const string CreateOperation = "Create";
        private const string AbortOperation = "Abort";
        private const string UnexpectedResponseMessage = "Unexpected response.";
        private const string TestPassedMessage = "OK!";
        private const string OverwritePrompt = "Overwrite {0}? [y/N] ";
        private const ConsoleKey OverwriteAffirmativeKey = ConsoleKey.Y;
        private const string SmokeTestFileExtension = ".xml";
        private const string StandardDatetimeFormat = "dd/MM/yyyy HH:mm:ss";
        private const string StandardNumberFormat = "#,##0";

        private static string _outputFile;

        internal static bool SmokeTestsPassed { get; private set; }

        internal static void Main(string[] args)
        {
            SmokeTestsPassed = false;
            bool runWithUi = true;

            try
            {
                string operation = RunOperation;

                if (args.Length > 0)
                {
                    runWithUi = false;
                    operation = args[0];
                }

                string file = args.Length > 1 ? args[1] : SelectFile(operation == RunOperation);

                _outputFile = args.Length > 2 ? args[2] : null;

                WriteLine();
                WriteLine("Post-Deployment Smoke Test Tool");

                if (file == null)
                {
                    return;
                }

                if (Path.GetExtension(file).ToLower() != SmokeTestFileExtension)
                {
                    file += SmokeTestFileExtension;
                }

                switch (operation)
                {
                    case CreateOperation:
                        CreateConfiguration(file);
                        break;
                    case RunOperation:
                        CheckConfiguration(file);
                        break;
                    default:
                        DisplayUsageHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Environment.ExitCode = int.MaxValue;

                if (Environment.UserInteractive)
                {
                    WriteLine("Message:" + ex.Message);
                    WriteLine("StackTrace: " + ex.StackTrace);

                    if (runWithUi && _outputFile != null)
                    {
                        Console.Write("Press any key to end . . .");
                        Console.ReadKey(true);
                    }
                }

                else throw;
            }
        }

        private static void DisplayUsageHelp()
        {
            string exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("{0} {1} <filename> <outputfilename>", exeName, RunOperation);
            Console.WriteLine("\tRun the tests contained in the given filename.");
            Console.WriteLine();
            Console.WriteLine("{0} {1} <filename> <outputfilename>", exeName, CreateOperation);
            Console.WriteLine("\tCreate a new XML file with examples of the usage.");
            Console.WriteLine();
            Console.WriteLine("\tThe default mode is Run.");
            Console.WriteLine("\tIf the filename does not end with .xml then .xml will be appended.");
            Console.WriteLine("\tIf the filename is omitted, you will be prompted for it.");
        }

        private static void CreateConfiguration(string file)
        {
            ConsoleColor temp = Console.ForegroundColor;

            try
            {
                if (File.Exists(file))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    WriteLine(OverwritePrompt, file);
                    Console.ForegroundColor = ConsoleColor.White;
                    ConsoleKeyInfo overwrite = Console.ReadKey(true);

                    if (overwrite.Key != OverwriteAffirmativeKey)
                    {
                        WriteLine("Not overwriting.");
                        return;
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                WriteLine("Preparing example data...");
                var configurationInformation = new ConfigurationTestSuite();
                configurationInformation.CreateExampleData();
                string xmlString = configurationInformation.ToXmlString();
                Console.Write("Writing file...");
                File.WriteAllText(Path.Combine(".", file), xmlString, Encoding.Unicode);
                WriteLine(" Done.");
            }
            finally
            {
                Console.ForegroundColor = temp;
            }
        }

        private static void CheckConfiguration(string file)
        {
            if (!File.Exists(file))
            {
                WriteLine("Could not find file {0}", file);
                Environment.ExitCode = int.MaxValue;
                return;
            }

            ConfigurationTestSuite info;

            try
            {
                string xml = File.ReadAllText(file, Encoding.Unicode);
                info = xml.ToObject<ConfigurationTestSuite>();
            }
            catch (InvalidOperationException ex)
            {
                DisplayError("Unable to read file, check that the file is in Unicode.");
                DisplayError(ex.ToString());
                return;
            }

            if (info == null)
            {
                DisplayError(String.Format("Could not convert {0} to ConfigurationTestSuite object.", file));
                return;
            }

            WriteLine("Running Tests: " + DateTime.Now.ToString(StandardDatetimeFormat));
            WriteLine();

            int successfulTests = info.Tests.Select(RunTest).Count(result => result);

            WriteLine();
            WriteLine("Completed Tests: " + DateTime.Now.ToString(StandardDatetimeFormat));
            int totalTests = info.Tests.Count();

            string totalTestsString = totalTests.ToString(StandardNumberFormat);
            int totalWidth = totalTestsString.Length;
            int failedTests = totalTests - successfulTests;

            WriteLine("Tests Run:    {0}", totalTestsString);
            WriteLine("Tests Passed: {0}", successfulTests.ToString(StandardNumberFormat).PadLeft(totalWidth));
            WriteLine("Tests Failed: {0}", failedTests.ToString(StandardNumberFormat).PadLeft(totalWidth));

            if (failedTests > 0)
            {
                DisplayError("SMOKE TEST FAILED!");
            }
            else
            {
                SmokeTestsPassed = true;
                DisplaySuccess("Smoke test passed successfully");
            }

            Environment.ExitCode = failedTests;

            if (Environment.UserInteractive && _outputFile == null)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey(true);
            }
        }

        private static void WriteLine(string text = "", params object[] parameters)
        {
            if (_outputFile == null)
            {
                Console.WriteLine(text, parameters);
            }
            else
            {
                File.AppendAllLines(_outputFile, new[] {string.Format(text, parameters)});
            }
        }

        private static bool RunTest(Test test)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            WriteLine("{0}, {1}, {2}", test.GetType().Name, test.TestName, DateTime.Now.ToString(StandardDatetimeFormat));

            try
            {
                test.Run();
                Console.ForegroundColor = ConsoleColor.Green;
                WriteLine("\t\t{0}", TestPassedMessage);

                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                WriteLine("\tMessage: {0}", ex.Message);
                WriteLine("\tStackTrace: {0}", ex.StackTrace);

                return false;
            }
            finally
            {
                Console.ForegroundColor = temp;
            }
        }

        private static string SelectFile(bool mustExist)
        {
            string[] files = Directory.GetFiles(".", "*" + SmokeTestFileExtension);
            string[] suites =
                files.Select(f => Path.GetFileName(f).Replace(SmokeTestFileExtension, ""))
                    .Where(f => f.ToUpper() != AbortOperation.ToUpper())
                    .ToArray();
            ChooseFile:

            PresentSelectionOptions(suites, AbortOperation);
            string input = GetInput().Trim();

            if (input == "?")
            {
                DisplayUsageHelp();
                goto ChooseFile;
            }

            if (string.IsNullOrWhiteSpace(input) ||
                input.Equals(AbortOperation, StringComparison.InvariantCultureIgnoreCase) || input == "0")
            {
                return null;
            }

            string file = null;

            if (suites.Any(e => e == input))
            {
                file = Path.Combine(".", string.Format("{0}{1}", input, SmokeTestFileExtension));
            }
            else
            {
                int fileIndex;

                if (int.TryParse(input, out fileIndex) && fileIndex <= files.Length)
                {
                    if (fileIndex > 0)
                    {
                        file = files[fileIndex - 1];
                    }
                }
                else
                {
                    if (mustExist)
                    {
                        DisplayError(UnexpectedResponseMessage);
                        goto ChooseFile;
                    }

                    file = input;
                }
            }

            return file;
        }

        private static void PresentSelectionOptions(string[] suites, string abort)
        {
            ConsoleColor temp = Console.ForegroundColor;

            Console.WriteLine();
            Console.WriteLine("Choose a suite or type its name, or type 0 to {0} or ? for CLI help:", abort);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("0: {0}", abort);

            for (int i = 0; i < suites.Length; i++)
            {
                Console.WriteLine("{0}: {1}", i + 1, suites[i]);
            }

            Console.ForegroundColor = temp;
        }

        private static void DisplayError(string errorMessage)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(errorMessage);

            Console.ForegroundColor = temp;
        }

        private static void DisplaySuccess(string message)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine(message);

            Console.ForegroundColor = temp;
        }

        private static string GetInput()
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.White;
            string input = Console.ReadLine();
            Console.ForegroundColor = temp;

            return input;
        }
    }
}