using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Infrastructure.Persistence;
using MondaySharp.NET.Infrastructure.Utilities;
using System.Text.Json;

namespace MondaySharp.Functional.Tests;

[TestClass]
public class UnitTest1
{
    MondayClient? MondayClient { get; set; }
    ILogger<MondayClient>? Logger { get; set; }

    ulong BoardId { get; set; }

    [TestInitialize]
    public void Init()
    {
        // Load appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        this.Logger = new LoggerFactory().CreateLogger<MondayClient>();
        MondayClient = new MondayClient(this.Logger, options =>
        {
            options.EndPoint = new System.Uri(configuration["mondayUrl"]!);
            options.Token = configuration["mondayToken"]!;
        });

        this.BoardId = ulong.Parse(configuration["boardId"]!);
    }

    [TestMethod]
    public async Task GetItemsByColumnValues_Should_Be_Ok()
    {
        // Arrange
        ColumnValue[] columnValues =
        [
            new ColumnValue()
            {
                Id = "text0",
                Text = "123"
            },
            new ColumnValue()
            {
                Id = "numbers9",
                Text = "1"
            },
        ];

        // Act
        List<TestRow?> items = await this.MondayClient!.GetBoardItemsAsync<TestRow>(this.BoardId, columnValues).ToListAsync();

        // Assert
        Assert.IsTrue(items.Count > 0);
    }

    [TestMethod]
    public async Task GetItems_Should_Be_Ok()
    {
        // Arrange
        // Act
        List<TestRow?> items = await this.MondayClient!.GetBoardItemsAsync<TestRow>(this.BoardId).ToListAsync();

        // Assert
        Assert.IsTrue(items.Count > 0);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithGroup_Should_Be_Ok()
    {
        // Arrange
        ColumnValue[] columnValues =
        [
            new ColumnValue()
            {
                Id = "text0",
                Text = "123"
            },
            new ColumnValue()
            {
                Id = "numbers9",
                Text = "1"
            },
        ];

        // Act
        List<TestRowWithGroup?> items = await this.MondayClient!.GetBoardItemsAsync<TestRowWithGroup>(this.BoardId, columnValues).ToListAsync();

        // Assert
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.FirstOrDefault()?.Group != null);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithAssets_Should_Be_Ok()
    {
        // Arrange
        ColumnValue[] columnValues =
        [
            new ColumnValue()
            {
                Id = "text0",
                Text = "123"
            },
            new ColumnValue()
            {
                Id = "numbers9",
                Text = "1"
            },
        ];

        // Act
        List<TestRowWithAssets?> items = await this.MondayClient!.GetBoardItemsAsync<TestRowWithAssets>(this.BoardId, columnValues).ToListAsync();

        // Assert
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.FirstOrDefault()?.Assets?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithUpdates_Should_Be_Ok()
    {
        // Arrange
        ColumnValue[] columnValues =
        [
            new ColumnValue()
            {
                Id = "text0",
                Text = "123"
            },
            new ColumnValue()
            {
                Id = "numbers9",
                Text = "1"
            },
        ];

        // Act
        List<TestRowWithUpdates?> items = await this.MondayClient!.GetBoardItemsAsync<TestRowWithUpdates>(this.BoardId, columnValues).ToListAsync();

        // Assert
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.FirstOrDefault()?.Updates?.Count > 0);
    }

    [TestMethod]
    public void ConvertColumnValuesToJson_Should_Be_Ok()
    {
        // Arrange
        List<ColumnBaseType> columnValues =
        [
            new ColumnDateTime("date", new DateTime(2023, 11, 29)),
            new ColumnText("text0", "Andrew Eberle"),
            new ColumnNumber("numbers", 10),
            new ColumnLongText("long_text7", "hello,world!"),
            new ColumnStatus("status_19", "Test"),
            new ColumnStatus("label", "Test"),
            new ColumnLongText("long_text", "long text with return \n"),
            new ColumnDropDown("dropdown", ["Hello", "World"]),
            new ColumnLink("link", "https://www.google.com", "google!"),
            new ColumnTag("tags", "21057674,21057675"),
            new ColumnTimeline("timeline", new DateTime(2023, 11, 29), new DateTime(2023, 12, 29)),
        ];

        // Act
        string json = MondayUtilties.ToColumnValuesJson(columnValues);

        // Assert
        Assert.IsTrue(!string.IsNullOrWhiteSpace(json));
        JsonDocument jsonDocument = JsonDocument.Parse(json);

        Assert.IsTrue(jsonDocument.RootElement.EnumerateObject().Count() == 11);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("date").GetProperty("date").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("text0").GetString() == "Andrew Eberle");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("numbers").GetString() == "10");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text7").GetProperty("text").GetString() == "hello,world!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("status_19").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("label").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text").GetProperty("text").GetString() == "long text with return ");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("dropdown").GetProperty("labels").EnumerateArray().Count() == 2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("url").GetString() == "https://www.google.com");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("text").GetString() == "google!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("tags").GetProperty("tag_ids").EnumerateArray().Count() == 2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("from").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("to").GetString() == "2023-12-29");
    }

    [TestMethod]
    public async Task CreateMultipleItemsMutation_Should_Be_Ok()
    {
        // Arrange
        Item[] items =[ 
            new Item()
            {
                Name = "Test Item 1",
                ColumnValues =
                [
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnText()
                        {
                            Id = "text0",
                            Text = "Andrew Eberle"
                        },
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnNumber()
                        {
                            Id = "numbers9",
                            Number = 10
                        },
                    },
                ]
            },
            new Item()
            {
                Name = "Test Item 2",
                ColumnValues =
                [
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnText()
                        {
                            Id = "text0",
                            Text = "Eberle Andrew"
                        },
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnNumber()
                        {
                            Id = "numbers9",
                            Number = 11
                        },
                    },
                ]
            }
        ];

        // Act
        Dictionary<string, Item>? keyValuePairs = await this.MondayClient!.CreateBoardItemsAsync(BoardId, items);

        // Assert
        Assert.IsTrue(keyValuePairs?.Count == 2);
        Assert.IsTrue(keyValuePairs?.FirstOrDefault().Value.Name == items.FirstOrDefault()?.Name);
        Assert.IsTrue(keyValuePairs?.LastOrDefault().Value.Name == items.LastOrDefault()?.Name);
    }

    public record TestRowWithGroup : TestRow
    {
        public Group? Group { get; set; }
    }

    public record TestRowWithAssets : TestRow
    {
        public List<Asset>? Assets { get; set; }
    }

    public record TestRowWithUpdates : TestRow
    {
        public List<Update>? Updates { get; set; }
    }

    public record TestRow : MondayRow
    {
        [MondayColumnHeader("text0")]
        public ColumnText? Text { get; set; }

        [MondayColumnHeader("numbers9")]
        public ColumnNumber? Number { get; set; }

        [MondayColumnHeader("checkbox")]
        public ColumnCheckBox? Checkbox { get; set; }

        [MondayColumnHeader("priority")]
        public ColumnStatus? Priority { get; set; }

        [MondayColumnHeader("status")]
        public ColumnStatus? Status { get; set; }

        [MondayColumnHeader("link2")]
        public ColumnLink? Link { get; set; }

        [MondayColumnHeader("dropdown")]
        public ColumnDropDown? Dropdown { get; set; }

        [MondayColumnHeader("date")]
        public ColumnDateTime? Date { get; set; }

        [MondayColumnHeader("long_text")]
        public ColumnLongText? LongText { get; set; }

        [MondayColumnHeader("color_picker")]
        public ColumnColorPicker? ColorPicker { get; set; }

        [MondayColumnHeader("timeline")]
        public ColumnTimeline? Timeline { get; set; }

        [MondayColumnHeader("tags")]
        public ColumnTag? Tags { get; set; }
    }
}