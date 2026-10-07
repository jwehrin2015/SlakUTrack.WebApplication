namespace SlakUTrack.WebApplication.Data;

public sealed class InstructorEntity
{
    public string InstructorId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public ICollection<CourseEntity> Courses { get; } = new List<CourseEntity>();
    public ICollection<ClassEntity> Classes { get; } = new List<ClassEntity>();
}

public sealed class CourseEntity
{
    public string CourseId { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string InstructorId { get; set; } = "";
    public InstructorEntity Instructor { get; set; } = null!;
    public ICollection<ClassEntity> Classes { get; } = new List<ClassEntity>();
    public ICollection<RegisteredClassEntity> Registrations { get; } = new List<RegisteredClassEntity>();
}

public sealed class ClassEntity
{
    public int ClassId { get; set; }
    public DateTime ClassDate { get; set; }
    public string ClassRoom { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string InstructorId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public InstructorEntity Instructor { get; set; } = null!;
    public CourseEntity Course { get; set; } = null!;
    public LocationEntity Location { get; set; } = null!;
    public ICollection<AttendanceEntity> AttendanceRecords { get; } = new List<AttendanceEntity>();
}

public sealed class LocationEntity
{
    public string ClassRoom { get; set; } = "";
    public ICollection<ClassEntity> Classes { get; } = new List<ClassEntity>();
}

public sealed class StudentEntity
{
    public string StudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public ICollection<RegisteredClassEntity> Registrations { get; } = new List<RegisteredClassEntity>();
    public ICollection<AttendanceEntity> AttendanceRecords { get; } = new List<AttendanceEntity>();
}

public sealed class RegisteredClassEntity
{
    public string StudentId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public StudentEntity Student { get; set; } = null!;
    public CourseEntity Course { get; set; } = null!;
}

public sealed class AttendanceEntity
{
    public int AttendanceId { get; set; }
    public string StudentId { get; set; } = "";
    public int ClassId { get; set; }
    public string? Attended { get; set; }
    public TimeSpan? TimeIn { get; set; }
    public StudentEntity Student { get; set; } = null!;
    public ClassEntity Class { get; set; } = null!;
}
