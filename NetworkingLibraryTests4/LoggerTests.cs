using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkingLibrary;
using NUnit.Framework;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class LoggerTests
    {
        [Test()]
        public void WriteLineTest_OverwriteMode()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            Logger testLogger = new Logger(filepath, LoggingMode.OVERWRITE, LoggingFormat.JUSTMESSAGE);
            string expected = "line2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void WriteLineTest_AppendMode()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            Logger testLogger = new Logger(filepath, LoggingMode.APPEND, LoggingFormat.JUSTMESSAGE);
            string expected = "line1\r\nline2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void WriteLineTest_AppendMode_And_DATETIMEANDMESSAGE()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            DateTime now = DateTime.Now;
            Logger testLogger = new Logger(filepath, LoggingMode.APPEND, LoggingFormat.DATETIMEANDMESSAGE);
            string expected = $"[{now:dd/MM/yy} | {now:HH:mm:ss}] line1\r\n[{now:dd/MM/yy} | {now:HH:mm:ss}] line2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void WriteLineTest_AppendMode_And_DATETIMEANDMESSAGE_AMERICAN()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            DateTime now = DateTime.Now;
            Logger testLogger = new Logger(filepath, LoggingMode.APPEND, LoggingFormat.DATETIMEANDMESSAGE, true);
            string expected = $"[{now:MM/dd/yy} | {now:HH:mm:ss}] line1\r\n[{now:MM/dd/yy} | {now:HH:mm:ss}] line2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void WriteLineTest_AppendMode_And_TIMEANDMESSAGE()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            DateTime now = DateTime.Now;
            Logger testLogger = new Logger(filepath, LoggingMode.APPEND, LoggingFormat.TIMEANDMESSAGE);
            string expected = $"[{now:HH:mm:ss}] line1\r\n[{now:HH:mm:ss}] line2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void WriteLineTest_AppendMode_And_DATEANDMESSAGE()
        {
            // Arrange
            string filepath = "unitTest.txt";

            // Clear file
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.Write(string.Empty);
            writer.Close();

            DateTime now = DateTime.Now;
            Logger testLogger = new Logger(filepath, LoggingMode.APPEND, LoggingFormat.DATEANDMESSAGE);
            string expected = $"[{now:dd/MM/yy}] line1\r\n[{now:dd/MM/yy}] line2\r\n";

            // Act
            testLogger.Log("line1");
            testLogger.Log("line2");

            StreamReader reader = new StreamReader(filepath);
            string actual = reader.ReadToEnd();
            reader.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
