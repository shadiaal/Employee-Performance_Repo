public class Employee
{
    public int Id { get; set; }  
    public string Name { get; set; }
    public double Salary { get; set; }
    public double PerformanceRating { get; set; } 
    public DateTime HireDate { get; set; }

    public int DepartmentId { get; set; } // Foreign Key
    public Department Department { get; set; }

    public List<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}
