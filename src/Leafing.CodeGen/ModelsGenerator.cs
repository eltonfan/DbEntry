using System;
using System.Collections.Generic;
using System.Text;
using Leafing.Data;
using Leafing.Data.SqlEntry;
using System.IO;

namespace Leafing.CodeGen
{
    public class ModelsGenerator
    {
        public class ModelBuilder
        {
            private readonly Dictionary<Type, string> _types;
            protected string TableName;
            protected List<DbColumnInfo> InfoList;
            protected StringBuilder Result;

            public ModelBuilder(string tableName, List<DbColumnInfo> list)
            {
                TableName = tableName;
                InfoList = list;

                Result = new StringBuilder();

                _types = new Dictionary<Type, string> {
                    { typeof (bool), "bool" },
                    { typeof (byte), "byte" },
                    { typeof (short), "short" },
                    { typeof (int), "int" },
                    { typeof (long), "long" },
                    { typeof (float), "float" },
                    { typeof (double), "double" },
                    { typeof (DateTime), "DateTime" },
                    { typeof (TimeSpan), "Time" },
                    { typeof (byte[]), "byte[]" },
                    { typeof (string), "string" },
                };
            }

            protected string GetTypeName(Type t)
            {
                if (_types.ContainsKey(t))
                {
                    return _types[t];
                }
                return t.ToString();
            }

            protected string GetNullableTypeName(DbColumnInfo info)
            {
                string s = GetTypeName(info.DataType);
                if (info.AllowDBNull && info.DataType.IsValueType)
                {
                    s += "?";
                }
                return s;
            }

            public virtual string Build()
            {
                Result.Append(
@"using Leafing.Data.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GuLinOA.Models
{
    public partial class ").Append(TableName);
                AppendBaseType(TableName);
                foreach (var info in InfoList)
                {
                    if (info.IsKey)
                    {
                        BuildKeyColomn(info);
                        ProcessKeyColumn(info);
                    }
                    else
                    {
                        BuildColumn(info);
                        ProcessColumn(info);
                    }
                }

                Result.Append(
@"        public ").Append(TableName).Append(@"()
        {
");
                foreach (var info in InfoList)
                {
                    if (info.ColumnName.ToLower() == "id")
                    {
                    }
                    else
                    {
                        var defaultValue = "";
                        if (info.DataType == typeof(string))
                        {
                            defaultValue = "\"\"";
                        }
                        else if (info.DataType == typeof(long) || info.DataType == typeof(int) || info.DataType == typeof(byte))
                        {
                            defaultValue = "0";
                        }
                        else if (info.DataType == typeof(bool))
                        {
                            defaultValue = "true";
                        }
                        else if (info.DataType == typeof(DateTime))
                        {
                            defaultValue = "DateTime.Now";
                        }
                        Result.AppendLine($"            this.{info.ColumnName} = {defaultValue};");
                    }
                }

                Result.Append(
@"        }
    }
}
");
                return Result.ToString();
            }

            protected virtual void AppendInitMethodBody()
            {
                Result.Append(";\r\n");
            }

            protected virtual void AppendBaseType(string tableName)
            {
                Result.Append(
$@" : DbObjectModel<{tableName}>
    {{
");
            }

            protected virtual void BuildKeyColomn(DbColumnInfo info)
            {
            }

            protected StringBuilder AppendLine(string prefix, string content)
            {
                Result.Append(prefix);
                Result.AppendLine(content);

                return Result;
            }

            protected virtual void BuildColumn(DbColumnInfo info)
            {
                var prefix = "        ";
                if (info.AllowDBNull && !info.DataType.IsValueType)
                {
                    AppendLine(prefix, "[AllowNull]");
                }
                if (info.DataType == typeof(string) || info.DataType == typeof(byte[]))
                {
                    if (info.ColumnSize < 32768)
                    {
                        AppendLine(prefix, $"[Length({info.ColumnSize})]");
                    }
                }
                if (info.IsUnique)
                {
                    AppendLine(prefix, "[Index(UNIQUE = true)]");
                }

                var nullableTypeName = GetNullableTypeName(info);
                var columnBody = GetColumnBody();

                AppendLine(prefix, $"public {nullableTypeName} {info.ColumnName} {columnBody}");
            }

            protected virtual void ProcessKeyColumn(DbColumnInfo info)
            {
            }

            protected virtual void ProcessColumn(DbColumnInfo info)
            {
            }

            protected virtual string GetColumnBody()
            {
                return "{ get; set; }";
            }
        }

        public class ObjectModelBuilder : ModelBuilder
        {
            protected StringBuilder InitMethodBody = new StringBuilder();

            public ObjectModelBuilder(string tableName, List<DbColumnInfo> list) : base(tableName, list)
            {
            }

            protected override void AppendInitMethodBody()
            {
                Result.AppendLine("    {");
                if (InitMethodBody.Length > 3)
                {
                    Result.Append(InitMethodBody);
                    Result.Append(
@"        return this;
");
                }
                Result.AppendLine("    }");
            }

            protected override void AppendBaseType(string tableName)
            {
                Result.Append(
@" : IDbObject
    {
");
            }

            protected override void BuildKeyColomn(DbColumnInfo info)
            {
                Result.AppendLine(info.IsAutoIncrement ? "    [DbKey]" : "    [DbKey(IsDbGenerate = false)]");
                BuildColumn(info);
            }

            protected override void ProcessKeyColumn(DbColumnInfo info)
            {
                if (!info.IsAutoIncrement)
                {
                    ProcessColumn(info);
                }
            }

            protected override void ProcessColumn(DbColumnInfo info)
            {
                base.ProcessColumn(info);

                InitMethodBody.AppendLine($@"        this.{info.ColumnName} = {info.ColumnName};");
            }

            protected override string GetColumnBody()
            {
                return ";";
            }
        }

        public List<string> GetTableList()
        {
            return DbEntry.Provider.GetTableNames();
        }

        public string GenerateModelFromDatabase(string tableName, string outputPath = null)
        {
            var sb = new StringBuilder();
            if(string.IsNullOrEmpty(tableName) || tableName == "*")
            {//生成所有表
                foreach (var table in GetTableList())
                {
                    string contents = GetModel(table);
                    sb.AppendLine(contents);
                }
            }
            else
            {//仅生成指定表
                string contents = GetModel(tableName, outputPath);
                sb.AppendLine(contents);
            }

            return sb.ToString();
        }

        static string GetModel(string tableName, string outputPath = null)
        {
            var list = DbEntry.Provider.GetDbColumnInfoList(tableName);
            //如果包含Id列，则生成 ModelBuilder；否则生成 ObjectModelBuilder。
            var hasIdColumn = false;
            foreach (var info in list)
            {
                if (info.IsKey && info.IsAutoIncrement && info.ColumnName.ToLower() == "id")
                {
                    hasIdColumn = true;
                    break;
                }
            }

            string contents = "";
            if (hasIdColumn)
                contents = new ModelBuilder(tableName, list).Build();
            else
                contents = new ObjectModelBuilder(tableName, list).Build();

            if(!string.IsNullOrEmpty(outputPath))
            {//存储到文件
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                File.WriteAllText(Path.Combine(outputPath, $"{tableName}.cs"), contents, Encoding.UTF8);
                CreatePartialFile(Path.Combine(outputPath, $"{tableName}_Ex.cs"), tableName);
            }

            return contents;
        }

        /// <summary>
        /// 如果文件不存在，则创建Partial类。
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="tableName"></param>
        static void CreatePartialFile(string fileName, string tableName)
        {
            if (File.Exists(fileName))
                return;

            var sb = new StringBuilder();
            sb.Append(@"using Leafing.Data.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GuLinOA.Models
{
    public partial class " + tableName + @"
    {
    }
}");
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }
    }
}
