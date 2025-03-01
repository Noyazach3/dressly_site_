using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "שם פרטי נדרש")]
        public string Username { get; set; }


        [Required(ErrorMessage = "אימייל נדרש")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; }

        [Required(ErrorMessage = "סיסמה נדרשת")]
        public string PasswordHash { get; set; }
    }
}
