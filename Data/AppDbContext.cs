using Microsoft.EntityFrameworkCore;

namespace SlakUTrack.WebApplication.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<InstructorEntity> Instructors => Set<InstructorEntity>();
    public DbSet<CourseEntity> Courses => Set<CourseEntity>();
    public DbSet<ClassEntity> Classes => Set<ClassEntity>();
    public DbSet<LocationEntity> Locations => Set<LocationEntity>();
    public DbSet<StudentEntity> Students => Set<StudentEntity>();
    public DbSet<RegisteredClassEntity> RegisteredClasses => Set<RegisteredClassEntity>();
    public DbSet<AttendanceEntity> Attendance => Set<AttendanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstructorEntity>(entity =>
        {
            entity.ToTable("Instructor");
            entity.HasKey(item => item.InstructorId);
            entity.Property(item => item.InstructorId).HasColumnName("InstructorID");
            entity.Property(item => item.FirstName).IsRequired();
            entity.Property(item => item.LastName).IsRequired();
            entity.HasData(new InstructorEntity
            {
                InstructorId = "DEMO-I-001",
                FirstName = "Demo",
                LastName = "Instructor",
            });
        });

        modelBuilder.Entity<CourseEntity>(entity =>
        {
            entity.ToTable("Course");
            entity.HasKey(item => item.CourseId);
            entity.Property(item => item.CourseId).HasColumnName("CourseID");
            entity.Property(item => item.CourseName).IsRequired();
            entity.Property(item => item.InstructorId).HasColumnName("InstructorID");
            entity.HasOne(item => item.Instructor)
                .WithMany(item => item.Courses)
                .HasForeignKey(item => item.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new CourseEntity
            {
                CourseId = "DEMO-C-001",
                CourseName = "DEMO - Introduction to Attendance Tracking",
                InstructorId = "DEMO-I-001",
            });
        });

        modelBuilder.Entity<ClassEntity>(entity =>
        {
            entity.ToTable("Class");
            entity.HasKey(item => item.ClassId);
            entity.Property(item => item.ClassId).HasColumnName("ClassID").ValueGeneratedOnAdd();
            entity.Property(item => item.ClassDate).HasColumnName("ClassDate").HasColumnType("TEXT");
            entity.Property(item => item.ClassRoom).HasColumnName("ClassRoom");
            entity.Property(item => item.StartTime).HasColumnName("StartTime").HasColumnType("TEXT");
            entity.Property(item => item.EndTime).HasColumnName("EndTime").HasColumnType("TEXT");
            entity.Property(item => item.InstructorId).HasColumnName("InstructorID");
            entity.Property(item => item.CourseId).HasColumnName("CourseID");
            entity.HasOne(item => item.Instructor)
                .WithMany(item => item.Classes)
                .HasForeignKey(item => item.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Course)
                .WithMany(item => item.Classes)
                .HasForeignKey(item => item.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Location)
                .WithMany(item => item.Classes)
                .HasForeignKey(item => item.ClassRoom)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new ClassEntity
            {
                ClassId = 1,
                ClassDate = new DateTime(2026, 10, 5),
                ClassRoom = "Demo Room A",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                InstructorId = "DEMO-I-001",
                CourseId = "DEMO-C-001",
            });
        });

        modelBuilder.Entity<LocationEntity>(entity =>
        {
            entity.ToTable("Location");
            entity.HasKey(item => item.ClassRoom);
            entity.Property(item => item.ClassRoom).HasColumnName("ClassRoom");
            entity.HasData(new LocationEntity { ClassRoom = "Demo Room A" });
        });

        modelBuilder.Entity<StudentEntity>(entity =>
        {
            entity.ToTable("Student");
            entity.HasKey(item => item.StudentId);
            entity.Property(item => item.StudentId).HasColumnName("StudentID");
            entity.Property(item => item.FirstName).IsRequired();
            entity.Property(item => item.LastName).IsRequired();
            entity.HasData(
                new StudentEntity { StudentId = "DEMO-S-001", FirstName = "Alex", LastName = "Sample" },
                new StudentEntity { StudentId = "DEMO-S-002", FirstName = "Jordan", LastName = "Example" },
                new StudentEntity { StudentId = "DEMO-S-003", FirstName = "Taylor", LastName = "Demo" });
        });

        modelBuilder.Entity<RegisteredClassEntity>(entity =>
        {
            entity.ToTable("RegisteredClasses");
            entity.HasKey(item => new { item.StudentId, item.CourseId });
            entity.Property(item => item.StudentId).HasColumnName("StudentID");
            entity.Property(item => item.CourseId).HasColumnName("CourseID");
            entity.HasOne(item => item.Student)
                .WithMany(item => item.Registrations)
                .HasForeignKey(item => item.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Course)
                .WithMany(item => item.Registrations)
                .HasForeignKey(item => item.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new RegisteredClassEntity { StudentId = "DEMO-S-001", CourseId = "DEMO-C-001" },
                new RegisteredClassEntity { StudentId = "DEMO-S-002", CourseId = "DEMO-C-001" },
                new RegisteredClassEntity { StudentId = "DEMO-S-003", CourseId = "DEMO-C-001" });
        });

        modelBuilder.Entity<AttendanceEntity>(entity =>
        {
            entity.ToTable("Attendance");
            entity.HasKey(item => item.AttendanceId);
            entity.Property(item => item.AttendanceId).HasColumnName("AttendanceID").ValueGeneratedOnAdd();
            entity.Property(item => item.StudentId).HasColumnName("StudentID");
            entity.Property(item => item.ClassId).HasColumnName("ClassID");
            entity.Property(item => item.Attended);
            entity.Property(item => item.TimeIn).HasColumnType("TEXT");
            entity.HasOne(item => item.Student)
                .WithMany(item => item.AttendanceRecords)
                .HasForeignKey(item => item.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Class)
                .WithMany(item => item.AttendanceRecords)
                .HasForeignKey(item => item.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new AttendanceEntity
                {
                    AttendanceId = 1,
                    StudentId = "DEMO-S-001",
                    ClassId = 1,
                    Attended = "Yes",
                    TimeIn = new TimeSpan(8, 57, 0),
                },
                new AttendanceEntity
                {
                    AttendanceId = 2,
                    StudentId = "DEMO-S-002",
                    ClassId = 1,
                    Attended = "Yes",
                    TimeIn = new TimeSpan(9, 3, 0),
                },
                new AttendanceEntity
                {
                    AttendanceId = 3,
                    StudentId = "DEMO-S-003",
                    ClassId = 1,
                    Attended = "No",
                });
        });
    }
}
