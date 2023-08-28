using System.Collections.Generic;
using System.Linq;

namespace FilterTracker.Models
{
    public class FilterListModel : ModelBase
    {
        public List<Filter> FilterList { get; set; }
        public FilterListModel()
        {
            using (var db = new FilterTrackerEntities())
            {
                FilterList = db.Filters.AsNoTracking().ToList();
            }
        }
    }
}