using System.Net;
using System.Net.Http.Json;
using MyStudents.Application.Students.Dto;

namespace MyStudents.AutomationTests;

public class OwnershipApiTests
{
    [Fact]
    public async Task TeachersCannotReadModifyOrDeleteAnotherTeachersStudent()
    {
        using var factory = new ApiFactory();
        using var client = await factory.InitializeAsync();
        var owner = await ApiFactory.RegisterAsync(client);
        var other = await ApiFactory.RegisterAsync(client);
        ApiFactory.Authenticate(client, owner);
        var create = await client.PostAsJsonAsync("/api/v1/students", new { firstName = "An", lastName = "Nguyen", gender = 1 });
        create.EnsureSuccessStatusCode();
        var id = await create.Content.ReadFromJsonAsync<Guid>();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/students/{id}")).StatusCode);
        ApiFactory.Authenticate(client, other);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/students/{id}")).StatusCode);
        var list = await client.GetFromJsonAsync<List<StudentDto>>("/api/v1/students");
        Assert.DoesNotContain(list!, s => s.Id == id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/students/{id}", new { id, firstName = "Changed", lastName = "Name", gender = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/students/{id}")).StatusCode);
        ApiFactory.Authenticate(client, owner);
        var student = await client.GetFromJsonAsync<StudentDto>($"/api/v1/students/{id}");
        Assert.Equal("An", student!.FirstName);
        (await client.DeleteAsync($"/api/v1/students/{id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/students/{id}")).StatusCode);
    }
}
