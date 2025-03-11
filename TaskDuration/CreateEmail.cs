using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaskDuration
{
    class CreateEmail
    {
        #region Properties
        private static List<String> bl = new List<string>();      //list of bugs
        private static List<String> al = new List<string>();      //list of people w/ missing EoT or Assignment
    
        private static short task;                                //task number
        private static String taskAddress = null;                 //task URL
        private static String taskSummary = null;                 //task summary
        private static short hours;                               //task hours worked
    
        private static Int32 bugNumber = 0;                       //bug number
        private static String bugSummary = null;                  //bug summary
        private static String bugStatus = null;                   //bug status
        private static String newBugAddress = null;               //bug summary of Exported or Duplicate bug
        private static bool isDup = false;                        //denotes a duplicate bug
        private static bool isExp = false;                        //denotes an exported bug
    
        private static Regex reg = new Regex(@"bugid=\d+");
        private static bool isFirst = true;
        #endregion
    
        #region Public Methods
        ///Will use this to create an html file which can consequently be copy/pasted into a message.
        public void CreateEmailList(List<String> bugList, short issueID, String summary, List<String> assignmentList, string issueAddress, short hrs)
        {
            if (bugList != null)
            {
                bl = bugList;
                bl.Sort();
            }
    
            if (assignmentList != null)
            {
                al = assignmentList;
                al.Sort();
            }
    
            task = issueID;
            taskSummary = summary;
            taskAddress = issueAddress;
            hours = hrs;
    
            CreateHTMLText();
            Clear();
        }
        #endregion
    
        #region Private Methods
        /// <summary>
        /// Will save a local file in html format.
        /// </summary>
        private void CreateHTMLText()
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("EoT Report " + GetTime() + ".html", true))
            {
                //Setup font and size at beginning of document only
                if (isFirst)
                {
                    sw.WriteLine("<font face=\"calibri\" size=\"2\">");
                    isFirst = false;
                }
    
                #region Beginning of Individual Task Report
                sw.WriteLine("<p><B><a href=\"" + taskAddress + "\">Task " + task + "</a>:</B>" + taskSummary + "<p>Hours worked on task: " + hours + ".<p>Notes: ");
                #endregion
    
                #region List People Missing EoTs or Assignment
                if (al.Count > 0)
                {
                    foreach (string line in al)
                    {
                        sw.WriteLine("<p>" + line + " is missing an EoT or missing an Assignment.");
                    }
                }
                #endregion
    
                #region Create Table for Bug List
                if (bl != null && bl.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("\r\n<table><tr><td></td><td></td><td>BUGS ENCOUNTERED</td></tr>");
                    foreach (string line in bl)
                    {
                        string tempLine = line;
                        if (String.IsNullOrEmpty(tempLine))
                        {
                            continue;
                        }
    
                        tempLine = GetBugInfo(tempLine);
                        if (!String.IsNullOrEmpty(tempLine))
                        {
                            if (bugStatus.Contains("Verified") || bugStatus.Contains("Fixed"))
                            {
                                //Don't do anything here, we don't care about these bugs for the report.
                            }
                            else if (isDup || isExp)
                            {
                                //We'll try to get the actual status of the new bug here:
                                String tempString = (isDup) ? "(Duplicate)" : "(Exported)";
    
                                StringBuilder nsb = new StringBuilder("<a href=\"");
                                nsb.Append(newBugAddress + "\"> " + tempString + "</a>");
    
                                sb.Append("<tr>" +
                                          "<td>" + bugStatus +
                                          " " + nsb.ToString() +
                                          "</td>" +
                                          "<td><a href=\"" + tempLine + "\">Bug " + bugNumber + "</a></td>" +
                                          "<td>" + bugSummary + "</td>" +
                                          "</tr>");
                            }
                            else
                            {
                                sb.Append("<tr>" +
                                          "<td>" + bugStatus + "</td>" +
                                          "<td><a href=\"" + tempLine + "\">Bug " + bugNumber + "</a></td>" +
                                          "<td>" + bugSummary + "</td>" +
                                          "</tr>");
                            }
                        }
    
                        bugNumber = 0;
                        bugSummary = null;
                        bugStatus = null;
                        newBugAddress = null;
                        isDup = false;
                        isExp = false;
                    }
    
                    String completeTable = sb.ToString();
                    if (completeTable.Contains("<a href="))
                    {
                        sw.WriteLine(completeTable);
                        sw.WriteLine("</table>");
                    }
                    else
                    {
                        sw.WriteLine("<p>No bugs encountered.");
                    }
                }
                else
                {
                    sw.WriteLine("<p>No bugs encountered.");
                }
                #endregion
    
                sw.Close();
            }
        }
    
        private String GetBugInfo(String address)
        {
            WebSource ws = new WebSource();
            List<String> stringList = new List<string>();
            String[] sa;
    
            try
            {
                sa = ws.GetWebSource(address);
                foreach (string line in sa)
                {
                    if (line.Contains("Invalid page request, invalid issue id specified!"))
                    {
                        throw new Exception("Invalid page request, invalid issue id specified!");
                    }
                }
            }
            catch (Exception e)
            {
                if (String.IsNullOrEmpty(address))
                {
                    Console.WriteLine("Unable to connect due to null string.");
                }
                else
                {
                    Console.WriteLine("Unable to connect to " + address + "\r\n\tThe page responded: \t\n" + e.Message);
                }
                return null;
            }
    
            stringList = sa.ToList<String>();
            stringList.RemoveAll(RemoveNullOrEmpty);
            bool isFinished = false;
    
            foreach (String line in stringList)
            {
                String newLine = null;
    
                if (line.Contains("span id=\"lblLargeIssueID\""))
                {
                    newLine = line.Replace("<span id=\"lblLargeIssueID\">", "");
                    newLine = newLine.Replace("</span>", "");
                    bugNumber = Convert.ToInt32(newLine.Trim());
                    continue;
                }
    
                if (line.Contains("span id=\"lblTextField1\""))
                {
                    newLine = line.Replace("<span id=\"lblTextField1\" class=\"sub-tab-area-value\">", "");
                    newLine = newLine.Replace("</span>", "");
                    bugSummary = newLine.Trim();
                    bugSummary = bugSummary.Replace("&#39;", "'");
                    continue;
                }
    
                if (line.Contains("This was exported to "))
                {
                    String tempLine = GetNewAddress(stringList, stringList.IndexOf(line));
    
                    isDup = false;
                    isFinished = true;
    
                    newBugAddress = tempLine;
                }
                else if (line.Contains("This is a duplicate of "))
                {
                    String tempLine = GetNewAddress(stringList, stringList.IndexOf(line));
    
                    isExp = false;
                    isFinished = true;
    
                    newBugAddress = tempLine;
                }
    
                if (line.Contains("lblLargeStatus"))
                {
                    newLine = line.Replace("<span id=\"lblLargeStatus\">", "");
                    newLine = newLine.Replace("</span>", "");
                    bugStatus = newLine.Trim();
                    bugStatus = bugStatus.Replace("&#39;", "'");
    
                    if (bugStatus.Contains("Duplicate") || bugStatus.Contains("Exported"))
                    {
                        //Set both to true, we'll set one to false later.
                        isDup = true;
                        isExp = true;
                        isFinished = false;
                    }
                    else
                    {
                        isFinished = true;
                    }
    
                    continue;
                }
    
                if (bugNumber != 0 && bugSummary != null && bugStatus != null && isFinished)
                {
                    break;
                }
            }
    
            if (isDup != isExp && !String.IsNullOrEmpty(newBugAddress))
            {
                isDup = isExp = false;
                address = GetBugInfo(newBugAddress);
            }
    
            return address;
        }
    
        private static String GetTime()
        {
            string date = System.DateTime.Now.Date.ToString();
            date = date.Replace('/', '-');
            date = date.Replace("12:00:00 AM", "");
            string time = date.Trim();
            return time;
        }
    
        private static void Clear()
        {
            bl.Clear();
            al.Clear();
        }
    
        private static bool RemoveNullOrEmpty(String s)
        {
            return String.IsNullOrEmpty(s);
        }
    
        private static String GetNewAddress(List<String> sl, int count)
        {
            int index = count;
            String tempLine = sl[index].ToString().Trim();
            List<String> stringList;
    
            if (tempLine.Contains("<br>"))
            {
                String[] separator = new String[] { "<br>" };
                stringList = tempLine.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList<String>();
            }
            else
            {
                // We should have this:
                //<a href="LinkDeleteConfirmation.aspx?issueID=9999999&amp;linkID=888888&amp;projectID=999"><img src="/Content/images/icons/delete.gif" alt="" style="border-width:0px;" >/</a>This was exported to PROJECT bug <a href="IssueView.aspx?projectID=888&amp;issueID=99989998">9999</a><br>
    
                String[] separator = new String[] { "This" };
                stringList = tempLine.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList<String>();
                stringList.RemoveAt(0);
            }
    
            foreach (string line in stringList)
            {
                if (line.Contains("This is a duplicate of bug ") || line.Contains("was exported to "))
                {
                    tempLine = line;
                }
            }
    
            tempLine = tempLine.Replace("<a href=\"", "http://blizzard.internal.net/");
            stringList = tempLine.Split(' ').ToList<String>();
    
            // Go through the list to find the actual link.
            foreach (String line in stringList)
            {
                if (line.Contains("http://"))
                {
                    tempLine = line;
                }
            }
    
            // Dump the rest of the string after the actual URL.  We should be left with the complete URL and nothing else.
            int newIndex = tempLine.IndexOf("\">");
    
            // In case "\">" is not in the line.
            if (newIndex > 0)
            {
                tempLine = tempLine.Remove(newIndex);
            }
            tempLine = tempLine.Replace("amp;", "");
    
            return tempLine;
        }
        #endregion
    }
}
