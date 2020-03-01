using System.Collections.Generic;

namespace Template.Blazor.Web.Models
{
    public class ApiResponseModel<T>
    {
        public T Result { get; set; }

        public bool Succeeded { get; set; }

        public List<string> Warnings { get; set; }

        public List<string> Errors { get; set; }
    }
}