using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class CommonEnumerationsTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddCommonEnumeration()
    {
        var item = new CommonEnumerationDataContract()
        {
            Name = "Animals",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Dog" },
                { "2", "Cat" },
                { "3", "Fish" },
                { "4", "Rabbit" }
            }
        };

        await AddCommonEnumeration(item);
        var allItems = await GetAllCommonEnumerations();

        Assert.That(allItems.Count(), Is.EqualTo(1));
        var result = allItems.Single();
        Assert.That(result.Name, Is.EqualTo(item.Name));
        CollectionAssert.AreEquivalent(item.Enumeration, result.Enumeration);
    }

    [Test]
    public async Task ShallGetMultipleItems()
    {
        var animals = new CommonEnumerationDataContract()
        {
            Name = "Animals",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Dog" },
                { "2", "Cat" },
            }
        };

        await AddCommonEnumeration(animals);

        var weather = new CommonEnumerationDataContract()
        {
            Name = "Weather",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Sunny" },
                { "2", "Rain" },
            }
        };

        await AddCommonEnumeration(weather);
        
        var allItems = (await GetAllCommonEnumerations()).ToList();

        Assert.That(allItems.Count, Is.EqualTo(2));
        Assert.That(allItems[0].Name, Is.EqualTo(animals.Name));
        Assert.That(allItems[1].Name, Is.EqualTo(weather.Name));
        CollectionAssert.AreEquivalent(allItems[0].Enumeration, animals.Enumeration);
        CollectionAssert.AreEquivalent(allItems[1].Enumeration, weather.Enumeration);
    }

    [Test]
    public async Task ShallUpdateCommonEnumeration()
    {
        var item = new CommonEnumerationDataContract()
        {
            Name = "Animals",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Dog" },
                { "2", "Cat" },
                { "3", "Fish" },
                { "4", "Rabbit" }
            }
        };

        await AddCommonEnumeration(item);
        var allItems = await GetAllCommonEnumerations();

        var updated = allItems.Single();

        updated.Name = "More Animals";
        updated.Enumeration.Add("5", "Snake");

        await UpdateCommonEnumeration(updated);

        Assert.That(allItems.Count(), Is.EqualTo(1));
        var result = allItems.Single();
        Assert.That(result.Name, Is.EqualTo(updated.Name));
        CollectionAssert.AreEquivalent(updated.Enumeration, result.Enumeration);
    }

    [Test]
    public async Task ShallDeleteCommonEnumeration()
    {
        var animals = new CommonEnumerationDataContract()
        {
            Name = "Animals",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Dog" },
                { "2", "Cat" },
            }
        };

        await AddCommonEnumeration(animals);

        var weather = new CommonEnumerationDataContract()
        {
            Name = "Weather",
            Enumeration = new Dictionary<string, string>()
            {
                { "1", "Sunny" },
                { "2", "Rain" },
            }
        };

        await AddCommonEnumeration(weather);

        var allItems = await GetAllCommonEnumerations();

        await DeleteCommonEnumeration(allItems.First().Id);

        var allItems2 = await GetAllCommonEnumerations();

        Assert.That(allItems2.Count(), Is.EqualTo(1));
    }
}