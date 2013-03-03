﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ReAttach.Contracts;
using ReAttach.Data;

namespace ReAttach.Tests.UnitTests
{
	[TestClass]
	public class ReAttachHistoryTests
	{
		[TestMethod]
		public void SaveTest()
		{
			var repository = new Mock<IReAttachRepository>(MockBehavior.Strict);
			var history = new ReAttachHistory(repository.Object);
			Assert.IsNotNull(history.Items);

			repository.Setup(r => r.Save(It.IsAny<ReAttachTargetList>())).Returns(true);
			Assert.IsTrue(history.Save());

			repository.Setup(r => r.Save(It.IsAny<ReAttachTargetList>())).Returns(false);
			Assert.IsFalse(history.Save());

			repository.Verify(r => r.Save(It.IsAny<ReAttachTargetList>()), Times.Exactly(2));
		}

		[TestMethod]
		public void LoadTest()
		{
			var repository = new Mock<IReAttachRepository>(MockBehavior.Strict);
			var history = new ReAttachHistory(repository.Object);
			Assert.IsNotNull(history.Items);

			repository.Setup(r => r.Load()).Returns<ReAttachTargetList>(null);
			Assert.IsFalse(history.Load());
			Assert.IsNotNull(history.Items);

			repository.Setup(r => r.Load()).Returns(new ReAttachTargetList(ReAttachConstants.ReAttachHistorySize));
			Assert.IsTrue(history.Load());
			Assert.IsNotNull(history.Items);
			Assert.AreEqual(0, history.Items.Count);

			repository.Verify(r => r.Load(), Times.Exactly(2));
		}
	}
}
