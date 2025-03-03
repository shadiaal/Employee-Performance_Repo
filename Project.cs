namespace EmployeeTrackingSystem
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Budget { get; set; }
        public DateTime Deadline { get; set; }
        public ICollection<EmployeeProject> EmployeeProjects { get; set; }
    }
}
