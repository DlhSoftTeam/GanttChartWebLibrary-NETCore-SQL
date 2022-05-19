using System;
using System.Collections.Generic;

namespace GanttChartWebLibrary_NETCore_SQL.Models.DB
{
    public partial class Predecessor
    {
        public int DependentTaskId { get; set; }
        public int PredecessorTaskId { get; set; }
        public int DependencyType { get; set; }

        public virtual Task DependentTask { get; set; } = null!;
        public virtual Task Task { get; set; } = null!;
    }
}
