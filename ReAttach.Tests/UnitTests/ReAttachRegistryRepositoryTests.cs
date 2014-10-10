﻿using System.IO;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ReAttach.Contracts;
using ReAttach.Data;

namespace ReAttach.Tests.UnitTests
{
	[TestClass]
	public class ReAttachRegistryRepositoryTests
	{
		[TestMethod]
		public void NullKeysTest()
		{
			var package = new Mock<IReAttachPackage>(MockBehavior.Strict);
			package.Setup(p => p.OpenUserRegistryRoot()).Returns<IRegistryKey>(null);
			package.Setup(p => p.Reporter).Returns(new ReAttachTraceReporter());

			var repository = new ReAttachRegistryRepository(package.Object);
			Assert.IsNull(repository.LoadTargets(), 
				"Null key should have resulted in empty result from load method.");
			Assert.IsFalse(repository.SaveTargets(new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize)),
				"Null key should have resulted in false result from save method.");

			var key = new Mock<IRegistryKey>(MockBehavior.Strict);
			key.Setup(k => k.OpenSubKey(It.IsAny<string>())).Returns<IRegistryKey>(null);
			key.Setup(k => k.CreateSubKey(It.IsAny<string>())).Returns<IRegistryKey>(null);
			key.Setup(k => k.Close());
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);

			Assert.IsNull(repository.LoadTargets(),
				"Null subkey should have resulted in empty result from load method.");
			Assert.IsFalse(repository.SaveTargets(new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize)),
				"Null subkey should have resulted in false result from save method.");
		}

		[TestMethod]
		public void EmptySaveTest()
		{
			var key = new Mock<IRegistryKey>();
			var subkey = new Mock<IRegistryKey>();
			key.Setup(k => k.CreateSubKey(It.IsAny<string>())).Returns(subkey.Object);
			
			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			var repository = new ReAttachRegistryRepository(package.Object);

			var targets = new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize);
			Assert.IsTrue(repository.SaveTargets(targets));

			key.Verify(k => k.CreateSubKey(It.IsAny<string>()), Times.Once());
			subkey.Verify(k => k.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
			subkey.Verify(k => k.Close(), Times.Once());
		}

		[TestMethod]
		public void SaveTest()
		{

			var key = new Mock<IRegistryKey>();
			var subkey = new Mock<IRegistryKey>();
			key.Setup(k => k.CreateSubKey(It.IsAny<string>())).Returns(subkey.Object);

			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			var repository = new ReAttachRegistryRepository(package.Object);
		
			var targets = new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize);
			for (var i = 1; i <= 3; i++)
				targets.AddFirst(new ReAttachTarget(i, "path" + i, "user" + i));

			Assert.IsTrue(repository.SaveTargets(targets));
			key.Verify(k => k.CreateSubKey(It.IsAny<string>()), Times.Once());

			for (var i = 1; i <= 3; i++)
			{
				subkey.Verify(k => k.SetValue(
					ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + i,
					It.IsAny<string>()), Times.Once());
			}

			subkey.Verify(k => k.Close(), Times.Once());	
		}

		[TestMethod]
		public void SaveErrorTest()
		{
			var key = new Mock<IRegistryKey>(MockBehavior.Strict);
			var subkey = new Mock<IRegistryKey>(MockBehavior.Strict);
			key.Setup(k => k.CreateSubKey(It.IsAny<string>())).Returns(subkey.Object);

			subkey.Setup(k => k.SetValue(It.IsAny<string>(), It.IsAny<string>())).Throws
				(new SecurityException("Simulating no access when setting value. :)"));
			subkey.Setup(k => k.DeleteValue(It.IsAny<string>(), It.IsAny<bool>())).Throws
				(new SecurityException("Simulating no access when deleting value. :)"));

			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			package.Setup(p => p.Reporter).Returns(new ReAttachTraceReporter());
			var repository = new ReAttachRegistryRepository(package.Object);

			var targets = new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize);
			Assert.IsFalse(repository.SaveTargets(targets));

			for (var i = 1; i <= 3; i++)
				targets.AddFirst(new ReAttachTarget(i, "path" + i, "user" + i));

			Assert.IsFalse(repository.SaveTargets(targets));
		}

		[TestMethod]
		public void LoadErrorTest()
		{
			var key = new Mock<IRegistryKey>();
			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			package.Setup(p => p.Reporter).Returns(new ReAttachTraceReporter());
			var repository = new ReAttachRegistryRepository(package.Object);

			Assert.IsNull(repository.LoadTargets(), "Non-null result on first load, no key should mean null return value.");

			key.Setup(k => k.OpenSubKey(It.IsAny<string>())).
				Throws(new SecurityException("Simulating no access. :)"));

			Assert.IsNull(repository.LoadTargets(), "Either SecurityException wasn't thrown, or there's a problem with error handling in load method.");
			key.Verify(k => k.OpenSubKey(It.IsAny<string>()), Times.Exactly(2));
		}

		[TestMethod]
		public void LoadEmptyTest()
		{
			var key = new Mock<IRegistryKey>();
			var subkey = new Mock<IRegistryKey>();
			key.Setup(k => k.OpenSubKey(ReAttachConstants.ReAttachRegistryKeyName)).Returns(subkey.Object);

			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			var repository = new ReAttachRegistryRepository(package.Object);

			var result = repository.LoadTargets();
			Assert.IsNotNull(result, "Empty set loaded resulted in null result, should result in empty list.");
			Assert.AreEqual(0, result.Count, "Empty set loaded resulted in non-empty result, should result in empty list.");
			key.Verify(k => k.OpenSubKey(It.IsAny<string>()), Times.Once());
		}

		[TestMethod]
		public void LoadInvalidDataTest()
		{
			const string json1 = "{\"ProcessId\":7024,\"ProcessName\":\"test1.exe\",\"ProcessPath\":\"test1.exe\",\"ProcessUser\":\"TEST1\",\"ServerName\":\"\",\"IsAttached\":false,\"IsLocal\":true,\"Engine\":null}";
			const string json2 = "{\"ProcessId\":7025,\"ProcessName\":\"test2.exe\",\"ProcessPath\":\"test2.exe\",\"ProcessUser\":\"TEST2\",\"ServerName\":\"\",\"IsAttached\":false,\"IsLocal\":true,\"Engine\":null}";

			var key = new Mock<IRegistryKey>();
			var subkey = new Mock<IRegistryKey>();
			key.Setup(k => k.OpenSubKey(ReAttachConstants.ReAttachRegistryKeyName)).Returns(subkey.Object);

			subkey.Setup(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + 1)).Returns(json1);
			subkey.Setup(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + 2)).Returns("invalid-json-item");
			subkey.Setup(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + 3)).Returns(json2);

			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			var repository = new ReAttachRegistryRepository(package.Object);

			var result = repository.LoadTargets();

			Assert.IsNotNull(result, "Empty set loaded resulted in null result, should result in empty list.");
			Assert.AreEqual(2, result.Count, "Invalid number of results loaded.");

			for (var i = 1; i <= 2; i++)
				subkey.Verify(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + i));
		}

		[TestMethod]
		public void LoadTest()
		{
			const string json = "{\"ProcessId\":7024,\"ProcessName\":\"test1.exe\",\"ProcessPath\":\"PROCESS-PATH\",\"ProcessUser\":\"TEST1\",\"ServerName\":\"\",\"IsAttached\":false,\"IsLocal\":true,\"Engine\":null}";

			var key = new Mock<IRegistryKey>();
			var subkey = new Mock<IRegistryKey>();
			key.Setup(k => k.OpenSubKey(ReAttachConstants.ReAttachRegistryKeyName)).Returns(subkey.Object);

			const int items = 3;

			for (var i = 1; i <= items; i++)
			{
				subkey.Setup(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + i))
					.Returns(json.Replace("PROCESS-PATH", "process" + i));
			}
			var package = new Mock<IReAttachPackage>();
			package.Setup(p => p.OpenUserRegistryRoot()).Returns(key.Object);
			var repository = new ReAttachRegistryRepository(package.Object);

			var result = repository.LoadTargets();
			Assert.IsNotNull(result, "Empty set loaded resulted in null result, should result in empty list.");
			Assert.AreEqual(items, result.Count, "Invalid number of results loaded.");

			for (var i = 0; i < items; i++)
				Assert.AreEqual("process" + (i + 1), result[i].ProcessPath, "Mismatching path found for item " + i);

			key.Verify(k => k.OpenSubKey(It.IsAny<string>()), Times.Once());
			for (var i = 1; i <= items; i++)
				subkey.Verify(k => k.GetValue(ReAttachConstants.ReAttachRegistryHistoryKeyPrefix + i));
		}
	}
}
