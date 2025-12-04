using System;

namespace labaa4.Models
{
    public class FunctionSurface
    {
        public string Title { get; }
        public string Description { get; }
        public FunctionPoint[,] Points { get; }

        public FunctionSurface(string title, string description, FunctionPoint[,] points)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? string.Empty;
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }
    }
}

