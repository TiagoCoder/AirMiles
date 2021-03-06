﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AIrMiles.WebApp.Common.Data.Entities
{
    public class User : IdentityUser
    {
        [Display(Name ="First Name")]
        public string FirstName { get; set; }

        [Display(Name ="Last Name")]
        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }

        public string PhotoUrl { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
