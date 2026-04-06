using SV22T1020433.Models.HR;

namespace SV22T1020433.Admin.Models
{
    public class EmployeeChangePasswordViewModel
    {
        public Employee Employee { get; set; } = new Employee();
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }

    public class EmployeeRoleViewModel
    {
        public Employee Employee { get; set; } = new Employee();
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}
