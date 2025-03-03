using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace EmployeeTrackingSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=.;Database=EmployeeTrackingDB;Trusted_Connection=True;TrustServerCertificate=True;";

            using (var context = new AppDbContext())
            {
                Console.WriteLine("Seeding Database with Dummy Data...");

                bool hasEmployees = context.Employees.Any();
                bool hasProjects = context.Projects.Any();

                if (!hasEmployees)
                {
                    var department = new Department { Name = "Software Engineering" };
                    context.Departments.Add(department);
                    context.SaveChanges();

                    var employees = new[]
                    {
                        new Employee { Name = "Ali Ahmed", Salary = 5000, PerformanceRating = 4.2, DepartmentId = department.Id },
                        new Employee { Name = "Sara Khalid", Salary = 7000, PerformanceRating = 3.8, DepartmentId = department.Id },
                        new Employee { Name = "Mohammed Saeed", Salary = 5500, PerformanceRating = 4.5, DepartmentId = department.Id }
                    };
                    context.Employees.AddRange(employees);
                    context.SaveChanges();
                }

                if (!hasProjects)
                {
                    var projects = new[]
                    {
                        new Project { Name = "AI Chatbot", Budget = 15000, Deadline = DateTime.Now.AddMonths(2) },
                        new Project { Name = "E-Commerce Platform", Budget = 30000, Deadline = DateTime.Now.AddMonths(4) },
                        new Project { Name = "HR Management System", Budget = 20000, Deadline = DateTime.Now.AddMonths(1) }
                    };
                    context.Projects.AddRange(projects);
                    context.SaveChanges();
                }

                // Retrieve updated employee and project lists
                var employeesList = context.Employees.ToList();
                var projectsList = context.Projects.ToList();

                if (employeesList.Count > 0 && projectsList.Count > 0)
                {
                    var employeeProjects = new[]
                    {
                        new EmployeeProject { EmployeeId = employeesList[0].Id, ProjectId = projectsList[0].Id },
                        new EmployeeProject { EmployeeId = employeesList[0].Id, ProjectId = projectsList[1].Id },
                        new EmployeeProject { EmployeeId = employeesList[1].Id, ProjectId = projectsList[1].Id },
                        new EmployeeProject { EmployeeId = employeesList[1].Id, ProjectId = projectsList[2].Id },
                        new EmployeeProject { EmployeeId = employeesList[2].Id, ProjectId = projectsList[2].Id },
                        new EmployeeProject { EmployeeId = employeesList[2].Id, ProjectId = projectsList[0].Id }
                    };
                    context.EmployeeProjects.AddRange(employeeProjects);
                    context.SaveChanges();
                }

                Console.WriteLine("Dummy data added successfully!");
            }

            // Optimized LINQ query for better performance
            using (var context = new AppDbContext())
            {
                var employees = context.Employees
                    .Include(e => e.EmployeeProjects)
                    .ThenInclude(ep => ep.Project)
                    .AsEnumerable()
                    .Where(e => e.EmployeeProjects.Count(ep => ep.Project.Deadline >= DateTime.Now.AddMonths(-6)) > 3)
                    .ToList();

                Console.WriteLine("\nEmployees who worked on more than 3 projects in the last 6 months:");
                foreach (var employee in employees)
                {
                    Console.WriteLine(employee.Name);
                }
            }

            // Fetch employees and projects using Dapper
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
                SELECT e.Name AS EmployeeName, p.Name AS ProjectName, p.Deadline 
                FROM Employees e
                JOIN EmployeeProjects ep ON e.Id = ep.EmployeeId
                JOIN Projects p ON ep.ProjectId = p.Id";

                var employeesWithProjects = connection.Query(query);

                Console.WriteLine("\n📌 Employees with their Projects:");
                foreach (var item in employeesWithProjects)
                {
                    Console.WriteLine($"Employee: {item.EmployeeName}, Project: {item.ProjectName}, Deadline: {item.Deadline}");
                }
            }

            // Improved stored procedure execution with Dapper
            using (var connection = new SqlConnection(connectionString))
            {
                string createProcedureQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'CalculateBonuses') AND type in (N'P'))
                    BEGIN
                        EXEC('
                            CREATE PROCEDURE CalculateBonuses
                            AS
                            BEGIN
                                SELECT Id, Name, Salary, PerformanceRating,
                                       (Salary * (PerformanceRating * 0.05)) AS Bonus
                                FROM Employees;
                            END;');
                    END;";

                connection.Execute(createProcedureQuery);
                Console.WriteLine("\nStored procedure 'CalculateBonuses' checked/created successfully.");

                Console.WriteLine("\nEmployee Bonuses:");
                var results = connection.Query("CalculateBonuses", commandType: CommandType.StoredProcedure);
                foreach (var row in results)
                {
                    Console.WriteLine($"Employee: {row.Name}, Salary: {row.Salary:C}, Rating: {row.PerformanceRating}, Bonus: {row.Bonus:C}");
                }
            }

            // Comparing EF Core and Dapper performance
            using (var context = new AppDbContext())
            {
                var totalSalary = context.Employees.Sum(e => e.Salary);
                var totalBudget = context.Projects.Sum(p => p.Budget);

                Console.WriteLine($"\n EF Core - Total Salaries: {totalSalary:C}, Total Project Budget: {totalBudget:C}");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT SUM(Salary) AS TotalSalary FROM Employees; SELECT SUM(Budget) AS TotalBudget FROM Projects;";
                using (var multi = connection.QueryMultiple(query))
                {
                    var totalSalary = multi.Read<decimal>().FirstOrDefault();
                    var totalBudget = multi.Read<decimal>().FirstOrDefault();
                    Console.WriteLine($"\nDapper - Total Salaries: {totalSalary:C}, Total Project Budget: {totalBudget:C}");
                }
            }
        }
    }
}
