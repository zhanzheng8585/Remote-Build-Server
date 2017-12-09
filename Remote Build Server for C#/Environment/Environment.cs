///////////////////////////////////////////////////////////////////////////
// Environment.cs - defines environment properties for Client and Server //
// ver 1.0                                                               //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017       //
///////////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * --------------------
 * ver 1.0 : 23 Oct 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Navigator
{
    public struct Environment
    {
        public static string root { get; set; }
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; }
        public static string address { get; set; }
        public static int port { get; set; }
        public static bool verbose { get; set; }
        public static string logdir { get; set; }
    }

    public struct ClientEnvironment
    {
        public static string root { get; set; } = "../../../RepoStore/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8077/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8077;
        public static bool verbose { get; set; } = false;
        public static string logdir { get; set; } = "../../../RepoStore/logfile";
    }

    public struct ServerEnvironment
    {
        public static string root { get; set; } = "../../../ServiceFileStore/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8078/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8078;
        public static bool verbose { get; set; } = false;
    }
}
