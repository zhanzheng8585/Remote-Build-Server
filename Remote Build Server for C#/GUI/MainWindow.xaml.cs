///////////////////////////////////////////////////////////////////////////
//MainWindow.xaml.cs - Client prototype GUI for Remote Build Server      //
//Author : Zheng Zhan                                                    //
//Language:    C#, Visual Studio 2017                                    //
//Application: Remote Build Server,      CSE681 - SMA  Project4          //
//SUID: 825530128                                                        //
//SU Email: zzhan03@syr.edu                                              //
//Source:Dr. Jim Fawcett                                                 //
///////////////////////////////////////////////////////////////////////////
/*  
 *  ---------------< Please read Readme.txt first >--------------------------
 *  
 *  “Clear” button is to clear selected files List<string>.
 *
 *  “Add library” button is to add test library into existing
 *   BuildRequest, which is merge several xml files, Note that
 *   you must select xml files or it will crush!
 *
 *  “Create BuildRequest” button is to create a buildrequest
 *   by selecting testcode and test drive files and csproj file for them.
 *   
 *   
 *   Operations Instruction
 *   ----------------------
 *   1.You need to enter a number, which is the number of child.
 *   process you want.
 *   2.Click "start".
 *   3.Click "show files" to check FileList of code or logs.
 *   4.Choose file you want build into BuildRequest.
 *   5.Click "send and build".
 *   6.If you want to kill process, just click "kill process".
 *   
 *   if you want to add test library into existing BuildRequest,
 *   choose several xml files and click "Add library"
 *
 *   Sometimes, you may need to wait a moment for step 2 and 6.
 *   
 *   
 *  Purpose:
 *    Prototype for a client for the Pluggable Repository. The Remote Builder 
 *    will be accessed remotely from a GUI built using Windows Presentation 
 *    Foundation (WPF). You can enter a number > 0 to create some child process.
 *    It can also send build request to repo.
 *    
 *  Public Interface
 *  ----------------
 *    - MainWindow()            : The main function of GUI
 *  
 *  
 *  Required Files:
 *    MainWindow.xaml, MainWindow.xaml.cs - view into repository and checkin/checkout
 *
 *
 *  Maintenance History:
 *    ver 1.0 : 06 Dec 2017
 *    - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading;
using MessagePassingComm;
using System.Xml.Linq;
using Navigator;
using static System.Console;

namespace GUI
{
    public partial class MainWindow : Window
    {
        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        public static string ServicePath { get; set; } = "../../../ServiceFileStore";
        public static List<string> testfiles { get; set; } = new List<string>();
        public static string testfiles2 { get; set; } = "";
        public static string testfiles3 { get; set; } = "";
        public static string childnum { get; set; } = null;
        public static int count { get; set; } = 0;
        public static int count2 { get; set; } = 0;
        public static int childcount { get; set; } = 0;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Comm GUI; CommMessage msg;
        bool start = false;
        bool kill = false;
        bool check = true;
        Thread rcvThread = null;
        /*----< MainWindow >--------------------------*/

        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            msg = new CommMessage(CommMessage.MessageType.connect);
            GUI = new Comm("http://localhost", 8079);
            initializeMessageDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            autoTest();
        }

        private void autoTest(){
            start = true;
            // start two childs
            msg = new CommMessage(CommMessage.MessageType.request);
            msg.from = "http://localhost:8079/IPluggableComm";
            msg.to = "http://localhost:8080/IPluggableComm";
            msg.info = "2"; childcount = 2; GUI.postMessage(msg);
            msgbody.Items.Clear();
            //get file list
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.showfiles);
            msg1.from = "http://localhost:8079/IPluggableComm";
            msg1.to = "http://localhost:8081/IPluggableComm";
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopFiles";
            GUI.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getlogfiles";
            GUI.postMessage(msg2);
            // caeate a build request
            XDocument xml = new XDocument();
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            XComment comment = new XComment("Demonstration XML");xml.Add(comment);
            XElement root = new XElement("BuildRequest");xml.Add(root);
            XElement author = new XElement("author", "Zheng Zhan");root.Add(author);
            XElement child1 = new XElement("test");string testname = "test1";
            child1.SetAttributeValue("name", testname);
            XElement grandchild = new XElement("library", "td.cs");
            XElement grandchild2 = new XElement("library", "testcode.cs");
            XElement grandchild3 = new XElement("library", "plus.csproj");
            child1.Add(grandchild);child1.Add(grandchild2);child1.Add(grandchild3);root.Add(child1);
            string xmlname = "BuildRequest" + string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".xml";
            string destpath = System.IO.Path.Combine("../../../RepoStore/", xmlname);
            xml.Save(@destpath);
            string destpath2 = System.IO.Path.Combine("../../../RepoStore/logfile", xmlname);
            xml.Save(@destpath2);
            // send and build
            CommMessage msg3 = new CommMessage(CommMessage.MessageType.buildrequest);
            msg3.from = "http://localhost:8079/IPluggableComm";
            msg3.to = "http://localhost:8081/IPluggableComm";
            testfiles.Add(xmlname);
            msg3.arguments = testfiles;
            msg3.author = "Zheng Zhan";
            GUI.postMessage(msg3); msgbody.Items.Clear();
            testfiles = new List<string>();
            kill = false;
        }
        //----< define how to process each message command >-------------

        void initializeMessageDispatcher()
        {
            // load remoteFiles listbox with files from root

            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {
                localFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    localFiles.Items.Add(file);
                }
            };
            // load remoteDirs listbox with dirs from root

            messageDispatcher["getlogfiles"] = (CommMessage msg) =>
            {
                logfiles.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    logfiles.Items.Add(dir);
                }
            };

            messageDispatcher["testresult"] = (CommMessage msg) =>
            {
                testresult.Items.Add(msg.status);
            };

            messageDispatcher["buildresult"] = (CommMessage msg) =>
            {
                testresult.Items.Add(msg.status);
            };
        }
        //----< define processing for GUI's receive thread >-------------

        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = GUI.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }
        //----< make Environment equivalent to ClientEnvironment >-------

        void initializeEnvironment()
        {
            Navigator.Environment.root = Navigator.ClientEnvironment.root;
            Navigator.Environment.address = Navigator.ClientEnvironment.address;
            Navigator.Environment.port = Navigator.ClientEnvironment.port;
            Navigator.Environment.endPoint = Navigator.ClientEnvironment.endPoint;
        }
        /*----< Start Mother Build Process >--------------------------*/

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            childnum = num.Text;
        }

        /*----< Create a specified number of child process >--------------------------*/

        private void Button_Click_kill(object sender, RoutedEventArgs e)
        {
            if (start == true)
            {
                start = false;
                kill = true;
                childnum = num.Text;
                int temp;
                if (childnum == "")
                    temp = childcount;
                else
                    temp = Int32.Parse(childnum);
                int childid = 0;

                //Send close msg to each child process
                for (int i = 1; i <= temp; i++)
                {
                    CommMessage msg00 = new CommMessage(CommMessage.MessageType.close);
                    msg00.from = "http://localhost:8079/IPluggableComm";
                    childid = i + 8081;
                    msg00.to = "http://localhost:" + childid + "/IPluggableComm";
                    GUI.postMessage(msg00);
                }
            }
            else
            {
                MessageBox.Show("You need to click start first.");
            }
        }
        /*----< Create a specified number of child process >--------------------------*/

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(ServicePath);
            if (start == true)
            {
                if (testfiles.Capacity != 0)                        //send buildrequest from file1
                {
                    msg.type = CommMessage.MessageType.buildrequest;
                    msg.from = "http://localhost:8079/IPluggableComm";
                    msg.to = "http://localhost:8081/IPluggableComm";
                    msg.arguments = testfiles;
                    msg.author = "1";
                    GUI.postMessage(msg);
                    msgbody.Items.Clear();
                    testfiles = new List<string>();
                }
            }
            else
            {
                MessageBox.Show("Please wait or you need to click start first.");
            }
        }
        /*----< Create a specified number of child process >--------------------------*/

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (kill == true)
            {
                childnum = num.Text;
                if (childnum == "")
                {
                    MessageBox.Show("Please enter number of child process you need");
                    return;
                }
                else if (Int32.Parse(childnum) <= 0)
                {
                    MessageBox.Show("Please enter a meaningful number");
                    return;
                }
                else
                {
                    start = true;
                    kill = false;
                    msg = new CommMessage(CommMessage.MessageType.request);
                    msg.from = "http://localhost:8079/IPluggableComm";
                    msg.to = "http://localhost:8080/IPluggableComm";
                    msg.info = childnum;
                    GUI.postMessage(msg);
                }
            }
            else MessageBox.Show("Please kill the current child process");
        }
        /*----< add file to xml >--------------------------*/

        private void localFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            testfiles.Add(localFiles.SelectedValue.ToString());
            if (check == true)
            {
                testfiles2 = localFiles.SelectedValue.ToString();
                check = false;
            }
            else
            {
                testfiles3 = localFiles.SelectedValue.ToString();
                check = true;
            }
            msgbody.Items.Add(localFiles.SelectedValue.ToString());
        }

        public void getTopFiles()
        {
            List<string> files = fileMgr.getFiles().ToList<string>();
            localFiles.Items.Clear();
            foreach (string file in files)
            {
                localFiles.Items.Add(file);
            }
        }

        private void localTop_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.showfiles);
            msg1.from = "http://localhost:8079/IPluggableComm";
            msg1.to = "http://localhost:8081/IPluggableComm";
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopFiles";
            GUI.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getlogfiles";
            GUI.postMessage(msg2);
        }

        private void build_xml(object sender, RoutedEventArgs e)
        {
            XDocument xml = new XDocument();
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            /*
             *  It is a quirk of the XDocument class that the XML declaration,
             *  a valid element, cannot be added to the XDocument's element
             *  collection.  Instead, it must be assigned to the document's
             *  Declaration property.
             */
            XComment comment = new XComment("Demonstration XML");
            xml.Add(comment);
            XElement root = new XElement("BuildRequest");
            xml.Add(root);
            XElement author = new XElement("author", "Zheng Zhan");
            root.Add(author);
            XElement child1 = new XElement("test");
            count2 = count2 + 1;
            string testname = "test" + count2;
            child1.SetAttributeValue("name", testname);
            foreach(string files in testfiles)
            {
                XElement grandchild = new XElement("library", files);
                child1.Add(grandchild);
            }
            root.Add(child1);
            count = count + 1;
            string xmlname = "BuildRequest" + string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".xml";
            string destpath = System.IO.Path.Combine("../../../RepoStore/", xmlname);
            xml.Save(@destpath);
            string destpath2 = System.IO.Path.Combine("../../../RepoStore/logfile", xmlname);
            xml.Save(@destpath2);
            testfiles = new List<string>();
            msgbody.Items.Clear();
            getTopFiles();
        }

        private void clearmsg_Click(object sender, RoutedEventArgs e)
        {
            msgbody.Items.Clear();
            testfiles = new List<string>();
        }

        private void logfiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = logfiles.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(Navigator.ClientEnvironment.logdir, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string xmlname = "BuildRequest" + string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".xml";
            XDocument buildReq = new XDocument();
            XElement buildReqElm = new XElement("BuildRequest");
            buildReq.Add(buildReqElm);

            foreach (var item in testfiles)
            {
                string file = System.IO.Path.Combine("../../../RepoStore/", item);
                file = System.IO.Path.GetFullPath(file);
                XElement e1 = XElement.Load(file);
                buildReqElm.Add(e1);
            }
            string destpath = System.IO.Path.Combine("../../../RepoStore/", xmlname);
            buildReq.Save(destpath);
            string destpath2 = System.IO.Path.Combine("../../../RepoStore/logfile", xmlname);
            buildReq.Save(destpath2);
            msgbody.Items.Clear();
        }
    }
}