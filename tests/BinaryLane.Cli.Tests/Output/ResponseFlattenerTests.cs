using System.Text.Json;
using BinaryLane.Cli.Infrastructure.Output;
using FluentAssertions;

namespace BinaryLane.Cli.Tests.Output;

public class ResponseFlattenerTests
{
    [Fact]
    public void ExtractPrimary_UnwrapsResponseWrapper()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            {
                "servers": [{"id": 1, "name": "web1"}],
                "meta": {"total": 1},
                "links": {}
            }
            """);

        var primary = ResponseFlattener.ExtractPrimary(json);
        primary.ValueKind.Should().Be(JsonValueKind.Array);
        primary.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void ExtractPrimary_ReturnsSingleObject()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            {
                "account": {"email": "test@example.com"},
                "links": {}
            }
            """);

        var primary = ResponseFlattener.ExtractPrimary(json);
        primary.ValueKind.Should().Be(JsonValueKind.Object);
        primary.GetProperty("email").GetString().Should().Be("test@example.com");
    }

    [Fact]
    public void FlattenList_SelectsFields()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            [
                {"id": 1, "name": "web1", "status": "active"},
                {"id": 2, "name": "web2", "status": "off"}
            ]
            """);

        var (columns, rows) = ResponseFlattener.FlattenList(json, ["id", "name"]);
        columns.Should().Equal("id", "name");
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("1", "web1");
        rows[1].Should().Equal("2", "web2");
    }

    [Fact]
    public void FlattenValue_BoolToYesNo()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            [{"enabled": true, "disabled": false}]
            """);

        var (_, rows) = ResponseFlattener.FlattenList(json, ["enabled", "disabled"]);
        rows[0].Should().Equal("Yes", "No");
    }

    [Fact]
    public void FlattenObject_ExtractsAllProperties()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            {"id": 1, "name": "test", "active": true}
            """);

        var result = ResponseFlattener.FlattenObject(json);
        result.Should().ContainKey("id").WhoseValue.Should().Be("1");
        result.Should().ContainKey("name").WhoseValue.Should().Be("test");
        result.Should().ContainKey("active").WhoseValue.Should().Be("Yes");
    }

    [Fact]
    public void GetAvailableFields_ReturnsFieldNames()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""
            [{"id": 1, "name": "test", "status": "active"}]
            """);

        var fields = ResponseFlattener.GetAvailableFields(json);
        fields.Should().Equal("id", "name", "status");
    }
}
