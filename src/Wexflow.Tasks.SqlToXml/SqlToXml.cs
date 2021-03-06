﻿using System;
using System.Collections.Generic;
using Wexflow.Core;
using System.Threading;
using System.Xml.Linq;
using System.Data.SqlClient;
using Oracle.DataAccess.Client;
using MySql.Data.MySqlClient;
using System.Data.SQLite;
using Npgsql;
using System.IO;
using System.Data.OleDb;
using System.Security;
using Teradata.Client.Provider;

namespace Wexflow.Tasks.SqlToXml
{
    public enum Type
    {
        SqlServer,
        Access,
        Oracle,
        MySql,
        Sqlite,
        PostGreSql,
        Teradata
    }

    public class SqlToXml : Task
    {
        public Type DbType { get; set; }
        public string ConnectionString { get; set; }
        public string SqlScript { get; set; }

        public SqlToXml(XElement xe, Workflow wf)
            : base(xe, wf)
        {
            DbType = (Type)Enum.Parse(typeof(Type), GetSetting("type"), true);
            ConnectionString = GetSetting("connectionString");
            SqlScript = GetSetting("sql", string.Empty);
        }

        public override TaskStatus Run()
        {
            Info("Executing SQL scripts...");

            bool success = true;
            bool atLeastOneSucceed = false;

            // Execute SqlScript if necessary
            try
            {
                if (!string.IsNullOrEmpty(SqlScript))
                {
                    ExecuteSql(SqlScript);
                    Info("The script has been executed through the sql option of the task.");
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while executing sql script. Error: {0}", e.Message);
                success = false;
            }

            // Execute SQL files scripts
            foreach (FileInf file in SelectFiles())
            {
                try
                {
                    var sql = File.ReadAllText(file.Path);
                    ExecuteSql(sql);
                    InfoFormat("The script {0} has been executed.", file.Path);

                    if (!atLeastOneSucceed) atLeastOneSucceed = true;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("An error occured while executing sql script {0}. Error: {1}", file.Path, e.Message);
                    success = false;
                }
            }

            var status = Status.Success;

            if (!success && atLeastOneSucceed)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status, false);
        }

        void ExecuteSql(string sql)
        {
            switch (DbType)
            {
                case Type.SqlServer:
                    using (var conn = new SqlConnection(ConnectionString))
                    {
                        var comm = new SqlCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("SqlServer_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                //xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column) , new XAttribute("value", SecurityElement.Escape(reader[column].ToString())))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.Access:
                    using (var conn = new OleDbConnection(ConnectionString))
                    {
                        var comm = new OleDbCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        if (reader != null)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i));
                            }
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("Access_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader != null && reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.Oracle:
                    using (var conn = new OracleConnection(ConnectionString))
                    {
                        var comm = new OracleCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("Oracle_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.MySql:
                    using (var conn = new MySqlConnection(ConnectionString))
                    {
                        var comm = new MySqlCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("MySql_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.Sqlite:
                    using (var conn = new SQLiteConnection(ConnectionString))
                    {
                        var comm = new SQLiteCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("Sqlite_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.PostGreSql:
                    using (var conn = new NpgsqlConnection(ConnectionString))
                    {
                        var comm = new NpgsqlCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("PostGreSql_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
                case Type.Teradata:
                    using (var conn = new TdConnection(ConnectionString))
                    {
                        var comm = new TdCommand(sql, conn);
                        conn.Open();
                        var reader = comm.ExecuteReader();

                        // Get column names
                        var columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Build Xml
                        string destPath = Path.Combine(Workflow.WorkflowTempFolder
                            , string.Format("Teradata_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));
                        var xdoc = new XDocument();
                        var xobjects = new XElement("Records");
                        while (reader.Read())
                        {
                            var xobject = new XElement("Record");
                            foreach (var column in columns)
                            {
                                xobject.Add(new XElement("Cell", new XAttribute("column", SecurityElement.Escape(column)), new XAttribute("value", SecurityElement.Escape(reader[column].ToString()))));
                            }
                            xobjects.Add(xobject);
                        }
                        xdoc.Add(xobjects);
                        xdoc.Save(destPath);
                        Files.Add(new FileInf(destPath, Id));
                        InfoFormat("XML file generated: {0}", destPath);
                    }
                    break;
            }
        }
    }
}
