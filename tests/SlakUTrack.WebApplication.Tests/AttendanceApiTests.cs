using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SlakUTrack.WebApplication.Tests;

public sealed class AttendanceApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SeededCourseProducesAttendanceReport()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        var instructors = await client.GetFromJsonAsync<JsonElement[]>("/api/instructors", JsonOptions);
        Assert.NotNull(instructors);
        Assert.Contains(instructors, instructor =>
            instructor.GetProperty("id").GetString() == "DEMO-I-001" &&
            instructor.GetProperty("label").GetString() == "Demo Instructor");

        var courses = await client.GetFromJsonAsync<JsonElement[]>(
            "/api/instructors/DEMO-I-001/courses",
            JsonOptions);
        Assert.NotNull(courses);
        Assert.Contains(courses, course => course.GetProperty("id").GetString() == "DEMO-C-001");

        var response = await client.GetAsync("/api/reports?courseId=DEMO-C-001&classId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var reportClass = report.RootElement.GetProperty("class");
        Assert.Equal("DEMO-C-001", reportClass.GetProperty("courseId").GetString());
        Assert.Equal("DEMO - Introduction to Attendance Tracking", reportClass.GetProperty("courseName").GetString());
        Assert.Equal("Demo Instructor", reportClass.GetProperty("professor").GetString());
        Assert.Equal("2026-10-05", reportClass.GetProperty("date").GetString());

        var rows = report.RootElement.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(3, rows.Length);
        Assert.Equal("Alex Sample", rows[0].GetProperty("name").GetString());
        Assert.Equal("Yes", rows[0].GetProperty("attended").GetString());
        Assert.Equal("08:57:00", rows[0].GetProperty("timeIn").GetString());
        Assert.Equal("2026-10-05", rows[0].GetProperty("lastDateAttended").GetString());
        Assert.Equal("Taylor Demo", rows[2].GetProperty("name").GetString());
        Assert.Equal("No", rows[2].GetProperty("attended").GetString());
        Assert.Equal("", rows[2].GetProperty("timeIn").GetString());
    }

    [Fact]
    public async Task RecurringScheduleCreatesReportableClassesForSelectedWeekdays()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/classes/recurring", new
        {
            instructorId = "DEMO-I-001",
            locationId = "Demo Room A",
            courseId = "DEMO-C-001",
            startTime = "09:00",
            endTime = "10:00",
            startDate = "2026-10-12",
            endDate = "2026-10-16",
            daysOfWeek = new[] { "Monday", "Wednesday" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2, created.GetProperty("count").GetInt32());

        var classes = await client.GetFromJsonAsync<JsonElement[]>("/api/courses/DEMO-C-001/classes", JsonOptions);
        Assert.NotNull(classes);
        Assert.Equal("2026-10-14", classes[0].GetProperty("date").GetString());
        Assert.Equal("2026-10-12", classes[1].GetProperty("date").GetString());

        var reportResponse = await client.GetAsync(
            $"/api/reports?courseId=DEMO-C-001&classId={classes[0].GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        using var report = JsonDocument.Parse(await reportResponse.Content.ReadAsStringAsync());
        Assert.Equal("2026-10-14", report.RootElement.GetProperty("class").GetProperty("date").GetString());
        Assert.Equal(3, report.RootElement.GetProperty("rows").GetArrayLength());
        Assert.Equal("", report.RootElement.GetProperty("rows")[0].GetProperty("attended").GetString());
    }

    [Theory]
    [InlineData("end-time")]
    [InlineData("no-weekdays")]
    [InlineData("reversed-range")]
    [InlineData("no-matching-dates")]
    [InlineData("invalid-selection")]
    public async Task InvalidScheduleIsRejectedWithoutCreatingClasses(string invalidInput)
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient();
        var request = new Dictionary<string, object>
        {
            ["instructorId"] = "DEMO-I-001",
            ["locationId"] = "Demo Room A",
            ["courseId"] = "DEMO-C-001",
            ["startTime"] = "09:00",
            ["endTime"] = "10:00",
            ["startDate"] = "2026-10-12",
            ["endDate"] = "2026-10-16",
            ["daysOfWeek"] = new[] { "Monday" },
        };

        switch (invalidInput)
        {
            case "end-time":
                request["endTime"] = "09:00";
                break;
            case "no-weekdays":
                request["daysOfWeek"] = Array.Empty<string>();
                break;
            case "reversed-range":
                request["startDate"] = "2026-10-16";
                request["endDate"] = "2026-10-12";
                break;
            case "no-matching-dates":
                request["startDate"] = "2026-10-17";
                request["endDate"] = "2026-10-18";
                break;
            case "invalid-selection":
                request["courseId"] = "NOT-A-COURSE";
                break;
        }

        var response = await client.PostAsJsonAsync("/api/classes/recurring", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("error").GetString()));

        var classes = await client.GetFromJsonAsync<JsonElement[]>("/api/courses/DEMO-C-001/classes", JsonOptions);
        Assert.NotNull(classes);
        Assert.Single(classes);
    }

    [Fact]
    public async Task ReportRejectsClassFromAnotherCourse()
    {
        using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports?courseId=NOT-A-COURSE&classId=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
