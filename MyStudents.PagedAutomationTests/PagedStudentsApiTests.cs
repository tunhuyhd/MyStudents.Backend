using System.Net.Http.Json;
using MyStudents.Application.Students.Dto;
using MyStudents.AutomationTests;

namespace MyStudents.PagedAutomationTests;

public class PagedStudentsApiTests
{
    [Fact]
    public async Task PagedListFiltersBySearchAndStatusAndReturnsPageMetadata()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        var teacher = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, teacher);

        var ids = new List<Guid>();
        for (var index = 0; index < 5; index++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/students", new
            {
                firstName = $"PagingUnique{index}",
                lastName = "Family",
                gender = 1
            });
            response.EnsureSuccessStatusCode();
            ids.Add((await response.Content.ReadFromJsonAsync<Guid>()));
        }

        var firstPage = await client.GetFromJsonAsync<PageResult>("/api/v1/students/paged?pageNumber=1&pageSize=2");
        Assert.NotNull(firstPage);
        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.False(firstPage.HasPreviousPage);
        Assert.True(firstPage.HasNextPage);

        var secondPage = await client.GetFromJsonAsync<PageResult>("/api/v1/students/paged?pageNumber=2&pageSize=2");
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Empty(firstPage.Items.Select(student => student.Id).Intersect(secondPage.Items.Select(student => student.Id)));

        var search = await client.GetFromJsonAsync<PageResult>("/api/v1/students/paged?pageNumber=1&pageSize=12&searchTerm=PagingUnique3");
        Assert.NotNull(search);
        Assert.Equal(1, search.TotalCount);
        Assert.Equal("PagingUnique3", Assert.Single(search.Items).FirstName);

        var statusUpdate = await client.PatchAsJsonAsync($"/api/v1/students/{ids[0]}/status", new { id = ids[0], status = 2 });
        statusUpdate.EnsureSuccessStatusCode();
        var inactive = await client.GetFromJsonAsync<PageResult>("/api/v1/students/paged?pageNumber=1&pageSize=12&status=2");
        Assert.NotNull(inactive);
        Assert.Equal(1, inactive.TotalCount);
        Assert.Equal(ids[0], Assert.Single(inactive.Items).Id);
    }

    private sealed record PageResult(
        List<StudentDto> Items,
        int PageNumber,
        int TotalPages,
        int TotalCount,
        int PageSize,
        bool HasNextPage,
        bool HasPreviousPage);
}