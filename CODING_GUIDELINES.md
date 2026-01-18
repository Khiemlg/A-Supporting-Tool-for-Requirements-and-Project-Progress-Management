# 📚 CODING GUIDELINES & PROJECT STANDARDS
## Dự án: SWP391 Project Management Tool (Jira + GitHub Integration)

> **Stack:** C# .NET (Backend) + React (Frontend)

---

## 📁 1. CẤU TRÚC DỰ ÁN (Project Structure)

### Backend (.NET)
```
/src
├── API/                          # Controllers, Middleware, Filters
│   ├── Controllers/              # API Endpoints
│   ├── Middleware/               # Custom middleware
│   └── Filters/                  # Action filters
├── Application/                  # Business Logic Layer
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Interfaces/               # Service interfaces
│   ├── Services/                 # Service implementations
│   ├── Validators/               # FluentValidation
│   └── Mappings/                 # AutoMapper profiles
├── Domain/                       # Core Business Entities
│   ├── Entities/                 # Domain models
│   ├── Enums/                    # Enumerations
│   └── Common/                   # Base classes
├── Infrastructure/               # External Services
│   ├── Data/                     # DbContext, Migrations
│   ├── Repositories/             # Repository implementations
│   ├── ExternalServices/         # Jira API, GitHub API clients
│   └── Identity/                 # Authentication
└── Shared/                       # Shared utilities
    ├── Constants/                # App constants
    ├── Extensions/               # Extension methods
    └── Helpers/                  # Helper utilities
```

### Frontend (React)
```
/frontend
├── public/
├── src/
│   ├── api/                      # API client & endpoints
│   ├── assets/                   # Images, fonts, icons
│   ├── components/               # Reusable components
│   │   ├── common/               # Button, Input, Modal...
│   │   ├── layout/               # Header, Sidebar, Footer
│   │   └── features/             # Feature-specific components
│   ├── contexts/                 # React Contexts
│   ├── hooks/                    # Custom hooks
│   ├── pages/                    # Page components
│   ├── services/                 # Business logic services
│   ├── store/                    # Redux/Zustand store
│   ├── types/                    # TypeScript types
│   ├── utils/                    # Utility functions
│   └── styles/                   # Global styles
├── .env.example                  # Environment template
└── package.json
```

---

## ✨ 2. CLEAN CODE PRINCIPLES

### 2.1 Naming Conventions

#### C# (.NET)
| Loại | Convention | Ví dụ |
|------|------------|-------|
| Class | PascalCase | `StudentService`, `JiraClient` |
| Interface | I + PascalCase | `IStudentRepository`, `IJiraService` |
| Method | PascalCase | `GetStudentById()`, `SyncJiraIssues()` |
| Variable | camelCase | `studentName`, `totalCommits` |
| Constant | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT`, `API_BASE_URL` |
| Private field | _camelCase | `_dbContext`, `_logger` |
| Property | PascalCase | `StudentId`, `CreatedAt` |
| Parameter | camelCase | `studentId`, `pageNumber` |

#### React/TypeScript
| Loại | Convention | Ví dụ |
|------|------------|-------|
| Component | PascalCase | `StudentList`, `JiraBoard` |
| File component | PascalCase.tsx | `StudentCard.tsx` |
| Hook | useCamelCase | `useAuth`, `useFetchStudents` |
| Constant | UPPER_SNAKE_CASE | `API_URL`, `MAX_PAGE_SIZE` |
| Function | camelCase | `handleSubmit`, `formatDate` |
| Type/Interface | PascalCase | `Student`, `ApiResponse` |
| CSS class | kebab-case | `student-card`, `btn-primary` |

### 2.2 Quy tắc đặt tên có ý nghĩa

```csharp
// ❌ BAD
var d = DateTime.Now;
var s = GetS();
public void Process(int i) { }

// ✅ GOOD
var currentDate = DateTime.Now;
var student = GetStudentById();
public void ProcessStudent(int studentId) { }
```

```typescript
// ❌ BAD
const [d, setD] = useState([]);
const fn = (x) => x * 2;

// ✅ GOOD
const [students, setStudents] = useState<Student[]>([]);
const calculateDoubleValue = (number: number) => number * 2;
```

### 2.3 Single Responsibility - Hàm ngắn gọn

```csharp
// ❌ BAD - Hàm làm quá nhiều việc
public async Task ProcessStudentReport(int studentId)
{
    var student = await _context.Students.FindAsync(studentId);
    var commits = await _githubClient.GetCommits(student.GithubUsername);
    var tasks = await _jiraClient.GetTasks(student.JiraAccountId);
    var report = GenerateHtml(student, commits, tasks);
    await SendEmail(student.Email, report);
    await SaveToDatabase(report);
}

// ✅ GOOD - Chia nhỏ thành các hàm riêng biệt
public async Task ProcessStudentReport(int studentId)
{
    var student = await _studentRepository.GetByIdAsync(studentId);
    var reportData = await _reportDataCollector.CollectAsync(student);
    var report = _reportGenerator.Generate(reportData);
    await _reportPublisher.PublishAsync(report);
}
```

---

## 🏗️ 3. SOLID PRINCIPLES

### 3.1 Single Responsibility Principle (SRP)
> Mỗi class chỉ có MỘT lý do để thay đổi

```csharp
// ❌ BAD - Class làm nhiều việc
public class StudentService
{
    public Student GetStudent(int id) { }
    public void SendEmail(string email, string content) { }
    public string GeneratePdfReport(Student s) { }
    public void LogActivity(string message) { }
}

// ✅ GOOD - Chia thành các class riêng
public class StudentRepository { } // Chỉ xử lý data access
public class EmailService { }      // Chỉ xử lý email
public class ReportGenerator { }   // Chỉ tạo report
public class ActivityLogger { }    // Chỉ logging
```

### 3.2 Open/Closed Principle (OCP)
> Mở rộng được, nhưng không sửa code cũ

```csharp
// ❌ BAD - Phải sửa code khi thêm loại report mới
public class ReportGenerator
{
    public string Generate(string type)
    {
        if (type == "PDF") return GeneratePdf();
        if (type == "Excel") return GenerateExcel();
        // Phải thêm if mới khi có loại report mới
    }
}

// ✅ GOOD - Dùng abstraction
public interface IReportGenerator
{
    string Generate(ReportData data);
}

public class PdfReportGenerator : IReportGenerator { }
public class ExcelReportGenerator : IReportGenerator { }
public class WordReportGenerator : IReportGenerator { } // Dễ dàng thêm mới
```

### 3.3 Liskov Substitution Principle (LSP)
> Class con có thể thay thế class cha mà không gây lỗi

```csharp
// ✅ GOOD
public abstract class User
{
    public abstract Task<IEnumerable<Task>> GetAccessibleTasks();
}

public class TeamMember : User
{
    public override Task<IEnumerable<Task>> GetAccessibleTasks()
        => _taskRepository.GetByAssigneeAsync(this.Id);
}

public class TeamLeader : User
{
    public override Task<IEnumerable<Task>> GetAccessibleTasks()
        => _taskRepository.GetByTeamAsync(this.TeamId);
}
```

### 3.4 Interface Segregation Principle (ISP)
> Interface nhỏ, cụ thể thay vì lớn và chung chung

```csharp
// ❌ BAD - Interface quá lớn
public interface IUserService
{
    void CreateUser();
    void DeleteUser();
    void GenerateReport();
    void SendNotification();
    void ManageTeam();
}

// ✅ GOOD - Chia nhỏ interface
public interface IUserCrudService
{
    Task CreateAsync(User user);
    Task DeleteAsync(int id);
}

public interface IReportService
{
    Task<Report> GenerateAsync(int userId);
}

public interface INotificationService
{
    Task SendAsync(Notification notification);
}
```

### 3.5 Dependency Inversion Principle (DIP)
> Phụ thuộc vào abstraction, không phụ thuộc vào implementation

```csharp
// ❌ BAD - Phụ thuộc trực tiếp vào implementation
public class StudentService
{
    private readonly SqlServerDbContext _context = new();
    private readonly SmtpEmailSender _emailSender = new();
}

// ✅ GOOD - Inject interfaces
public class StudentService
{
    private readonly IDbContext _context;
    private readonly IEmailSender _emailSender;

    public StudentService(IDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }
}
```

---

## 🔧 4. DESIGN PATTERNS SỬ DỤNG

### 4.1 Repository Pattern
```csharp
// Interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Implementation
public class StudentRepository : IRepository<Student>
{
    private readonly AppDbContext _context;
    
    public async Task<Student?> GetByIdAsync(int id)
        => await _context.Students.FindAsync(id);
}
```

### 4.2 Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    ITaskRepository Tasks { get; }
    IGroupRepository Groups { get; }
    Task<int> SaveChangesAsync();
}
```

### 4.3 Dependency Injection (DI)
```csharp
// Program.cs
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IJiraService, JiraService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddHttpClient<IJiraClient, JiraClient>();
```

---

## 🌿 5. GIT WORKFLOW & CONVENTIONS

### 5.1 Branch Naming
```
main              # Production branch
develop           # Development branch
feature/xxx       # New features
bugfix/xxx        # Bug fixes
hotfix/xxx        # Urgent production fixes

Ví dụ:
feature/jira-integration
feature/github-commits-report
bugfix/login-validation
hotfix/security-patch
```

### 5.2 Commit Message Format
```
<type>(<scope>): <subject>

Types:
- feat:     Tính năng mới
- fix:      Sửa bug
- docs:     Thay đổi documentation
- style:    Format code (không ảnh hưởng logic)
- refactor: Refactor code
- test:     Thêm/sửa tests
- chore:    Maintenance tasks

Ví dụ:
feat(auth): add JWT authentication
fix(jira): resolve API timeout issue
docs(readme): update installation guide
refactor(student): extract validation logic
test(github): add unit tests for commit parser
```

### 5.3 Pull Request Rules
- [ ] PR title theo format: `[Type] Brief description`
- [ ] Có description rõ ràng
- [ ] Link đến Jira ticket (nếu có)
- [ ] Self-review trước khi request
- [ ] Tối thiểu 1 reviewer approve
- [ ] All tests passed
- [ ] No merge conflicts

---

## ⚠️ 6. ERROR HANDLING

### Backend
```csharp
// Custom Exception
public class NotFoundException : Exception
{
    public NotFoundException(string entity, int id)
        : base($"{entity} with ID {id} was not found.") { }
}

// Global Exception Handler Middleware
public class ExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}
```

### Frontend
```typescript
// API Error Handler
const handleApiError = (error: AxiosError) => {
  if (error.response?.status === 401) {
    // Redirect to login
    navigate('/login');
  } else if (error.response?.status === 404) {
    toast.error('Resource not found');
  } else {
    toast.error('Something went wrong');
  }
};

// Try-catch trong async functions
const fetchStudents = async () => {
  try {
    setLoading(true);
    const data = await studentApi.getAll();
    setStudents(data);
  } catch (error) {
    handleApiError(error);
  } finally {
    setLoading(false);
  }
};
```

---

## 📝 7. API DESIGN (RESTful)

### Endpoint Naming
```
GET    /api/students           # Lấy danh sách
GET    /api/students/{id}      # Lấy theo ID
POST   /api/students           # Tạo mới
PUT    /api/students/{id}      # Cập nhật toàn bộ
PATCH  /api/students/{id}      # Cập nhật một phần
DELETE /api/students/{id}      # Xóa

# Nested resources
GET    /api/groups/{id}/students
GET    /api/students/{id}/commits
```

### Response Format
```json
// Success
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}

// Error
{
  "success": false,
  "error": {
    "code": "STUDENT_NOT_FOUND",
    "message": "Student with ID 123 not found"
  }
}

// Pagination
{
  "success": true,
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalItems": 48
  }
}
```

---

## ✅ 8. CHECKLIST TRƯỚC KHI COMMIT

### Code Quality
- [ ] Không có hardcoded values
- [ ] Không có magic numbers (dùng constants)
- [ ] Không có code comment thừa
- [ ] Không có `console.log` / `Debug.WriteLine` thừa
- [ ] Có error handling đầy đủ
- [ ] Methods không quá dài (< 30 lines)

### Security
- [ ] Không commit secrets/api keys
- [ ] Validate tất cả input
- [ ] Escape output tránh XSS
- [ ] Dùng parameterized queries (tránh SQL injection)
- [ ] Check authorization ở mọi endpoint

### Testing
- [ ] Unit tests cho business logic
- [ ] Integration tests cho API endpoints
- [ ] Code coverage >= 70%

---

## 📦 9. DEPENDENCIES & PACKAGES RECOMMENDED

### Backend (.NET)
```xml
<!-- Core -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />

<!-- Utilities -->
<PackageReference Include="AutoMapper" />
<PackageReference Include="FluentValidation" />
<PackageReference Include="Serilog" />

<!-- API Integration -->
<PackageReference Include="Octokit" />               <!-- GitHub API -->
<PackageReference Include="Atlassian.Jira" />        <!-- Jira API -->

<!-- Testing -->
<PackageReference Include="xUnit" />
<PackageReference Include="Moq" />
<PackageReference Include="FluentAssertions" />
```

### Frontend (React)
```json
{
  "dependencies": {
    "react": "^18.x",
    "react-router-dom": "^6.x",
    "axios": "^1.x",
    "@tanstack/react-query": "^5.x",
    "zustand": "^4.x",
    "react-hook-form": "^7.x",
    "zod": "^3.x",
    "tailwindcss": "^3.x",
    "lucide-react": "^0.x"
  },
  "devDependencies": {
    "typescript": "^5.x",
    "vitest": "^1.x",
    "@testing-library/react": "^14.x"
  }
}
```

---

## 🎯 10. ROLES & PERMISSIONS MATRIX

| Feature | Admin | Lecturer | Team Leader | Team Member |
|---------|-------|----------|-------------|-------------|
| Manage Groups | ✅ | ❌ | ❌ | ❌ |
| Manage Lecturers | ✅ | ❌ | ❌ | ❌ |
| Configure Integrations | ✅ | ❌ | ❌ | ❌ |
| View All Groups | ✅ | Assigned only | Own group | Own group |
| Manage Tasks | ❌ | ❌ | ✅ | ❌ |
| Assign Tasks | ❌ | ❌ | ✅ | ❌ |
| Update Task Status | ❌ | ❌ | ✅ | Own tasks |
| View Reports | ✅ | ✅ | ✅ | Own only |
| View Commits Stats | ✅ | ✅ | ✅ | Own only |
| Generate SRS | ❌ | ❌ | ✅ | ❌ |

---

## 📞 11. EXTERNAL API INTEGRATION

### Jira Cloud REST API
```csharp
public class JiraService : IJiraService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://{domain}.atlassian.net/rest/api/3";

    public async Task<IEnumerable<JiraIssue>> GetIssuesAsync(string projectKey)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/search?jql=project={projectKey}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JqlSearchResult>();
    }
}
```

### GitHub REST API
```csharp
public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;

    public async Task<IEnumerable<Commit>> GetCommitsAsync(string owner, string repo)
    {
        return await _client.Repository.Commit.GetAll(owner, repo);
    }

    public async Task<CommitStatistics> GetContributorStats(string owner, string repo)
    {
        return await _client.Repository.Statistics.GetContributors(owner, repo);
    }
}
```

---

## 🚀 12. ENVIRONMENT CONFIGURATION

### .env.example (KHÔNG commit .env thật)
```env
# Database
DB_CONNECTION_STRING=Server=localhost;Database=SWP391Tool;...

# JWT
JWT_SECRET=your-secret-key-here
JWT_EXPIRY_HOURS=24

# Jira
JIRA_DOMAIN=your-domain
JIRA_EMAIL=your-email
JIRA_API_TOKEN=your-token

# GitHub
GITHUB_TOKEN=your-github-token
```

### appsettings.json structure
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "ExpiryHours": 24,
    "Issuer": "SWP391Tool",
    "Audience": "SWP391ToolUsers"
  },
  "ExternalApis": {
    "Jira": {
      "Domain": "",
      "Email": "",
      "ApiToken": ""
    },
    "GitHub": {
      "Token": ""
    }
  }
}
```

---

## 📌 TÓM TẮT NHANH

| Nguyên tắc | Mô tả ngắn |
|------------|------------|
| **DRY** | Don't Repeat Yourself - Không lặp code |
| **KISS** | Keep It Simple, Stupid - Đơn giản hóa |
| **YAGNI** | You Aren't Gonna Need It - Không làm thừa |
| **SOLID** | 5 nguyên tắc OOP cơ bản |
| **Clean Code** | Code đọc được như văn xuôi |
| **Separation of Concerns** | Phân tách trách nhiệm rõ ràng |

---

> 📅 Tạo ngày: 16/01/2026  
> 👨‍💻 Dự án: SWP391 Project Management Tool  
> 🛠️ Stack: C# .NET + React + Jira API + GitHub API
