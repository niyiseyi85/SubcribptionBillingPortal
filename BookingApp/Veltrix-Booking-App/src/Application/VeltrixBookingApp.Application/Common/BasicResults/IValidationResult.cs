using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VeltrixBookingApp.Application.Common.BasicResults
{
    public interface IValidationResult
    {
        [JsonIgnore]
        HttpStatusCode Status { get; set; }
        string ErrorMessage { get; set; }

        IEnumerable<string> Errors { get; set; }
    }
}
