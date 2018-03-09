﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace FKDbManager
{
    class Program
    {
        public static string backupFolder = "";
        static void Main(string[] args)
        {
            PrintSummary();

            //
            if (!System.IO.Directory.Exists(Properties.Settings.Default.BackupPath))
            {
                Console.WriteLine(Properties.Settings.Default.BackupPath + " <<<<< NOT FOUND!");
                return;
            }

            backupFolder = Properties.Settings.Default.BackupPath + "\\" + Properties.Settings.Default.Database;

            if (!System.IO.Directory.Exists(backupFolder))
                System.IO.Directory.CreateDirectory(backupFolder);
            //
            bool flag = true;
            while (flag)
            {
                String command = "";

                Console.Write("[l=list] [b=create] [r=restore] [q=bye] Command? ");
                command = Console.ReadLine();

                switch (command)
                {
                    case "l":
                        ListFiles();
                        break;
                    case "b":
                        String label = "";
                        Echo("+++++++++++++++++++++++++++++++++++", ConsoleColor.Green);
                        Echo("Label: ", ConsoleColor.Green);
                        label = Console.ReadLine();

                        string fileName = Properties.Settings.Default.Database + "-" + label + ".bak";
                        Echo("+++++++++++++++++++++++++++++++++++", ConsoleColor.Yellow);
                        Echo("Creating... " + fileName, ConsoleColor.Green);

                        Backup(backupFolder + "\\" + fileName);

                        Echo("DONE", ConsoleColor.Green);

                        break;
                    case "r":
                        var lst = ListFiles();
                        Echo("Which one?", ConsoleColor.Magenta);
                        var ix = Console.ReadLine();
                        int index = -1;
                        int.TryParse(ix, out index);
                        if (index - 1 >= lst.Count || index < 0)
                        {
                            Echo("Stoned or something?", ConsoleColor.Red);
                            continue;
                        }

                        var file = lst[index - 1];

                        Echo("+++++++++++++++++++++++++++++++++++", ConsoleColor.Magenta);
                        Echo("Restoring (type: yes to continue): " + file, ConsoleColor.Magenta);
                        ix = Console.ReadLine();

                        if (ix.CompareTo("yes") == 0)
                            Restore(file);

                        Echo("DONE", ConsoleColor.Magenta);

                        break;
                    case "q":
                        flag = false;
                        break;
                    default:
                        Echo("Meh?!", ConsoleColor.Cyan);
                        break;
                }
            }
        }

        static void PrintSummary()
        {
            var b = Console.BackgroundColor;
            var f = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.DarkRed;

            //
            Console.WriteLine("\n++++++++ CONFIGURATION ++++++++");
            Console.WriteLine("Connection String: " + Properties.Settings.Default.ConnectionStringWithMaster);
            Console.WriteLine("Backup Store: " + Properties.Settings.Default.BackupPath);
            Console.WriteLine("Target Database: " + Properties.Settings.Default.Database);
            //

            Console.WriteLine("++++++++++++++++++++++++++++++");
            Console.BackgroundColor = b;
            Console.ForegroundColor = f;
        }
        static List<String> ListFiles()
        {
            List<String> lst = System.IO.Directory.EnumerateFiles(backupFolder).ToList();
            var f = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            //
            int i = 0;
            foreach (var l in lst)
                Console.WriteLine(++i + " - " + l);
            //
            Console.ForegroundColor = f;

            return lst;
        }
        static void Echo(String msg, System.ConsoleColor c)
        {
            var f = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(msg);
            Console.ForegroundColor = f;
        }

        static void Restore(string file)
        {
            String c = "";
            String db = Properties.Settings.Default.Database;

            using (SqlConnection con = new SqlConnection(Properties.Settings.Default.ConnectionStringWithMaster))
            {
                con.Open();
                SqlCommand cmd = null;

                c = "ALTER DATABASE [" + db + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                cmd = new SqlCommand
                {
                    CommandText = c,
                    CommandTimeout = int.MaxValue,
                    CommandType = System.Data.CommandType.Text,
                    Connection = con
                };
                cmd.ExecuteNonQuery();
                cmd.Dispose();

                c = "RESTORE DATABASE [" + db + "] FROM  DISK = N'" + file + "' WITH  FILE = 1,  NOUNLOAD,  REPLACE,  STATS = 5";
                cmd = new SqlCommand
                {
                    CommandText = c,
                    CommandTimeout = int.MaxValue,
                    CommandType = System.Data.CommandType.Text,
                    Connection = con
                };
                cmd.ExecuteNonQuery();
                cmd.Dispose();

                c = "ALTER DATABASE [" + db + "] SET MULTI_USER";
                cmd = new SqlCommand
                {
                    CommandText = c,
                    CommandTimeout = int.MaxValue,
                    CommandType = System.Data.CommandType.Text,
                    Connection = con
                };
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                //

                con.Close();
                con.Dispose();
            }
        }

        static void Backup(string file)
        {
            String c = "";
            String db = Properties.Settings.Default.Database;

            c += "BACKUP DATABASE [" + db + "] TO  DISK = N'" + file + "' WITH NOFORMAT, INIT,  NAME = N'" + db + "-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10, CHECKSUM";

            using (SqlConnection con = new SqlConnection(Properties.Settings.Default.ConnectionStringWithMaster))
            {
                con.Open();

                //
                SqlCommand cmd = new SqlCommand
                {
                    CommandText = c,
                    CommandTimeout = int.MaxValue,
                    CommandType = System.Data.CommandType.Text,
                    Connection = con
                };
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                //

                con.Close();
                con.Dispose();
            }
        }
    }
}
