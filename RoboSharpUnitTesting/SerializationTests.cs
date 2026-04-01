using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class SerializationTests
    {
        public TestContext TestContext { get; set; }
        private string Destination { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            Destination = Test_Setup.GetNewTempPath();
        }

        [TestCleanup]
        public void Cleanup() => Test_Setup.ClearOutTestDestination(Destination);

        [TestMethod]
        public void Test_JobFileSerializer()
        {
            IRoboCommand[] arr1 = new IRoboCommand[]
            {
                new RoboCommand("Job1", true),
                new RoboCommand("Job2", true),
                new RoboCommand("Job3", true)
            };
            string path = Path.Combine(Destination);
            var serializer = new JobFileSerializer();
            serializer.Serialize(arr1, path);
            var obj = serializer.Deserialize(path);
            Assert.IsTrue(obj.Count() == 3);
        }

        [TestMethod]
        public void Test_XmlSerializer()
        {
            Directory.CreateDirectory(Destination);
            var serializer = new RoboSharp.RoboCommandXmlSerializer();
            var commands = new IRoboCommand[]
            {
                new RoboCommand(Test_Setup.Source_Standard, Destination, "Test1", true),
                new RoboCommand(Test_Setup.Source_Standard, Destination, copyActionFlags: CopyActionFlags.Purge, loggingFlags: LoggingFlags.IncludeFullPathNames) {Name = "Test2" },
                new RoboCommand(Test_Setup.Source_Standard, Destination, CopyActionFlags.MoveFilesAndDirectories, SelectionFlags.ExcludeLonely, LoggingFlags.NoDirectoryList){Name = "Test3" }
            };
            commands[0].CopyOptions.AddFileFilter("*.pdf", "*.txt");
            commands[1].SelectionOptions.ExcludedDirectories.AddRange(new string[] { "Archive", "SomeDirectory" });
            commands[2].SelectionOptions.ExcludedFiles.AddRange(new string[] { "SomeFile.txt", "SomeOtherFile.pdf" });
            string path = Path.Combine(Destination, "XmlSerializerTest.xml");
            serializer.Serialize(commands, path);
            Assert.IsTrue(File.Exists(path), "Failed to create file.");
            var readCommands = serializer.Deserialize(path).ToArray();
            Assert.AreEqual(commands.Length, readCommands.Length, "\nParsed count != Input Count");

            for (int i = 0; i < commands.Length; i++)
            {
                Console.WriteLine("\nCommand Index " + i);
                Console.WriteLine("Input  : " + commands[i].CommandOptions);
                Console.WriteLine("Output : " + readCommands[i].CommandOptions);
                Assert.AreEqual(commands[i].CommandOptions, readCommands[i].CommandOptions, $"\n Command index {i} Input != Output");
            }
        }

        [TestMethod]
        public void Test_RoboQueueCollectionChanged()
        {
            Directory.CreateDirectory(Destination);
            string path = Path.Combine(Destination, "XmlSerializerTest.xml");
            if (!File.Exists(path)) Test_XmlSerializer();

            RoboQueue Q = new RoboQueue();
            List<IRoboCommand> startedCommands = new List<IRoboCommand>();
            List<Results.RoboCopyResults> resultsData = new List<Results.RoboCopyResults>();
            Q.CollectionChanged += Q_CollectionChanged;
            Q.OnCommandCompleted += Q_OnCommandCompleted;
            Q.OnCommandStarted += Q_OnCommandStarted;
            Q.OnCommandError += Q_OnCommandError;
            var commands = new RoboCommandXmlSerializer().Deserialize(path).ToList();
            commands.ForEach(cmd => cmd.JobOptions.PreventCopyOperation = false);
            commands.ForEach(cmd => cmd.LoggingOptions.ListOnly = true);
            Q.AddCommand(commands[0], commands[1]);
            bool assertedSuccess = false;
            Q.StartAll().Wait();

            // Remove
            Q.RemoveCommand(commands[0]);
            Q.RemoveCommand(commands[1]);
            Q.RemoveCommand(commands[2]);
            Assert.IsTrue(assertedSuccess);

            void Q_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    Assert.IsTrue(e.NewItems.Count == 2);
                }
                
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    var removedItem = e.OldItems[0] as IRoboCommand;
                    Assert.IsNotNull(removedItem);
                    Assert.AreEqual(1, resultsData.RemoveAll(p => p.JobName == removedItem.Name));
                    assertedSuccess = true;
                }
            }

            void Q_OnCommandCompleted(IRoboCommand sender, RoboCommandCompletedEventArgs e)
            {
                resultsData.Add(e.Results);
            }

            void Q_OnCommandStarted(RoboQueue sender, EventArgObjects.RoboQueueCommandStartedEventArgs e)
            {
                startedCommands.Add(e.Command);
            }

            void Q_OnCommandError(IRoboCommand sender, CommandErrorEventArgs e)
            {
                string err = "Command Error: " + e.Error;
                Console.WriteLine(err);
                throw new Exception(err);
            }
        }
    }
}
