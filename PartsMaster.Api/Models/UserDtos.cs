namespace PartsMaster.Api.Models
{
    public class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "User";
    }

    public class UpdateUserDto
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class ChangePasswordDto
    {
        public string NewPassword { get; set; } = "";
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
