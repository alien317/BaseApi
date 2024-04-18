using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Common.Models.ApiRequests
{
    public class CreateRoleRequest
    {
        [MinLength(4)]
        public string RoleName { get; set; }
    }
}
