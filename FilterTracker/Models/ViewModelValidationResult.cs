using System.Collections.Generic;

namespace FilterTracker.Models
{
    public class ViewModelValidationResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; }
    }
}