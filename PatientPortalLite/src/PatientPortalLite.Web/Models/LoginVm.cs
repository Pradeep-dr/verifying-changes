using System.ComponentModel.DataAnnotations;

namespace PatientPortalLite.Web.Models;

public class LoginVm
{
    [Required, Display(Name = "Last name")]
    public string? LastName { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateOnly DateOfBirth { get; set; }

    [Required, Display(Name = "Postcode")]
    public string? Postcode { get; set; }

    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}

public class VerifyCodeVm
{
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "Please enter the 6-digit code.")]
    public string? Code { get; set; }

    public string? Error { get; set; }
}
