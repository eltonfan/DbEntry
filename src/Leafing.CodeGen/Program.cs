﻿using System;
using System.IO;
using Leafing.Core;
using System.Reflection;
using Leafing.Data.Model;
using Leafing.Data;
using Leafing.Extra;
using Leafing.Extra.Logging;
using Leafing.Membership;
using Leafing.Data.Common;
using Leafing.Data.Model.Handler.Generator;

namespace Leafing.CodeGen
{
    /// <summary>
    /// 用法：exe m TableName1,TableName2,TableName3 "C:\OutputPath\"
    /// </summary>
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                Process(args);
                return 0;
            }
            catch (ArgsErrorException ex)
            {
                if (ex.ReturnCode != 0)
                {
                    Console.WriteLine(ex.Message);
                }
                ShowHelp();
                return ex.ReturnCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return 999;
        }

        private static void Process(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Code Generator For DbEntry.Net http://dbentry.codeplex.com.");
                Console.WriteLine("Modified by Elton FAN, http://elton.io/");
                return;
            }

            if(args[0].ToLower() == "m")
            {
                if(args.Length < 3)
                {
                    ShowTableList();
                }
                else
                {
                    var basePath = args[1];
                    var parts = args[2].Split(new string[] { ",", ";", "|" }, StringSplitOptions.RemoveEmptyEntries);

                    GenerateModelFromDatabase(basePath, parts);
                }
                return;
            }

            if (args.Length < 2)
            {
                throw new ArgsErrorException(0, null);
            }

            var fileName = Path.GetFullPath(args[1]);

            if (!File.Exists(fileName))
            {
                throw new ArgsErrorException(2, "The file you input doesn't exist!");
            }

			if (args.Length == 2 && args[0].ToLower() == "dll") {
				GenerateAssembly(fileName);
				Console.WriteLine("Assembly saved!");
				return;
			}

			switch (args [0].ToLower ()) {
			case "a":
			case "asp":
			case "aspnet":
				if (!SearchClasses (fileName, args.Length)) {
					GenerateAspNetTemplate (fileName, args [2]);
				}
				break;
			case "ra":
			case "action":
				if (!SearchClasses (fileName, args.Length)) {
					if (args.Length >= 4) {
						var gen = new MvcActionGenerator (fileName, args [2], args [3]);
						string s = gen.ToString ();
						Console.WriteLine (s);
					} else {
						throw new ArgsErrorException (3, "Need class name and action name.");
					}
				}
				break;
			case "rv":
			case "view":
				if (!SearchClasses (fileName, args.Length)) {
					if (args.Length >= 4) {
						string mpn = args.Length >= 5 ? args [4] : null;
						var gen = new MvcViewGenerator (fileName, args [2], args [3], mpn);
						string s = gen.ToString ();
						Console.WriteLine (s);
					} else {
						throw new ArgsErrorException (4, "Need class name and view name.");
					}
				}
				break;
			case "fn":
			case "fullname":
				var assembly = Assembly.LoadFile (args [1]);
				Console.WriteLine (assembly.FullName);
				break;
			default:
				throw new ArgsErrorException (0, null);
			}
        }

        //private static void GenerateAssembly(string fileName)
        //{
        //    ObjectInfo.GetInstance(typeof (LeafingEnum));
        //    ObjectInfo.GetInstance(typeof (LeafingLog));
        //    ObjectInfo.GetInstance(typeof (DbEntryMembershipUser));
        //    ObjectInfo.GetInstance(typeof (DbEntryRole));
        //    ObjectInfo.GetInstance(typeof (LeafingSetting));
        //    Helper.EnumTypes(fileName, true, t =>
        //    {
        //        ObjectInfo.GetInstance(t);
        //        return true;
        //    });
        //    MemoryAssembly.Instance.Save();
        //}

        private static void GenerateAspNetTemplate(string fileName, string className)
        {
            Helper.EnumTypes(fileName, t =>
            {
                if (t.FullName == className)
                {
                    var tb = new AspNetGenerator(t);
                    Console.WriteLine(tb.ToString());
                    return false;
                }
                return true;
            });
        }

        private static bool SearchClasses(string fileName, int argCount)
        {
			if (argCount == 2) {
				Helper.EnumTypes (fileName, t => {
					Console.WriteLine (t.FullName);
					return true;
				});
				return true;
			}
			return false;
        }

        private static void ShowTableList()
        {
            var g = new ModelsGenerator();
            foreach (var table in g.GetTableList())
            {
                Console.WriteLine(table);
            }
        }

        static void GenerateModelFromDatabase(string outputPath, params string[] tableNames)
        {
            var g = new ModelsGenerator();
            foreach (var tableName in tableNames)
            {
                var result = g.GenerateModelFromDatabase(tableName, outputPath);
                Console.WriteLine(result);
            }
        }

		private static void GenerateAssembly(string fileName)
		{
			ModelContext.GetInstance(typeof(LeafingEnum));
			ModelContext.GetInstance(typeof(LeafingLog));
			ModelContext.GetInstance(typeof(DbEntryMembershipUser));
			ModelContext.GetInstance(typeof(DbEntryRole));
			ModelContext.GetInstance(typeof(LeafingSetting));
			Helper.EnumTypes(fileName, true, t => {
				ModelContext.GetInstance(t);
				return true;
			});
			MemoryAssembly.Instance.Save();
		}

        private static void ShowHelp()
        {
            string s = ResourceHelper.ReadToEnd(typeof(Program), "Readme.txt");
            Console.WriteLine(s);
        }
    }
}
