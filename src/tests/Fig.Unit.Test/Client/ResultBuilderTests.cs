using System;
using System.Collections.Generic;
using Fig.Client.Abstractions.CustomActions;
using NUnit.Framework;

namespace Fig.Unit.Test.Client
{
    [TestFixture]
    public class ResultBuilderTests
    {
        [Test]
        public void CreateSuccessResult_ShouldReturnSucceededModel()
        {
            var result = ResultBuilder.CreateSuccessResult("TestAction");
            Assert.That(result.Name, Is.EqualTo("TestAction"));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void CreateFailureResult_ShouldReturnFailedModel()
        {
            var result = ResultBuilder.CreateFailureResult("TestAction");
            Assert.That(result.Name, Is.EqualTo("TestAction"));
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void WithTextResult_ShouldSetTextResult()
        {
            var model = new CustomActionResultModel("Test", true);
            var result = model.WithTextResult("Some text");
            Assert.That(result.TextResult, Is.EqualTo("Some text"));
        }

        [Test]
        public void WithTextResult_WhenAlreadySet_ShouldThrow()
        {
            var model = new CustomActionResultModel("Test", true) { TextResult = "Existing" };
            Assert.That(() => model.WithTextResult("New text"), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void WithDataGridResult_ShouldSetDataGridResult()
        {
            var model = new CustomActionResultModel("Test", true);
            var data = new List<TestData> { new TestData { Name = "A", Value = 1 } };
            var result = model.WithDataGridResult(data);
            Assert.That(result.DataGridResult, Is.Not.Null);
            Assert.That(result.DataGridResult, Has.Count.EqualTo(1));
            Assert.That(result.DataGridResult![0]["Name"], Is.EqualTo("A"));
            Assert.That(result.DataGridResult![0]["Value"], Is.EqualTo("1"));
        }

        [Test]
        public void WithDataGridResult_WhenAlreadySet_ShouldThrow()
        {
            var model = new CustomActionResultModel("Test", true) { DataGridResult = new List<Dictionary<string, object?>>() };
            var data = new List<TestData> { new TestData { Name = "A", Value = 1 } };
            Assert.That(() => model.WithDataGridResult(data), Throws.TypeOf<InvalidOperationException>());
        }
        
        private class TestData
        {
            public string Name { get; set; } = string.Empty;
            
            public int Value { get; set; }
        }
    }
}
