﻿using System;
using System.Collections.Generic;
using System.Text;
using Lephone.Data;
using Lephone.Data.SqlEntry;

namespace Lephone.CodeGen
{
    public class ModelsGenerator
    {
        public class ModelBuilder
        {
            private readonly Dictionary<Type, string> _types;
            protected string TableName;
            protected List<DbColumnInfo> InfoList;
            protected StringBuilder Result;
            protected StringBuilder InitDefine;

            public ModelBuilder(string tableName, List<DbColumnInfo> list)
            {
                TableName = tableName;
                InfoList = list;

                Result = new StringBuilder();
                InitDefine = new StringBuilder();

                _types = new Dictionary<Type, string>
                             {
                                 {typeof (string), "string"},
                                 {typeof (int), "int"},
                                 {typeof (short), "short"},
                                 {typeof (long), "long"},
                                 {typeof (float), "float"},
                                 {typeof (double), "double"},
                                 {typeof (DateTime), "DateTime"},
                                 {typeof (bool), "bool"},
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

            public virtual string Build()
            {
                Result.Append("public").Append(GetAbstract()).Append("class ").Append(TableName);
                AppendBaseType(TableName);
                foreach (var info in InfoList)
                {
                    if(info.IsKey)
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
                if (InitDefine.Length > 2)
                {
                    InitDefine.Length -= 2;
                    Result.Append("\tpublic").Append(GetAbstract()).Append(TableName).Append(" ");
                    Result.Append("Initialize(");
                    Result.Append(InitDefine);
                    Result.Append(")");
                    AppendInitMethodBody();

                }
                Result.Append("}\r\n");
                return Result.ToString();
            }

            protected virtual string GetAbstract()
            {
                return " abstract ";
            }

            protected virtual void AppendInitMethodBody()
            {
                Result.Append(";\r\n");
            }

            protected virtual void AppendBaseType(string tableName)
            {
                Result.Append(" : LinqObjectModel<").Append(tableName).Append(">\r\n{\r\n");
            }

            protected virtual void BuildKeyColomn(DbColumnInfo info)
            {
            }

            protected virtual void BuildColumn(DbColumnInfo info)
            {
                Result.Append("\tpublic").Append(GetAbstract());
                Result.Append(GetTypeName(info.DataType));
                Result.Append(" ");
                Result.Append(info.ColumnName);
                Result.Append(GetColumnBody());
                Result.Append("\r\n");
            }

            protected virtual void ProcessKeyColumn(DbColumnInfo info)
            {
            }

            protected virtual void ProcessColumn(DbColumnInfo info)
            {
                InitDefine.Append(GetTypeName(info.DataType));
                InitDefine.Append(" ");
                InitDefine.Append(info.ColumnName);
                InitDefine.Append(", ");
            }

            protected virtual string GetColumnBody()
            {
                return " { get; set; }";
            }
        }

        public class ObjectModelBuilder : ModelBuilder
        {
            protected StringBuilder InitMethodBody = new StringBuilder();

            public ObjectModelBuilder(string tableName, List<DbColumnInfo> list) : base(tableName, list)
            {
            }

            protected override string GetAbstract()
            {
                return " ";
            }

            protected override void AppendInitMethodBody()
            {
                Result.Append("{\r\n");
                if(InitMethodBody.Length > 3)
                {
                    Result.Append(InitMethodBody);
                    Result.Append("\t\treturn this;\r\n");
                }
                Result.Append("\t}\r\n");
            }

            protected override void AppendBaseType(string tableName)
            {
                Result.Append(" : IDbObject\r\n");
            }

            protected override void BuildKeyColomn(DbColumnInfo info)
            {
                if (info.IsAutoIncrement)
                {
                    Result.Append("\t[DbKey]\r\n");
                }
                else
                {
                    Result.Append("\t[DbKey(IsDbGenerate = false)]\r\n");
                }
                BuildColumn(info);
            }

            protected override void ProcessKeyColumn(DbColumnInfo info)
            {
                if(!info.IsAutoIncrement)
                {
                    ProcessColumn(info);
                }
            }

            protected override void ProcessColumn(DbColumnInfo info)
            {
                base.ProcessColumn(info);

                InitMethodBody.Append("\t\tthis.");
                InitMethodBody.Append(info.ColumnName);
                InitMethodBody.Append(" = ");
                InitMethodBody.Append(info.ColumnName);
                InitMethodBody.Append(";\r\n");
            }

            protected override string GetColumnBody()
            {
                return ";";
            }
        }

        public List<string> GetTableList()
        {
            return DbEntry.Context.GetTableNames();
        }

        public string GenerateModelFromDatabase(string tableName)
        {
            if(tableName.ToLower() == "*")
            {
                var sb = new StringBuilder();
                foreach (var table in GetTableList())
                {
                    string s = GetModel(table);
                    sb.Append(s);
                    sb.Append("\r\n");
                }
                return sb.ToString();
            }
            return GetModel(tableName);
        }

        private static string GetModel(string tableName)
        {
            var list = DbEntry.Context.GetDbColumnInfoList(tableName);
            foreach (var info in list)
            {
                if (info.IsKey && info.IsAutoIncrement && info.ColumnName.ToLower() == "id")
                {
                    return new ModelBuilder(tableName, list).Build();
                }
            }
            return new ObjectModelBuilder(tableName, list).Build();
        }
    }
}
