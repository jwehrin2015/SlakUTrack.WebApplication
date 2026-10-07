using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SlakUTrack.WebApplication.Data;

namespace SlakUTrack.WebApplication.Services;

public static class AttendanceApi
{
    public static IEndpointRouteBuilder MapAttendanceApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        api.MapGet("/instructors", async (IDbContextFactory<AppDbContext> factory) =>
        {
            await using var context = await factory.CreateDbContextAsync();
            var instructorRows = await context.Instructors
                .AsNoTracking()
                .OrderBy(item => item.LastName)
                .ThenBy(item => item.FirstName)
                .Select(item => new { item.InstructorId, item.FirstName, item.LastName })
                .ToListAsync();
            var instructors = instructorRows.Select(item => new
            {
                id = item.InstructorId,
                label = string.Join(" ", new[] { item.FirstName, item.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
            });
            return Results.Ok(instructors);
        });

        api.MapGet("/instructors/{instructorId}/courses", async (
            string instructorId,
            IDbContextFactory<AppDbContext> factory) =>
        {
            await using var context = await factory.CreateDbContextAsync();
            var courses = await context.Courses
                .AsNoTracking()
                .Where(item => item.InstructorId == instructorId)
                .OrderBy(item => item.CourseName)
                .Select(item => new { id = item.CourseId, label = item.CourseName })
                .ToListAsync();
            return Results.Ok(courses);
        });

        api.MapGet("/courses/{courseId}/classes", async (
            string courseId,
            IDbContextFactory<AppDbContext> factory) =>
        {
            await using var context = await factory.CreateDbContextAsync();
            var classRows = await context.Classes
                .AsNoTracking()
                .Where(item => item.CourseId == courseId)
                .OrderByDescending(item => item.ClassDate)
                .ThenByDescending(item => item.ClassId)
                .Select(item => new { item.ClassId, item.ClassDate })
                .ToListAsync();
            var classes = classRows.Select(item => new
            {
                id = item.ClassId,
                date = item.ClassDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            });
            return Results.Ok(classes);
        });

        api.MapGet("/locations", async (IDbContextFactory<AppDbContext> factory) =>
        {
            await using var context = await factory.CreateDbContextAsync();
            var locations = await context.Locations
                .AsNoTracking()
                .OrderBy(item => item.ClassRoom)
                .Select(item => new { id = item.ClassRoom, label = item.ClassRoom })
                .ToListAsync();
            return Results.Ok(locations);
        });

        api.MapGet("/reports", async (
            string courseId,
            int classId,
            IDbContextFactory<AppDbContext> factory) =>
        {
            await using var context = await factory.CreateDbContextAsync();
            var classInfo = await context.Classes
                .AsNoTracking()
                .Where(item => item.ClassId == classId && item.CourseId == courseId)
                .Select(item => new
                {
                    item.CourseId,
                    item.Course.CourseName,
                    item.Instructor.FirstName,
                    item.Instructor.LastName,
                    item.ClassDate,
                })
                .SingleOrDefaultAsync();

            if (classInfo is null)
            {
                return Results.NotFound(new { error = "The selected class could not be found for this course." });
            }

            var studentIds = await context.RegisteredClasses
                .AsNoTracking()
                .Where(item => item.CourseId == courseId)
                .Select(item => item.StudentId)
                .ToListAsync();
            var students = await context.Students
                .AsNoTracking()
                .Where(item => studentIds.Contains(item.StudentId))
                .Select(item => new { item.StudentId, item.FirstName, item.LastName })
                .ToListAsync();
            var attendanceRecords = await context.Attendance
                .AsNoTracking()
                .Where(item => item.ClassId == classId && studentIds.Contains(item.StudentId))
                .OrderBy(item => item.AttendanceId)
                .ToListAsync();
            var attendanceByStudent = attendanceRecords
                .GroupBy(item => item.StudentId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var rows = students
                .Select(student =>
                {
                    attendanceByStudent.TryGetValue(student.StudentId, out var records);
                    var latestTimeIn = records?
                        .Where(item => item.TimeIn.HasValue)
                        .OrderByDescending(item => item.TimeIn)
                        .Select(item => item.TimeIn)
                        .FirstOrDefault();
                    var firstRecord = records?.FirstOrDefault();
                    var name = string.Join(" ", new[] { student.FirstName, student.LastName }
                        .Where(part => !string.IsNullOrWhiteSpace(part)));

                    return new
                    {
                        name,
                        attended = firstRecord?.Attended ?? "",
                        timeIn = latestTimeIn?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) ?? "",
                        lastDateAttended = records is { Count: > 0 }
                            ? classInfo.ClassDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : "",
                    };
                })
                .OrderBy(item => item.name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return Results.Ok(new
            {
                @class = new
                {
                    courseId = classInfo.CourseId,
                    courseName = classInfo.CourseName,
                    professor = string.Join(" ", new[] { classInfo.FirstName, classInfo.LastName }
                        .Where(part => !string.IsNullOrWhiteSpace(part))),
                    date = classInfo.ClassDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                rows,
            });
        });

        api.MapPost("/classes/recurring", async (
            ScheduleRequest request,
            IDbContextFactory<AppDbContext> factory) =>
        {
            if (request.EndDate < request.StartDate)
            {
                return Results.BadRequest(new { error = "The end date must be on or after the start date." });
            }

            if (request.EndTime <= request.StartTime)
            {
                return Results.BadRequest(new { error = "The end time must be later than the start time." });
            }

            var days = new HashSet<DayOfWeek>();
            foreach (var dayName in request.DaysOfWeek ?? [])
            {
                if (!Enum.TryParse<DayOfWeek>(dayName, true, out var day) ||
                    day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    return Results.BadRequest(new { error = "Select one or more weekdays from Monday to Friday." });
                }

                days.Add(day);
            }

            if (days.Count == 0)
            {
                return Results.BadRequest(new { error = "Select at least one weekday." });
            }

            var dates = new List<DateTime>();
            for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            {
                if (days.Contains(date.DayOfWeek))
                {
                    dates.Add(date.ToDateTime(TimeOnly.MinValue));
                }
            }

            if (dates.Count == 0)
            {
                return Results.BadRequest(new { error = "There are no dates matching the selected weekdays in this range." });
            }

            await using var context = await factory.CreateDbContextAsync();
            var validSelection = await context.Courses.AnyAsync(item =>
                item.CourseId == request.CourseId && item.InstructorId == request.InstructorId);
            var validLocation = await context.Locations.AnyAsync(item => item.ClassRoom == request.LocationId);
            if (!validSelection || !validLocation)
            {
                return Results.BadRequest(new { error = "Select a valid professor, course, and class room." });
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Classes.AddRange(dates.Select(date => new ClassEntity
            {
                ClassDate = date,
                ClassRoom = request.LocationId,
                StartTime = request.StartTime.ToTimeSpan(),
                EndTime = request.EndTime.ToTimeSpan(),
                InstructorId = request.InstructorId,
                CourseId = request.CourseId,
            }));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Results.Ok(new { count = dates.Count });
        });

        api.MapGet("/settings", (DatabaseSettings settings) => Results.Ok(new
        {
            databasePath = settings.Path,
            provider = "SQLite",
        }));

        return endpoints;
    }

    public sealed record ScheduleRequest(
        string InstructorId,
        string LocationId,
        string CourseId,
        TimeOnly StartTime,
        TimeOnly EndTime,
        DateOnly StartDate,
        DateOnly EndDate,
        string[]? DaysOfWeek);
}
