﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Handbrake.Queue
{
    public class Queue
    {
        private static XmlSerializer ser = new XmlSerializer(typeof(List<QueueItem>));
        List<QueueItem> queue = new List<QueueItem>();
        int id = 0; // Unique identifer number for each job
        private QueueItem lastItem;

        public List<QueueItem> getQueue()
        {
             return queue;
        }

        /// <summary>
        /// Get's the next CLI query for encoding
        /// </summary>
        /// <returns>String</returns>
        public string getNextItemForEncoding()
        {
            QueueItem job = queue[0];
            String query = job.Query;
            lastItem = job;
            remove(0);    // Remove the item which we are about to pass out.
            return query;
        }

        /// <summary>
        /// Get the last query that was returned by getNextItemForEncoding()
        /// </summary>
        /// <returns></returns>
        public QueueItem getLastQuery()
        {
            return lastItem;
        }

        /// <summary>
        /// Add's a new item to the queue
        /// </summary>
        /// <param name="query">String</param>
        public void add(string query, string source, string destination)
        {
            QueueItem newJob = new QueueItem();
            newJob.Id = id;
            newJob.Query = query;
            newJob.Source = source;
            newJob.Destination = destination;
            id++;

            // Adds the job to the queue
            queue.Add(newJob);
        }

        /// <summary>
        /// Removes an item from the queue.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Bolean true if successful</returns>
        public Boolean remove(int index)
        {
            queue.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Returns how many items are in the queue
        /// </summary>
        /// <returns>Int</returns>
        public int count()
        {
            return queue.Count;
        }

        /// <summary>
        /// Move an item with an index x, up in the queue
        /// </summary>
        /// <param name="index">Int</param>
        public void moveUp(int index)
        {
            if (index != 0)
            {
                QueueItem item = (QueueItem)queue[index];

                queue.Insert((index - 1), item);
                queue.RemoveAt((index + 1));
            }
        }

        /// <summary>
        /// Move an item with an index x, down in the queue
        /// </summary>
        /// <param name="index">Int</param>
        public void moveDown(int index)
        {
            if (index != queue.Count - 1)
            {
                QueueItem item = (QueueItem)queue[index];

                queue.Insert((index + 2), item);
                queue.RemoveAt((index));
            }
        }

        /// <summary>
        /// Writes the current queue to disk. hb_queue_recovery.xml
        /// This function is called after getNextItemForEncoding()
        /// </summary>
        public void write2disk(string file)
        {
            string tempPath = "";
            if (file == "hb_queue_recovery.xml")
                tempPath = Path.Combine(Path.GetTempPath(), "hb_queue_recovery.xml");
            else
                tempPath = file;

            try
            {
                using (FileStream strm = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    ser.Serialize(strm, queue);
                    strm.Close();
                    strm.Dispose();
                }
            }
            catch (Exception)
            {
                // Any Errors will be out of diskspace/permissions problems. 
                // Don't report them as they'll annoy the user.
            }
        }

        /// <summary>
        /// Writes the current queue to disk to the location specified in file
        /// </summary>
        /// <param name="file"></param>
        public void writeBatchScript(string file)
        {
            string queries = "";
            foreach (QueueItem queue_item in queue)
            {
                string q_item = queue_item.Query;
                string fullQuery = '"' + Application.StartupPath.ToString() + "\\HandBrakeCLI.exe" + '"' + q_item;

                if (queries == string.Empty)
                    queries = queries + fullQuery;
                else
                    queries = queries + " && " + fullQuery;
            }
            string strCmdLine = queries;

            if (file != "")
            {
                try
                {
                    // Create a StreamWriter and open the file, Write the batch file query to the file and 
                    // Close the stream
                    StreamWriter line = new StreamWriter(file);
                    line.WriteLine(strCmdLine);
                    line.Close();

                    MessageBox.Show("Your batch script has been sucessfully saved.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to write to the file. Please make sure that the location has the correct permissions for file writing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }

            }
        }

        /// <summary>
        /// Recover the queue from hb_queue_recovery.xml
        /// </summary>
        public void recoverQueue(string file)
        {
            string tempPath = "";
            if (file == "hb_queue_recovery.xml")
                tempPath = Path.Combine(Path.GetTempPath(), "hb_queue_recovery.xml");
            else
                tempPath = file;

            if (File.Exists(tempPath))
            {
                using (FileStream strm = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                {
                    if (strm.Length != 0)
                    {
                        List<QueueItem> list = ser.Deserialize(strm) as List<QueueItem>;

                        foreach (QueueItem item in list)
                            queue.Add(item);
                    }
                }
            }
        }
    }
}