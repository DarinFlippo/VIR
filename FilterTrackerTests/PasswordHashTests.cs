using Microsoft.VisualStudio.TestTools.UnitTesting;
using FilterTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterTracker.Tests
{
    [TestClass()]
    public class PasswordHashTests
    {
        [TestMethod()]
        public void PasswordHashTest()
        {
            string expected = "password#1";

            var target = new PasswordHash(expected);

            bool result = target.Verify(expected);

            Assert.IsTrue(result);

        }

        [TestMethod()]
        public void PasswordHashTest_2()
        {
            string expected = "password#1";
            var target = new PasswordHash(expected);

            bool result = target.Verify("passworD#1");
            Assert.IsFalse(result);

            result = target.Verify("password#111111111111");
            Assert.IsFalse(result);

            result = target.Verify("password#111111111111111");
            Assert.IsFalse(result);

            result = target.Verify("");
            Assert.IsFalse(result);
        }

    }
}